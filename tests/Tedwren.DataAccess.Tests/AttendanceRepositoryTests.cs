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
/// Integration tests for the Dapper attendance repository against a real SQL Server. Run only when
/// TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and
/// deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class AttendanceRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task OpenSignIn_IsFound_ThenClosedBySignOut_AndOvernightDetected()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(factory, new SqlServerDialect()).RunAsync();

        var repo = new AttendanceRepository(factory);
        var person = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        try
        {
            var signIn = new AttendanceRecord
            {
                PersonId = person,
                SiteId = siteId,
                Type = AttendanceEventType.SignIn,
                Outcome = AttendanceOutcome.Accepted,
                OccurredUtc = DateTimeOffset.UtcNow.AddDays(-1),   // yesterday, still open → overnight
            };
            await repo.AddAsync(signIn);

            var open = await repo.GetOpenSignInForPersonAsync(person);
            Assert.NotNull(open);
            Assert.Equal(signIn.Id, open!.Id);

            var overnight = await repo.GetUnflaggedOvernightSignInsAsync(new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero));
            Assert.Contains(overnight, r => r.Id == signIn.Id);

            // A later sign-out closes the presence.
            await repo.AddAsync(new AttendanceRecord
            {
                PersonId = person,
                SiteId = siteId,
                Type = AttendanceEventType.SignOut,
                Outcome = AttendanceOutcome.Accepted,
                OccurredUtc = DateTimeOffset.UtcNow,
            });
            Assert.Null(await repo.GetOpenSignInForPersonAsync(person));
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM Attendance WHERE PersonId = @person", new { person });
        }
    }
}
