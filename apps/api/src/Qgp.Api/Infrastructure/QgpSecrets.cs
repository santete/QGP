namespace Qgp.Api.Infrastructure;

/// <summary>
/// A5 (ADR-0012) — đọc secret qua IConfiguration để thứ tự ưu tiên là
/// file /run/secrets (AddKeyPerFile: Docker secrets / Vault Agent) &gt; env var &gt; appsettings.
/// KHÔNG đọc thẳng Environment — sẽ bỏ qua file secret.
/// </summary>
public static class QgpSecrets
{
    private const string DevDbConnection = "Host=localhost;Port=5432;Database=qgp_db;Username=qgp;Password=qgp";

    public static string DbConnection(IConfiguration config) =>
        config["QGP_DB_CONNECTION"]
        ?? config.GetConnectionString("Qgp")
        ?? DevDbConnection;

    public static string? MeiliKey(IConfiguration config) =>
        config["QGP_MEILI_KEY"] ?? config["Meili:ApiKey"];
}
