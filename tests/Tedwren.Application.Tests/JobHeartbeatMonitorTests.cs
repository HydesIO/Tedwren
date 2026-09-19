using Tedwren.Application.Expiry;
using Tedwren.Application.Jobs;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Jobs;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>Verifies the job heartbeat (SF-21/R12): a job with no recent successful run is flagged to ops.</summary>
public sealed class JobHeartbeatMonitorTests
{
    private static (JobHeartbeatMonitor Monitor, InMemoryExpiryStore Store, NotificationOutbox Outbox) CreateSut()
    {
        var store = new InMemoryExpiryStore();
        var outbox = new NotificationOutbox();
        var monitor = new JobHeartbeatMonitor(
            new InMemoryJobRunRepository(store),
            new OutboxEmailSender(outbox),
            new ExpiryJobOptions());
        return (monitor, store, outbox);
    }

    [Fact]
    public async Task NoRunsEver_EveryScheduledJobAlerted()
    {
        var (monitor, _, outbox) = CreateSut();

        var alerts = await monitor.CheckAsync(DateTimeOffset.UtcNow);

        // All four monitored jobs never ran: expiry-scan, weekly-digest, form-reminder, overnight-check.
        Assert.Equal(4, alerts);
        Assert.Equal(4, outbox.Messages.Count);
        Assert.All(outbox.Messages, m => Assert.Equal("ops@tedwren.local", m.Recipient));
    }

    [Fact]
    public async Task RecentExpiryScanSuccess_OtherThreeAlerted()
    {
        var (monitor, store, outbox) = CreateSut();
        var now = DateTimeOffset.UtcNow;
        store.JobRuns[Guid.NewGuid()] = new JobRun
        {
            JobName = JobNames.ExpiryScan,
            Status = JobRunStatus.Succeeded,
            FinishedUtc = now.AddMinutes(-5),
        };

        var alerts = await monitor.CheckAsync(now);

        // Only the expiry scan is recent; digest, form-reminder and overnight-check are all overdue.
        Assert.Equal(3, alerts);
        Assert.DoesNotContain(outbox.Messages, m => m.Subject.Contains(JobNames.ExpiryScan));
        Assert.Contains(outbox.Messages, m => m.Subject.Contains(JobNames.WeeklyDigest));
        Assert.Contains(outbox.Messages, m => m.Subject.Contains(JobNames.FormReminder));
        Assert.Contains(outbox.Messages, m => m.Subject.Contains(JobNames.OvernightCheck));
    }

    [Fact]
    public async Task AllJobsRecentlySucceeded_NoAlerts()
    {
        var (monitor, store, outbox) = CreateSut();
        var now = DateTimeOffset.UtcNow;
        foreach (var jobName in new[] { JobNames.ExpiryScan, JobNames.WeeklyDigest, JobNames.FormReminder, JobNames.OvernightCheck })
        {
            store.JobRuns[Guid.NewGuid()] = new JobRun
            {
                JobName = jobName,
                Status = JobRunStatus.Succeeded,
                FinishedUtc = now.AddMinutes(-5),
            };
        }

        var alerts = await monitor.CheckAsync(now);

        Assert.Equal(0, alerts);
        Assert.Empty(outbox.Messages);
    }
}
