using Tedwren.Application.Persistence;
using Tedwren.Domain.Jobs;

namespace Tedwren.Application.Jobs;

/// <summary>
/// Runs a scheduled job while recording a <see cref="JobRun"/> — a Running record on start, updated to
/// Succeeded (with item/notification counts) or Failed (with the error) on completion (SF-21, R12). This is
/// what makes a job's execution observable and a silent stop detectable.
/// </summary>
public sealed class JobRunner
{
    private readonly IJobRunRepository _runs;

    /// <summary>Creates the runner over the job-run repository.</summary>
    public JobRunner(IJobRunRepository runs) => _runs = runs;

    /// <summary>
    /// Executes <paramref name="work"/> under a recorded run named <paramref name="jobName"/>. The work
    /// returns the number of items processed and notifications sent. Failures are recorded and rethrown.
    /// </summary>
    public async Task<JobRun> RunAsync(
        string jobName,
        Func<CancellationToken, Task<(int Items, int Notifications)>> work,
        CancellationToken cancellationToken = default)
    {
        var run = new JobRun { JobName = jobName };
        await _runs.AddAsync(run, cancellationToken);

        try
        {
            var (items, notifications) = await work(cancellationToken);
            run.ItemsProcessed = items;
            run.NotificationsSent = notifications;
            run.Status = JobRunStatus.Succeeded;
        }
        catch (Exception ex)
        {
            run.Status = JobRunStatus.Failed;
            run.Error = ex.Message;
            run.FinishedUtc = DateTimeOffset.UtcNow;
            await _runs.UpdateAsync(run, cancellationToken);
            throw;
        }

        run.FinishedUtc = DateTimeOffset.UtcNow;
        await _runs.UpdateAsync(run, cancellationToken);
        return run;
    }
}
