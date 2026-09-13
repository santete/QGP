using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ISC.Observability.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Qgp.Api.Api;
using Qgp.Api.Application;
using Qgp.Api.Auth;
using Qgp.Api.Infrastructure.Git;
using Qgp.Api.Infrastructure.Persistence;
using Qgp.Api.Infrastructure.Scheduling;
using Qgp.Api.Infrastructure.Search;

var builder = WebApplication.CreateBuilder(args);

// A5 (ADR-0012) — secret dạng FILE (Vault Agent / Docker secrets): mỗi file 1 secret,
// tên file = config key ('__' cho nesting, vd Auth__ClientSecret). Path QGP_SECRETS_DIR (mặc định /run/secrets).
// Thêm SAU cùng → override appsettings/env. Optional để dev không cần thư mục này.
var secretsDir = Environment.GetEnvironmentVariable("QGP_SECRETS_DIR") ?? "/run/secrets";
if (Directory.Exists(secretsDir))
    builder.Configuration.AddKeyPerFile(secretsDir, optional: true);

// A7 (ADR-0013) — Observability: ISC.Observability (OTLP traces/metrics/logs + auto-instrument EF/Quartz/HTTP).
// Bật khi có Otel:OtlpEndpoint (prod: Otel__OtlpEndpoint=http://otel-collector:4317). KHÔNG bật ở Testing.
var obsEnabled = !string.IsNullOrWhiteSpace(builder.Configuration["Otel:OtlpEndpoint"])
    && !builder.Environment.IsEnvironment("Testing");
if (obsEnabled)
    builder.AddStandardObservability(builder.Configuration["ServiceName"] ?? "qgp-api");

// Connection string: env QGP_DB_CONNECTION > appsettings "Qgp". (Secret qua config, không hardcode.)
var conn =
    Environment.GetEnvironmentVariable("QGP_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("Qgp")
    ?? "Host=localhost;Port=5432;Database=qgp_db;Username=qgp;Password=qgp";

builder.Services.AddDbContext<QgpDbContext>(options =>
    options
        .UseNpgsql(conn, o => o.MigrationsHistoryTable("__ef_migrations_history", QgpDbContext.Schema))
        .UseSnakeCaseNamingConvention());

// JSON: field snake_case (R-RESP-FIELD-001 + openapi), bỏ null, DateOnly mặc định "yyyy-MM-dd".
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Readiness gồm kết nối DB (T1 DoD: healthz phản ánh Postgres sẵn sàng).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<QgpDbContext>("postgres");

// Auth (OIDC/dev) + RBAC §10.1 (T6).
builder.Services.AddQgpAuth(builder.Configuration);

// Application services (E1.1 — DOC lifecycle WF-01 + đọc).
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IVersionService, VersionService>();
builder.Services.AddScoped<IEffectiveTransitionService, EffectiveTransitionService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReviewReminderService, ReviewReminderService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAuditPartitionService, AuditPartitionService>();

// Git content store (ADR-0001 docs-as-code): commit nội dung khi publish. Repo cục bộ (data volume);
// path qua env QGP_GIT_REPO_PATH > config Git:RepoPath > mặc định "content-repo" cạnh content root.
var gitRepoPath = Environment.GetEnvironmentVariable("QGP_GIT_REPO_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Git:RepoPath"] ?? "content-repo");
builder.Services.AddSingleton<IGitContentStore>(new LibGit2GitContentStore(gitRepoPath));

// Search (Meilisearch REST). HttpClient có Authorization (mọi request) + timeout (R-TIMEOUT-MUST).
// Secret master key qua env QGP_MEILI_KEY (fallback dev config).
builder.Services.AddHttpClient<ISearchIndex, MeiliSearchIndex>(http =>
{
    var url = builder.Configuration["Meili:Url"] ?? "http://localhost:7700";
    var key = Environment.GetEnvironmentVariable("QGP_MEILI_KEY") ?? builder.Configuration["Meili:ApiKey"];
    http.BaseAddress = new Uri(url);
    http.Timeout = TimeSpan.FromSeconds(5);
    if (!string.IsNullOrEmpty(key))
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
});
builder.Services.AddScoped<ISearchService, SearchService>();

// Scheduler WF-03 (Quartz.NET): auto Published→Effective (SDD §5.1). KHÔNG chạy hosted job ở Testing.
if (!builder.Environment.IsEnvironment("Testing"))
{
    var intervalSeconds = builder.Configuration.GetValue("Scheduler:IntervalSeconds", 60);
    builder.Services.AddQuartz(q =>
    {
        q.AddJob<EffectiveTransitionJob>(EffectiveTransitionJob.Key);
        q.AddTrigger(t => t
            .ForJob(EffectiveTransitionJob.Key)
            .WithSimpleSchedule(s => s.WithIntervalInSeconds(intervalSeconds).RepeatForever()));

        // Bảo trì partition audit_logs (SDD §2.4): chạy ngay lúc start + lặp mỗi ngày (tạo trước partition tháng kế).
        var partitionIntervalSeconds = builder.Configuration.GetValue("Scheduler:PartitionMaintenanceIntervalSeconds", 86400);
        q.AddJob<AuditPartitionMaintenanceJob>(AuditPartitionMaintenanceJob.Key);
        q.AddTrigger(t => t
            .ForJob(AuditPartitionMaintenanceJob.Key)
            .StartNow()
            .WithSimpleSchedule(s => s.WithIntervalInSeconds(partitionIntervalSeconds).RepeatForever()));

        // Nhắc chu kỳ soát xét (DOC-F-08): quét next_review_date hằng ngày → thông báo QA_LEAD/ADMIN.
        var reviewReminderIntervalSeconds = builder.Configuration.GetValue("Scheduler:ReviewReminderIntervalSeconds", 86400);
        q.AddJob<ReviewReminderJob>(ReviewReminderJob.Key);
        q.AddTrigger(t => t
            .ForJob(ReviewReminderJob.Key)
            .StartNow()
            .WithSimpleSchedule(s => s.WithIntervalInSeconds(reviewReminderIntervalSeconds).RepeatForever()));
    });
    builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
}

// Envelope lỗi { error: { code, message } } (ADR-0014).
builder.Services.AddExceptionHandler<QgpExceptionHandler>();
builder.Services.AddProblemDetails();

// A7 — Forwarded headers (API đứng sau nginx): lấy IP/scheme thật từ X-Forwarded-* cho rate limit + log.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();   // ingress duy nhất là nginx nội bộ → tin proxy phía trước
    o.KnownProxies.Clear();
});

// A7 — Rate limiting (SECURITY_RULES: chống brute-force). Global fixed-window theo IP,
// riêng /auth siết chặt hơn. Ngưỡng cấu hình được (RateLimit:*) → 429 khi vượt.
// Mặc định bật, TRỪ môi trường Testing (tránh nhiễu suite nhiều request); test rate-limit tự opt-in.
var rlEnabled = builder.Configuration.GetValue("RateLimit:Enabled", !builder.Environment.IsEnvironment("Testing"));
var rlPermit = builder.Configuration.GetValue("RateLimit:PermitLimit", 100);
var rlWindow = builder.Configuration.GetValue("RateLimit:WindowSeconds", 10);
var rlAuthPermit = builder.Configuration.GetValue("RateLimit:AuthPermitLimit", 10);
var rlAuthWindow = builder.Configuration.GetValue("RateLimit:AuthWindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var isAuth = ctx.Request.Path.StartsWithSegments("/auth");
        return RateLimitPartition.GetFixedWindowLimiter(
            (isAuth ? "auth:" : "global:") + ip,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isAuth ? rlAuthPermit : rlPermit,
                Window = TimeSpan.FromSeconds(isAuth ? rlAuthWindow : rlWindow),
                QueueLimit = 0,
            });
    });
});

// CORS: origin đọc từ config (Cors:AllowedOrigins, csv). Prod set env Cors__AllowedOrigins=https://qgp.example.com.
// KHÔNG dùng '*' cho endpoint authenticated (SECURITY_RULES). Dev mặc định Vite :5173.
const string WebCors = "web";
var corsOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddPolicy(WebCors, p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// IP/scheme thật từ nginx (phải chạy sớm nhất để middleware sau thấy đúng).
app.UseForwardedHeaders();

// ISC.Observability middleware (request logging + TraceId). Đặt NGOÀI UseExceptionHandler để
// QgpExceptionHandler (inner) vẫn sở hữu envelope lỗi {error:{code,message}} (ADR-0014).
if (obsEnabled)
    app.UseStandardObservability();

// A7 — security headers cơ bản (nginx bổ sung CSP/HSTS ở lớp edge cho SPA).
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseExceptionHandler();
app.UseCors(WebCors);
if (rlEnabled) app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Liveness/readiness (T1 DoD: healthz xanh) — anonymous.
app.MapHealthChecks("/healthz");

app.MapGet("/", () => Results.Ok(new { service = "qgp-api", status = "ok" }));

app.MapAuthEndpoints();
app.MapV1();

// Seed dữ liệu mẫu (chỉ Dev) — QA-PROC-005 Effective cho slice S4.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();
    await DevDataSeeder.SeedAsync(db);

    // Build lại Meilisearch index từ DB (best-effort — Meili có thể chưa sẵn sàng).
    try
    {
        var search = scope.ServiceProvider.GetRequiredService<ISearchService>();
        await search.ReindexAllAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Bỏ qua reindex lúc startup (Meili chưa sẵn sàng?)");
    }
}

app.Run();

/// <summary>Điểm neo cho WebApplicationFactory ở integration test (E1+).</summary>
public partial class Program;
