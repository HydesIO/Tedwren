using Dapper;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Inductions;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration tests for the induction Dapper repositories against a real SQL Server. Run only when
/// TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and
/// deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class InductionRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Template_And_Session_RoundTrip_WithJsonAndSupersedeQuery()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);
        var templates = new InductionTemplateRepository(factory);
        var sessions = new InductionSessionRepository(factory);

        var companyId = Guid.NewGuid();
        var template = new InductionTemplate
        {
            CompanyId = companyId,
            Name = "Test Induction",
            ValidityDays = 365,
            PassMark = 2,
            Steps = new[] { new InductionStep("s1", InductionStepKind.Identity, "Identity", true) },
            Questions = new[] { new InductionQuizQuestion("q1", "?", new[] { "a", "b" }, 1) },
        };
        var personId = Guid.NewGuid();
        var session = new InductionSession
        {
            TemplateId = template.Id, CompanyId = companyId, PersonId = personId, PersonName = "Test Operative",
            CompletedStepIds = new List<string> { "s1" }, ConsentGiven = true,
        };

        try
        {
            await templates.AddAsync(template);

            // The quiz answers round-trip in JSON (server-side only, R5).
            var reloadedTemplate = await templates.GetByIdAsync(template.Id);
            Assert.Equal(1, reloadedTemplate!.Questions[0].CorrectOptionIndex);

            await sessions.AddAsync(session);

            // The latest non-superseded session is found for the supersede check (MC-7).
            var latest = await sessions.GetLatestForPersonAsync(companyId, template.Id, personId);
            Assert.NotNull(latest);
            Assert.Contains("s1", latest!.CompletedStepIds);

            // Passing persists the completion reference and validity (MC-5/MC-7).
            session.Status = InductionStatus.Passed;
            session.CompletionReference = "IND-TEST";
            session.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(365);
            session.LastScore = 2;
            await sessions.UpdateAsync(session);

            var reloadedSession = await sessions.GetAsync(session.Id);
            Assert.Equal(InductionStatus.Passed, reloadedSession!.Status);
            Assert.Equal("IND-TEST", reloadedSession.CompletionReference);
            Assert.True(reloadedSession.ConsentGiven);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM InductionSessions WHERE Id = @id", new { id = session.Id });
            await connection.ExecuteAsync("DELETE FROM InductionTemplates WHERE Id = @id", new { id = template.Id });
        }
    }
}
