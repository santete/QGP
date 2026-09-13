using Quartz;
using Qgp.Api.Application;

namespace Qgp.Api.Infrastructure.Scheduling;

/// <summary>
/// Job Quartz: định kỳ tạo trước partition tháng cho audit_logs (SDD §2.4) để insert luôn
/// rơi vào partition đúng thay vì DEFAULT. DisallowConcurrentExecution → không chồng lần chạy.
/// </summary>
[DisallowConcurrentExecution]
public sealed class AuditPartitionMaintenanceJob(IServiceProvider services) : IJob
{
    public static readonly JobKey Key = new("audit-partition-maintenance");

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IAuditPartitionService>();
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        await svc.EnsureUpcomingPartitionsAsync(today, monthsAhead: 2, context.CancellationToken);
    }
}
