using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Qgp.Api.Infrastructure.Persistence;

/// <summary>
/// Factory design-time cho EF tooling (`dotnet ef migrations add/script`) —
/// không cần chạy app hay kết nối DB thật. Connection string chỉ để build model.
/// </summary>
public class QgpDbContextFactory : IDesignTimeDbContextFactory<QgpDbContext>
{
    public QgpDbContext CreateDbContext(string[] args)
    {
        var conn =
            Environment.GetEnvironmentVariable("QGP_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=qgp_db;Username=qgp;Password=qgp";

        var options = new DbContextOptionsBuilder<QgpDbContext>()
            .UseNpgsql(conn, o => o.MigrationsHistoryTable("__ef_migrations_history", QgpDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new QgpDbContext(options);
    }
}
