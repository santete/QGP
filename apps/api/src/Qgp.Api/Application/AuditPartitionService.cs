using Microsoft.EntityFrameworkCore;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Application;

public interface IAuditPartitionService
{
    /// <summary>
    /// Đảm bảo partition tháng cho audit_logs tồn tại từ tháng asOf tới asOf+monthsAhead.
    /// Idempotent (CREATE TABLE IF NOT EXISTS). Tạo TRƯỚC khi có row của tháng đó (tránh
    /// overlap với DEFAULT partition). Trả số partition đã đảm bảo.
    /// </summary>
    Task<int> EnsureUpcomingPartitionsAsync(DateOnly asOf, int monthsAhead = 2, CancellationToken ct = default);
}

/// <summary>Bảo trì declarative partitioning của audit_logs (SDD §2.4) — chạy định kỳ bởi Quartz.</summary>
public sealed class AuditPartitionService(QgpDbContext db, ILogger<AuditPartitionService> logger) : IAuditPartitionService
{
    public async Task<int> EnsureUpcomingPartitionsAsync(DateOnly asOf, int monthsAhead = 2, CancellationToken ct = default)
    {
        var first = new DateOnly(asOf.Year, asOf.Month, 1);
        var ensured = 0;
        for (var i = 0; i <= monthsAhead; i++)
        {
            var start = first.AddMonths(i);
            var end = start.AddMonths(1);
            var name = $"audit_logs_{start:yyyy_MM}";
            // Tên + ngày sinh từ lịch (không phải user input) → an toàn nội suy vào DDL.
            var sql =
                $"CREATE TABLE IF NOT EXISTS qgp.{name} PARTITION OF qgp.audit_logs " +
                $"FOR VALUES FROM ('{start:yyyy-MM-dd}') TO ('{end:yyyy-MM-dd}');";
            await db.Database.ExecuteSqlRawAsync(sql, ct);
            ensured++;
        }
        logger.LogInformation("Audit partition maintenance: đảm bảo {Count} partition từ {From}", ensured, first);
        return ensured;
    }
}
