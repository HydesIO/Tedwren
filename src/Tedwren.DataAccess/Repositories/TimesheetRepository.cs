using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="ITimesheetRepository"/> (SUB-7). The header is stateful; its lines are append-only, so a
/// correction is inserted as a new row referencing the original (R16). SQL is ANSI-portable across both
/// engines; enum columns store the numeric value.
/// </summary>
public sealed class TimesheetRepository : RepositoryBase, ITimesheetRepository
{
    private const string HeaderColumns =
        "Id, CompanyId, PersonId, OperativeName, WeekStart, Status, SubmittedUtc, DecidedBy, DecidedUtc, DecisionScope, ReturnReason, CreatedUtc";

    private const string EntryColumns =
        "Id, TimesheetId, WorkDate, SiteId, SiteName, Hours, CorrectsEntryId, Author, Reason, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public TimesheetRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a timesheet header by id, or null.</summary>
    public async Task<Timesheet?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<HeaderRow>(
            $"SELECT {HeaderColumns} FROM Timesheets WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToHeader(row);
    }

    /// <summary>Returns an operative's timesheet header for a company and week, or null.</summary>
    public async Task<Timesheet?> GetByWeekAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<HeaderRow>(
            $"SELECT {HeaderColumns} FROM Timesheets WHERE CompanyId = @CompanyId AND PersonId = @PersonId AND WeekStart = @WeekStart",
            new { CompanyId = companyId, PersonId = personId, WeekStart = weekStart }, cancellationToken);
        return row is null ? null : ToHeader(row);
    }

    /// <summary>Returns a company's timesheet headers for a week (MC-24).</summary>
    public async Task<IReadOnlyList<Timesheet>> GetByCompanyWeekAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<HeaderRow>(
            $"SELECT {HeaderColumns} FROM Timesheets WHERE CompanyId = @CompanyId AND WeekStart = @WeekStart ORDER BY OperativeName",
            new { CompanyId = companyId, WeekStart = weekStart }, cancellationToken);
        return rows.Select(ToHeader).ToList();
    }

    /// <summary>Returns all lines of a timesheet, oldest first (retained superseded lines included, R16).</summary>
    public async Task<IReadOnlyList<TimesheetEntry>> GetEntriesAsync(Guid timesheetId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<EntryRow>(
            $"SELECT {EntryColumns} FROM TimesheetEntries WHERE TimesheetId = @TimesheetId ORDER BY CreatedUtc",
            new { TimesheetId = timesheetId }, cancellationToken);
        return rows.Select(ToEntry).ToList();
    }

    /// <summary>Persists a new timesheet with its initial rolled-up lines.</summary>
    public async Task AddAsync(Timesheet timesheet, IReadOnlyList<TimesheetEntry> entries, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            $"INSERT INTO Timesheets ({HeaderColumns}) VALUES " +
            "(@Id, @CompanyId, @PersonId, @OperativeName, @WeekStart, @Status, @SubmittedUtc, @DecidedBy, @DecidedUtc, @DecisionScope, @ReturnReason, @CreatedUtc)",
            ToHeaderParameters(timesheet), cancellationToken);

        foreach (var entry in entries)
        {
            await AddEntryAsync(entry, cancellationToken);
        }
    }

    /// <summary>Appends a line (a correction) to an existing timesheet (R16).</summary>
    public Task AddEntryAsync(TimesheetEntry entry, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"INSERT INTO TimesheetEntries ({EntryColumns}) VALUES " +
            "(@Id, @TimesheetId, @WorkDate, @SiteId, @SiteName, @Hours, @CorrectsEntryId, @Author, @Reason, @CreatedUtc)",
            new
            {
                entry.Id, entry.TimesheetId, entry.WorkDate, entry.SiteId, entry.SiteName, entry.Hours,
                entry.CorrectsEntryId, entry.Author, entry.Reason, entry.CreatedUtc,
            }, cancellationToken);

    /// <summary>Persists the header's workflow changes (status, approval metadata).</summary>
    public Task UpdateAsync(Timesheet timesheet, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Timesheets SET Status = @Status, SubmittedUtc = @SubmittedUtc, DecidedBy = @DecidedBy, " +
            "DecidedUtc = @DecidedUtc, DecisionScope = @DecisionScope, ReturnReason = @ReturnReason WHERE Id = @Id",
            ToHeaderParameters(timesheet), cancellationToken);

    /// <summary>Flattens a header to Dapper parameters (enums as ints).</summary>
    private static object ToHeaderParameters(Timesheet t) => new
    {
        t.Id,
        t.CompanyId,
        t.PersonId,
        t.OperativeName,
        t.WeekStart,
        Status = (int)t.Status,
        t.SubmittedUtc,
        t.DecidedBy,
        t.DecidedUtc,
        DecisionScope = t.DecisionScope is { } scope ? (int?)scope : null,
        t.ReturnReason,
        t.CreatedUtc,
    };

    /// <summary>Maps a header row to the domain entity.</summary>
    private static Timesheet ToHeader(HeaderRow r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        PersonId = r.PersonId,
        OperativeName = r.OperativeName,
        WeekStart = r.WeekStart,
        Status = (TimesheetStatus)r.Status,
        SubmittedUtc = r.SubmittedUtc,
        DecidedBy = r.DecidedBy,
        DecidedUtc = r.DecidedUtc,
        DecisionScope = r.DecisionScope is { } scope ? (ApprovalScope)scope : null,
        ReturnReason = r.ReturnReason,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Maps an entry row to the domain entity.</summary>
    private static TimesheetEntry ToEntry(EntryRow r) => new()
    {
        Id = r.Id,
        TimesheetId = r.TimesheetId,
        WorkDate = r.WorkDate,
        SiteId = r.SiteId,
        SiteName = r.SiteName,
        Hours = r.Hours,
        CorrectsEntryId = r.CorrectsEntryId,
        Author = r.Author,
        Reason = r.Reason,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat header row shape Dapper maps query results into.</summary>
    private sealed record HeaderRow(
        Guid Id, Guid CompanyId, Guid PersonId, string OperativeName, DateOnly WeekStart, int Status,
        DateTimeOffset? SubmittedUtc, string? DecidedBy, DateTimeOffset? DecidedUtc, int? DecisionScope,
        string? ReturnReason, DateTimeOffset CreatedUtc);

    /// <summary>Flat entry row shape Dapper maps query results into.</summary>
    private sealed record EntryRow(
        Guid Id, Guid TimesheetId, DateOnly WorkDate, Guid SiteId, string SiteName, decimal Hours,
        Guid? CorrectsEntryId, string? Author, string? Reason, DateTimeOffset CreatedUtc);
}
