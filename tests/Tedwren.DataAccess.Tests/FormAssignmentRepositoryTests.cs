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
/// Integration tests for the Forms Library assignment repository against a real SQL Server. Run only when
/// TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and deleted
/// afterwards, so existing data is never modified.
/// </summary>
public sealed class FormAssignmentRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Assignment_RoundTrips_AndByFamilyQueryWorks()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);
        var repository = new FormAssignmentRepository(factory);

        var companyId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var assignment = new FormAssignment
        {
            CompanyId = companyId,
            FormTemplateFamilyId = familyId,
            FormName = "Plant Check",
            Scope = FormScope.Site,
            SiteId = Guid.NewGuid(),
            Schedule = FormSchedule.Weekly,
            FailureAlertEmail = "safety@example.com",
        };

        try
        {
            await repository.AddAsync(assignment);

            var reloaded = await repository.GetByIdAsync(assignment.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(FormScope.Site, reloaded!.Scope);
            Assert.Equal(FormSchedule.Weekly, reloaded.Schedule);
            Assert.Equal("safety@example.com", reloaded.FailureAlertEmail);

            var byFamily = await repository.GetByFamilyAsync(companyId, familyId);
            Assert.Single(byFamily);

            Assert.Single(await repository.GetByCompanyAsync(companyId));

            // R12 — recurring assignments are picked up by the reminder query and the last-reminder marker round-trips.
            Assert.Contains(await repository.GetRecurringAsync(), a => a.Id == assignment.Id);
            Assert.Null(reloaded.LastReminderUtc);
            var remindedAt = DateTimeOffset.UtcNow;
            await repository.UpdateLastReminderAsync(assignment.Id, remindedAt);
            var afterReminder = await repository.GetByIdAsync(assignment.Id);
            Assert.NotNull(afterReminder!.LastReminderUtc);

            await repository.DeleteAsync(assignment.Id);
            Assert.Null(await repository.GetByIdAsync(assignment.Id));
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM FormAssignments WHERE CompanyId = @id", new { id = companyId });
        }
    }
}
