using Tedwren.Domain.Jobs;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IJobRunRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryJobRunRepository : IJobRunRepository
{
    private readonly InMemoryExpiryStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryJobRunRepository(InMemoryExpiryStore store) => _store = store;

    /// <summary>Records a newly-started run.</summary>
    public Task AddAsync(JobRun run, CancellationToken cancellationToken = default)
    {
        _store.JobRuns[run.Id] = run;
        return Task.CompletedTask;
    }

    /// <summary>Persists a run's completion.</summary>
    public Task UpdateAsync(JobRun run, CancellationToken cancellationToken = default)
    {
        _store.JobRuns[run.Id] = run;
        return Task.CompletedTask;
    }

    /// <summary>Returns the most recent runs, newest first.</summary>
    public Task<IReadOnlyList<JobRun>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<JobRun> runs = _store.JobRuns.Values
            .OrderByDescending(r => r.StartedUtc)
            .Take(take)
            .ToList();
        return Task.FromResult(runs);
    }

    /// <summary>Returns the last successful run of a named job, or null.</summary>
    public Task<JobRun?> GetLastSuccessAsync(string jobName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.JobRuns.Values
            .Where(r => r.JobName == jobName && r.Status == JobRunStatus.Succeeded)
            .OrderByDescending(r => r.FinishedUtc)
            .FirstOrDefault());
}
