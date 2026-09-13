using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qgp.Api.Application;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Task 5b — bảo trì partition audit_logs (SDD §2.4): EnsureUpcomingPartitionsAsync tạo partition
/// tháng tương lai, idempotent. Cần Postgres đã áp migration PartitionAuditLogs.
/// </summary>
public class AuditPartitionTests(QgpApiFactory factory) : IClassFixture<QgpApiFactory>
{
    private readonly QgpApiFactory _factory = factory;

    private static async Task<bool> PartitionExistsAsync(QgpDbContext db, string name)
    {
        var rows = await db.Database
            .SqlQueryRaw<int>("SELECT 1 AS \"Value\" FROM pg_class WHERE relname = {0} AND relnamespace = 'qgp'::regnamespace", name)
            .ToListAsync();
        return rows.Count > 0;
    }

    [Fact]
    public async Task Ensure_upcoming_partitions_creates_future_month_and_is_idempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IAuditPartitionService>();
        var db = scope.ServiceProvider.GetRequiredService<QgpDbContext>();

        // Tháng tương lai (chưa có partition).
        var asOf = new DateOnly(2027, 3, 1);
        try
        {
            await svc.EnsureUpcomingPartitionsAsync(asOf, monthsAhead: 0);
            Assert.True(await PartitionExistsAsync(db, "audit_logs_2027_03"));

            // Gọi lại → không lỗi (CREATE TABLE IF NOT EXISTS).
            var ensured = await svc.EnsureUpcomingPartitionsAsync(asOf, monthsAhead: 0);
            Assert.Equal(1, ensured);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS qgp.audit_logs_2027_03;");
        }
    }
}
