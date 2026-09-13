using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Qgp.Api.Domain.Entities;
using Qgp.Api.Infrastructure.Persistence;

namespace Qgp.Api.Tests;

/// <summary>
/// Kiểm thử cấu hình persistence trên model metadata — KHÔNG cần Postgres.
/// Build model offline rồi assert ràng buộc governance (BR-01/02/07), naming, seed.
/// </summary>
public class QgpDbContextTests
{
    private static QgpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QgpDbContext>()
            .UseNpgsql("Host=localhost;Database=qgp_db;Username=qgp;Password=qgp")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new QgpDbContext(options);
    }

    // Seed data + check constraint là metadata design-time → bị lược khỏi runtime model (ctx.Model).
    private static IModel DesignModel(QgpDbContext ctx) => ctx.GetService<IDesignTimeModel>().Model;

    [Fact]
    public void Uses_named_schema_qgp_not_public()
    {
        using var ctx = CreateContext();
        Assert.Equal("qgp", ctx.Model.GetDefaultSchema());
    }

    [Fact]
    public void Maps_all_twelve_core_tables_in_snake_case()
    {
        using var ctx = CreateContext();
        var tables = ctx.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .Where(n => n is not null)
            .ToHashSet();

        string[] expected =
        [
            "users", "roles", "user_roles", "tags", "documents", "document_versions",
            "doc_tags", "doc_audience_roles", "doc_relations", "acknowledgements",
            "feedback", "audit_logs",
        ];
        foreach (var t in expected)
            Assert.Contains(t, tables);
    }

    [Fact]
    public void BR02_partial_unique_index_one_effective_per_doc()
    {
        using var ctx = CreateContext();
        var vt = ctx.Model.FindEntityType(typeof(DocumentVersion))!;
        var idx = vt.GetIndexes().Single(i => i.GetDatabaseName() == "uq_one_effective_per_doc");

        Assert.True(idx.IsUnique);
        Assert.Contains("Effective", idx.GetFilter());
    }

    [Fact]
    public void BR07_check_constraint_effective_ge_issue()
    {
        using var ctx = CreateContext();
        var vt = DesignModel(ctx).FindEntityType(typeof(DocumentVersion))!;
        var ck = vt.GetCheckConstraints().SingleOrDefault(c => c.Name == "ck_effective_ge_issue");

        Assert.NotNull(ck);
        Assert.Contains("effective_date >= issue_date", ck!.Sql);
    }

    [Fact]
    public void BR01_doc_id_is_unique()
    {
        using var ctx = CreateContext();
        var dt = ctx.Model.FindEntityType(typeof(Document))!;
        var idx = dt.GetIndexes().Single(i => i.GetDatabaseName() == "uq_doc_id");
        Assert.True(idx.IsUnique);
    }

    [Fact]
    public void Ids_default_to_gen_random_uuid()
    {
        using var ctx = CreateContext();
        var idProp = ctx.Model.FindEntityType(typeof(Document))!.FindProperty(nameof(Document.Id))!;
        Assert.Equal("gen_random_uuid()", idProp.GetDefaultValueSql());
    }

    [Fact]
    public void Seeds_six_rbac_roles()
    {
        using var ctx = CreateContext();
        var seed = DesignModel(ctx).FindEntityType(typeof(Role))!.GetSeedData().ToList();

        Assert.Equal(6, seed.Count);
        var codes = seed.Select(d => (string)d["Code"]!).ToHashSet();
        foreach (var code in new[] { "READER", "CONTRIBUTOR", "AUTHOR", "APPROVER", "QA_LEAD", "ADMIN" })
            Assert.Contains(code, codes);
    }
}
