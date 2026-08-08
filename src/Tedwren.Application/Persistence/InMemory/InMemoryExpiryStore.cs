using System.Collections.Concurrent;
using Tedwren.Domain.Jobs;
using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock expiry repositories (API <c>DataSource=Mock</c>): the sent-
/// notification log (idempotency) and the job-run history. Registered as a singleton so both repositories
/// share one dataset across requests.
/// </summary>
public sealed class InMemoryExpiryStore
{
    /// <summary>The sent-warning log (SF-9 idempotency).</summary>
    public ConcurrentBag<ExpiryNotification> Notifications { get; } = new();

    /// <summary>Job-run records by id (SF-21).</summary>
    public ConcurrentDictionary<Guid, JobRun> JobRuns { get; } = new();
}
