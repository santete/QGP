using Microsoft.Extensions.Configuration;
using Qgp.Api.Infrastructure;

namespace Qgp.Api.Tests;

/// <summary>
/// A5 (ADR-0012) — secret DB/Meili phải đọc qua IConfiguration để file /run/secrets (Docker secrets /
/// Vault Agent, AddKeyPerFile) thắng env var. Trước đây Program.cs đọc thẳng Environment → file bị bỏ qua.
/// </summary>
public sealed class SecretConfigTests : IDisposable
{
    private readonly string _secretsDir = Directory.CreateTempSubdirectory("qgp-secrets-").FullName;

    public void Dispose() => Directory.Delete(_secretsDir, recursive: true);

    // Thứ tự nguồn giống Program.cs: appsettings → env var → key-per-file (sau cùng thắng).
    private IConfiguration Build(Dictionary<string, string?> appsettings, Dictionary<string, string?> env) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(appsettings)
            .AddInMemoryCollection(env)
            .AddKeyPerFile(_secretsDir, optional: true)
            .Build();

    [Fact]
    public void Secret_file_overrides_env_for_db_connection_and_meili_key()
    {
        File.WriteAllText(Path.Combine(_secretsDir, "QGP_DB_CONNECTION"), "Host=from-file\n");
        File.WriteAllText(Path.Combine(_secretsDir, "QGP_MEILI_KEY"), "meili-from-file\n");
        var config = Build(new(), new() { ["QGP_DB_CONNECTION"] = "Host=from-env", ["QGP_MEILI_KEY"] = "meili-from-env" });

        Assert.Equal("Host=from-file", QgpSecrets.DbConnection(config));
        Assert.Equal("meili-from-file", QgpSecrets.MeiliKey(config));
    }

    [Fact]
    public void Env_is_used_when_no_secret_file()
    {
        var config = Build(
            new() { ["ConnectionStrings:Qgp"] = "Host=from-appsettings", ["Meili:ApiKey"] = "meili-from-appsettings" },
            new() { ["QGP_DB_CONNECTION"] = "Host=from-env", ["QGP_MEILI_KEY"] = "meili-from-env" });

        Assert.Equal("Host=from-env", QgpSecrets.DbConnection(config));
        Assert.Equal("meili-from-env", QgpSecrets.MeiliKey(config));
    }

    [Fact]
    public void Falls_back_to_appsettings_then_dev_default()
    {
        var withAppsettings = Build(
            new() { ["ConnectionStrings:Qgp"] = "Host=from-appsettings", ["Meili:ApiKey"] = "meili-from-appsettings" }, new());
        Assert.Equal("Host=from-appsettings", QgpSecrets.DbConnection(withAppsettings));
        Assert.Equal("meili-from-appsettings", QgpSecrets.MeiliKey(withAppsettings));

        var empty = Build(new(), new());
        Assert.Equal("Host=localhost;Port=5432;Database=qgp_db;Username=qgp;Password=qgp", QgpSecrets.DbConnection(empty));
        Assert.Null(QgpSecrets.MeiliKey(empty));
    }
}
