using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for the timesheet (SUB-7). The header is stateful (its status changes through the
/// lifecycle) while its lines are append-only — a correction is a new line, never an edit (R16). All reads are
/// scoped by company so one company never sees another's timesheets (R15).
/// </summary>
public interface ITimesheetRepository
{
    /// <summary>Returns a timesheet header by id, or null.</summary>
    Task<Timesheet?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns an operative's timesheet header for a company and week, or null.</summary>
    Task<Timesheet?> GetByWeekAsync(Guid companyId, Guid personId, DateOnly weekStart, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's timesheet headers for a week — the valuation-period view (MC-24).</summary>
    Task<IReadOnlyList<Timesheet>> GetByCompanyWeekAsync(Guid companyId, DateOnly weekStart, CancellationToken cancellationToken = default);

    /// <summary>Returns all lines of a timesheet (including retained superseded lines, R16).</summary>
    Task<IReadOnlyList<TimesheetEntry>> GetEntriesAsync(Guid timesheetId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new timesheet with its initial rolled-up lines.</summary>
    Task AddAsync(Timesheet timesheet, IReadOnlyList<TimesheetEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>Appends a line (a correction) to an existing timesheet (R16).</summary>
    Task AddEntryAsync(TimesheetEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Persists the header's workflow changes (status, approval metadata).</summary>
    Task UpdateAsync(Timesheet timesheet, CancellationToken cancellationToken = default);
}
