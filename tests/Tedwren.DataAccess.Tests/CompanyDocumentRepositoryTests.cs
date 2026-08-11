using Dapper;
using Tedwren.Abstractions.Configuration;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration test for the company-document Dapper repository (SUB-4) against a real SQL Server. Runs only
/// when TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise it is skipped. All rows are
/// purpose-created with fresh GUIDs and deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class CompanyDocumentRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    /// <summary>Builds a connection factory for the configured test SQL Server.</summary>
    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Document_RoundTrips_ForItsCompany()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(factory, new SqlServerDialect()).RunAsync();

        var companies = new CompanyRepository(factory);
        var documents = new CompanyDocumentRepository(factory);

        var company = new Company { Name = "DocCo " + Guid.NewGuid() };
        var document = new CompanyDocument
        {
            CompanyId = company.Id,
            Name = "Employer's Liability Insurance",
            Type = "Insurance",
            ExpiresOn = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(6),
            Reference = "EL-" + Guid.NewGuid().ToString("N")[..8],
        };

        try
        {
            await companies.AddAsync(company);
            await documents.AddAsync(document);

            var byCompany = await documents.GetByCompanyAsync(company.Id);
            var loaded = Assert.Single(byCompany);
            Assert.Equal(document.Name, loaded.Name);
            Assert.Equal(document.Type, loaded.Type);
            Assert.Equal(document.ExpiresOn, loaded.ExpiresOn);
            Assert.Equal(document.Reference, loaded.Reference);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM CompanyDocuments WHERE CompanyId = @companyId", new { companyId = company.Id });
            await connection.ExecuteAsync("DELETE FROM Companies WHERE Id = @companyId", new { companyId = company.Id });
        }
    }
}
