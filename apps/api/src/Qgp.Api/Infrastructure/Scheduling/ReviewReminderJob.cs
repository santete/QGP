using Quartz;
using Qgp.Api.Application;

namespace Qgp.Api.Infrastructure.Scheduling;

/// <summary>
/// Job Quartz: nhắc chu kỳ soát xét tài liệu (DOC-F-08). Chạy hằng ngày, quét next_review_date
/// và sinh thông báo cho QA_LEAD/ADMIN. DisallowConcurrentExecution → không chồng lần chạy.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ReviewReminderJob(IServiceProvider services) : IJob
{
    public static readonly JobKey Key = new("review-reminder");

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IReviewReminderService>();
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        await svc.RunAsync(today, context.CancellationToken);
    }
}
