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
    public async Task NoRunsEver_BothJobsAlerted()
    {
        var (monitor, _, outbox) = CreateSut();

        var alerts = await monitor.CheckAsync(DateTimeOffset.UtcNow);

        Assert.Equal(2, alerts);   // expiry-scan + weekly-digest both never ran
        Assert.Equal(2, outbox.Messages.Count);
        Assert.All(outbox.Messages, m => Assert.Equal("ops@tedwren.local", m.Recipient));
    }

    [Fact]
    public async Task RecentExpiryScanSuccess_OnlyDigestAlerted()
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

        Assert.Equal(1, alerts);   // only the weekly digest is overdue
        Assert.Contains(outbox.Messages, m => m.Subject.Contains("weekly-digest"));
    }
}
