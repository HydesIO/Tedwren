using Dapper;
using Tedwren.Abstractions.Configuration;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration tests for the Forms Library Dapper repository against a real SQL Server. Run only when
/// TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and
/// deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class FormTemplateRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Template_RoundTrips_WithSectionsJson_AndVersioning()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);
        var repository = new FormTemplateRepository(factory);

        var companyId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var v1 = new FormTemplate
        {
            CompanyId = companyId,
            FamilyId = familyId,
            Name = "Test Inspection",
            Description = "Round-trip test.",
            Version = 1,
            Status = FormTemplateStatus.Draft,
            Sections = new List<FormSectionDef>
            {
                new("s1", "General", new List<FormField>
                {
                    new("f1", FormFieldKind.RagStatus, "Housekeeping", "Check all", true, "{\"x\":1}", null, 0),
                    new("f2", FormFieldKind.YesNo, "Fire exits clear", null, true, null, null, 1),
                }, 0),
            },
        };
        var v2 = new FormTemplate
        {
            CompanyId = companyId,
            FamilyId = familyId,
            Name = "Test Inspection v2",
            Version = 2,
            Status = FormTemplateStatus.Draft,
            Sections = v1.Sections,
        };

        try
        {
            await repository.AddAsync(v1);

            // The sections/fields round-trip through JSON, including per-kind config.
            var reloaded = await repository.GetByIdAsync(v1.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(FormTemplateStatus.Draft, reloaded!.Status);
            Assert.Equal(2, reloaded.Sections[0].Fields.Count);
            Assert.Equal(FormFieldKind.RagStatus, reloaded.Sections[0].Fields[0].Kind);
            Assert.Equal("{\"x\":1}", reloaded.Sections[0].Fields[0].ValidationJson);

            // Publishing persists the status change.
            reloaded.Status = FormTemplateStatus.Published;
            reloaded.UpdatedUtc = DateTimeOffset.UtcNow;
            await repository.UpdateAsync(reloaded);
            Assert.Equal(FormTemplateStatus.Published, (await repository.GetByIdAsync(v1.Id))!.Status);

            // A second version is grouped under the same family, newest first.
            await repository.AddAsync(v2);
            var versions = await repository.GetByFamilyAsync(companyId, familyId);
            Assert.Equal(2, versions.Count);
            Assert.Equal(2, versions[0].Version);

            // The company query returns both versions.
            Assert.Equal(2, (await repository.GetByCompanyAsync(companyId)).Count);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM FormTemplates WHERE CompanyId = @id", new { id = companyId });
        }
    }
}
