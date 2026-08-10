using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Jobs;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IJobRunRepository"/>. Paging uses ANSI <c>OFFSET … FETCH</c> (supported by both SQL
/// Server and PostgreSQL); the fetch count is an internal, clamped integer, so it is inlined safely.
/// </summary>
public sealed class JobRunRepository : RepositoryBase, IJobRunRepository
{
    private const string Columns =
        "Id, JobName, StartedUtc, FinishedUtc, Status, ItemsProcessed, NotificationsSent, Error";

    /// <summary>Creates the repository over the connection factory.</summary>
    public JobRunRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Inserts a newly-started run.</summary>
    public Task AddAsync(JobRun run, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO JobRuns (Id, JobName, StartedUtc, FinishedUtc, Status, ItemsProcessed, NotificationsSent, Error) " +
            "VALUES (@Id, @JobName, @StartedUtc, @FinishedUtc, @Status, @ItemsProcessed, @NotificationsSent, @Error)",
            ToParameters(run), cancellationToken);

    /// <summary>Updates a run's completion fields.</summary>
    public Task UpdateAsync(JobRun run, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE JobRuns SET FinishedUtc = @FinishedUtc, Status = @Status, ItemsProcessed = @ItemsProcessed, " +
            "NotificationsSent = @NotificationsSent, Error = @Error WHERE Id = @Id",
            ToParameters(run), cancellationToken);

    /// <summary>Returns the most recent runs, newest first.</summary>
    public async Task<IReadOnlyList<JobRun>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 500);
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM JobRuns ORDER BY StartedUtc DESC OFFSET 0 ROWS FETCH NEXT {limit} ROWS ONLY",
            null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns the last successful run of a named job, or null.</summary>
    public async Task<JobRun?> GetLastSuccessAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM JobRuns WHERE JobName = @JobName AND Status = @Status " +
            "ORDER BY FinishedUtc DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY",
            new { JobName = jobName, Status = (int)JobRunStatus.Succeeded }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Builds the parameter object shared by insert and update.</summary>
    private static object ToParameters(JobRun r) => new
    {
        r.Id,
        r.JobName,
        r.StartedUtc,
        r.FinishedUtc,
        Status = (int)r.Status,
        r.ItemsProcessed,
        r.NotificationsSent,
        r.Error,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static JobRun ToEntity(Row r) => new()
    {
        Id = r.Id,
        JobName = r.JobName,
        StartedUtc = r.StartedUtc,
        FinishedUtc = r.FinishedUtc,
        Status = (JobRunStatus)r.Status,
        ItemsProcessed = r.ItemsProcessed,
        NotificationsSent = r.NotificationsSent,
        Error = r.Error,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string JobName, DateTimeOffset StartedUtc, DateTimeOffset? FinishedUtc,
        int Status, int ItemsProcessed, int NotificationsSent, string? Error);
}
