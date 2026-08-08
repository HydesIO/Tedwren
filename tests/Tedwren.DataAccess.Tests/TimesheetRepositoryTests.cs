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
/// Integration tests for the timesheet Dapper repository against a real SQL Server. Run only when
/// TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and
/// deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class TimesheetRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Timesheet_RoundTrips_WithCorrectionAndStatusUpdate()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(factory, new SqlServerDialect()).RunAsync();
        var repository = new TimesheetRepository(factory);

        var companyId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var weekStart = new DateOnly(2026, 8, 3);

        var timesheet = new Timesheet
        {
            CompanyId = companyId, PersonId = personId, OperativeName = "Test Operative", WeekStart = weekStart,
        };
        var original = new TimesheetEntry
        {
            TimesheetId = timesheet.Id, WorkDate = weekStart, SiteId = siteId, SiteName = "Test Site", Hours = 8m,
        };

        try
        {
            await repository.AddAsync(timesheet, new[] { original });

            // Append a correction (R16) — the original is retained.
            await repository.AddEntryAsync(new TimesheetEntry
            {
                TimesheetId = timesheet.Id, WorkDate = weekStart, SiteId = siteId, SiteName = "Test Site",
                Hours = 6m, CorrectsEntryId = original.Id, Author = "Reviewer", Reason = "Left early",
                CreatedUtc = DateTimeOffset.UtcNow.AddMinutes(1),
            });

            var entries = await repository.GetEntriesAsync(timesheet.Id);
            Assert.Equal(2, entries.Count);   // original retained + correction
            Assert.Contains(entries, e => e.IsCorrection && e.Hours == 6m);

            // The header's workflow state persists.
            timesheet.Status = TimesheetStatus.Approved;
            timesheet.DecidedBy = "Reviewer";
            timesheet.DecidedUtc = DateTimeOffset.UtcNow;
            timesheet.DecisionScope = ApprovalScope.All;
            await repository.UpdateAsync(timesheet);

            var reloaded = await repository.GetByWeekAsync(companyId, personId, weekStart);
            Assert.NotNull(reloaded);
            Assert.Equal(TimesheetStatus.Approved, reloaded!.Status);
            Assert.Equal(ApprovalScope.All, reloaded.DecisionScope);

            var week = await repository.GetByCompanyWeekAsync(companyId, weekStart);
            Assert.Single(week);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM TimesheetEntries WHERE TimesheetId = @id", new { id = timesheet.Id });
            await connection.ExecuteAsync("DELETE FROM Timesheets WHERE Id = @id", new { id = timesheet.Id });
        }
    }
}
