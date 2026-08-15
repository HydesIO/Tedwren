using Dapper;
using Tedwren.Abstractions.Configuration;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration tests for the Dapper site repositories against a real SQL Server. Run only when
/// TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and
/// deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class SiteRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Site_AndProperty_RoundTrip_WithBoundary()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);

        var sites = new SiteRepository(factory);
        var properties = new SitePropertyRepository(factory);

        var site = new Site
        {
            CompanyId = Guid.NewGuid(),
            Name = "IntegrationTest " + Guid.NewGuid(),
            HasCompound = false,
            IsDispersed = true,
        };
        var property = new SiteProperty
        {
            SiteId = site.Id,
            Address = "1 Test Street",
            Units = 4,
            Boundary = new Geofence(51.5074, -0.1278, 75),
        };

        try
        {
            await sites.AddAsync(site);
            await properties.AddAsync(property);

            var reread = await sites.GetByIdAsync(site.Id);
            Assert.NotNull(reread);
            Assert.True(reread!.IsDispersed);
            Assert.False(reread.HasCompound);

            var siteProperties = await properties.GetBySiteAsync(site.Id);
            var storedProperty = Assert.Single(siteProperties);
            Assert.Equal(75, storedProperty.Boundary.RadiusMetres);
            Assert.Equal(1, await properties.CountBySiteAsync(site.Id));
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM SiteProperties WHERE SiteId = @id", new { id = site.Id });
            await connection.ExecuteAsync("DELETE FROM Sites WHERE Id = @id", new { id = site.Id });
        }
    }
}
