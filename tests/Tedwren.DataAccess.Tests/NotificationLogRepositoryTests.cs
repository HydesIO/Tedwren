using Dapper;
using Tedwren.Abstractions.Configuration;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Jobs;
using Tedwren.Domain.Notifications;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration tests for the expiry-log and job-run Dapper repositories against a real SQL Server. Run only
/// when TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and
/// deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class NotificationLogRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task NotificationLog_IsIdempotent_AndJobRunRoundTrips()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(factory, new SqlServerDialect()).RunAsync();

        var log = new NotificationLogRepository(factory);
        var runs = new JobRunRepository(factory);
        var cardId = Guid.NewGuid();
        var jobName = "test-" + Guid.NewGuid();
        const string recipient = "integration@test.local";

        try
        {
            Assert.False(await log.ExistsAsync(cardId, ExpiryWarningStage.ThirtyDays, NotificationChannel.Email, recipient));
            await log.AddAsync(new ExpiryNotification
            {
                CardId = cardId,
                Stage = ExpiryWarningStage.ThirtyDays,
                Channel = NotificationChannel.Email,
                Recipient = recipient,
            });
            Assert.True(await log.ExistsAsync(cardId, ExpiryWarningStage.ThirtyDays, NotificationChannel.Email, recipient));

            // SF-9: the unique index rejects a duplicate (card, stage, channel, recipient).
            await Assert.ThrowsAnyAsync<Exception>(() => log.AddAsync(new ExpiryNotification
            {
                CardId = cardId,
                Stage = ExpiryWarningStage.ThirtyDays,
                Channel = NotificationChannel.Email,
                Recipient = recipient,
            }));

            // SF-21: a job run records and its last success is retrievable.
            var run = new JobRun { JobName = jobName };
            await runs.AddAsync(run);
            run.Status = JobRunStatus.Succeeded;
            run.FinishedUtc = DateTimeOffset.UtcNow;
            run.ItemsProcessed = 3;
            await runs.UpdateAsync(run);

            var last = await runs.GetLastSuccessAsync(jobName);
            Assert.NotNull(last);
            Assert.Equal(3, last!.ItemsProcessed);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM ExpiryNotifications WHERE CardId = @cardId", new { cardId });
            await connection.ExecuteAsync("DELETE FROM JobRuns WHERE JobName = @jobName", new { jobName });
        }
    }
}
