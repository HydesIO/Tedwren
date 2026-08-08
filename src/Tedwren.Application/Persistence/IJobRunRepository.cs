using Tedwren.Domain.Jobs;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for scheduled-job run records (SF-21, R12).</summary>
public interface IJobRunRepository
{
    /// <summary>Records a newly-started run.</summary>
    Task AddAsync(JobRun run, CancellationToken cancellationToken = default);

    /// <summary>Persists a run's completion (status, counts, error, finished time).</summary>
    Task UpdateAsync(JobRun run, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent runs, newest first.</summary>
    Task<IReadOnlyList<JobRun>> GetRecentAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>Returns the last successful run of a named job, or null (for the heartbeat check, R12).</summary>
    Task<JobRun?> GetLastSuccessAsync(string jobName, CancellationToken cancellationToken = default);
}
