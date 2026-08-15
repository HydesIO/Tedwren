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
/// Integration tests for the entitlement, audit and decision Dapper repositories against a real SQL Server.
/// Run only when TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are
/// purpose-created and deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class ConsoleFoundationRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Entitlement_Upsert_RoundTrips()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);
        var repository = new EntitlementRepository(factory);
        var company = Guid.NewGuid();

        try
        {
            await repository.SetAsync(company, "time", enabled: true);
            await repository.SetAsync(company, "time", enabled: false);   // upsert (Q2: one per company+module)

            var overrides = await repository.GetOverridesByCompanyAsync(company);
            Assert.True(overrides.TryGetValue("time", out var enabled));
            Assert.False(enabled);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM ModuleEntitlements WHERE CompanyId = @company", new { company });
        }
    }

    [SkippableFact]
    public async Task Audit_Search_ByCompanyAndText()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);
        var repository = new AuditRepository(factory);
        var company = Guid.NewGuid();
        var marker = "Riverside-" + Guid.NewGuid().ToString("N");

        try
        {
            await repository.AddAsync(new AuditEntry
            {
                CompanyId = company, Actor = "Priya Shah", Action = "Approved timesheet",
                Entity = marker, Reference = "TS-77", Category = "Time",
            });

            var hits = await repository.SearchAsync(company, marker, null, null);
            Assert.Single(hits);

            var scoped = await repository.SearchAsync(Guid.NewGuid(), marker, null, null);
            Assert.Empty(scoped);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM AuditEntries WHERE CompanyId = @company", new { company });
        }
    }

    [SkippableFact]
    public async Task Decision_RoundTrips_WithChecks()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);
        var repository = new DecisionRepository(factory);
        var personId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        try
        {
            var decision = new SiteEntryDecision
            {
                PersonId = personId,
                SiteId = siteId,
                Admitted = false,
                Checks = new List<DecisionCheck>
                {
                    new("Registered", DecisionCheckOutcome.Passed, null),
                    new("Cards in date", DecisionCheckOutcome.Failed, "CSCS expired"),
                },
            };
            await repository.AddAsync(decision);

            var back = Assert.Single(await repository.GetByPersonAsync(personId));
            Assert.False(back.Admitted);
            Assert.Equal(2, back.Checks.Count);
            Assert.Contains(back.Checks, c => c.Name == "Cards in date" && c.Outcome == DecisionCheckOutcome.Failed);
            Assert.Single(await repository.GetBySiteAsync(siteId));
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM Decisions WHERE PersonId = @personId", new { personId });
        }
    }
}
