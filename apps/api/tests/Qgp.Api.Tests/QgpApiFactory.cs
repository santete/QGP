using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Qgp.Api.Tests;

/// <summary>
/// Factory test dùng environment "Testing" → KHÔNG chạy DevDataSeeder (chỉ Development).
/// Auth: mặc định Dev mode (khoá ephemeral). Integration test hit Postgres thật (docker compose up).
/// </summary>
public class QgpApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
