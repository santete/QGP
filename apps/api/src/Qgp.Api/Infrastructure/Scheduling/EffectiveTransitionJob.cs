using Quartz;
using Qgp.Api.Application;

namespace Qgp.Api.Infrastructure.Scheduling;

/// <summary>
/// Job Quartz WF-03: định kỳ chuyển bản Published tới hạn sang Effective.
/// DisallowConcurrentExecution → không chồng lần chạy (idempotent thêm 1 lớp).
/// </summary>
[DisallowConcurrentExecution]
public sealed class EffectiveTransitionJob(IServiceProvider services) : IJob
{
    public static readonly JobKey Key = new("wf03-effective-transition");

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IEffectiveTransitionService>();
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        await svc.RunAsync(today, context.CancellationToken);
    }
}
