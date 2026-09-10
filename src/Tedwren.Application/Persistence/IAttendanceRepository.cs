using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for the append-only attendance log (SF-13–SF-19, R4). Records are only added.
/// "Open" means a presence-counting sign-in with no later sign-out at the same site for the same person.
/// </summary>
public interface IAttendanceRepository
{
    /// <summary>Appends an attendance record.</summary>
    Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes an attendance record by id (used only by the demo-data teardown).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the person's current open sign-in anywhere, or null (the SF-18 cross-site check, Q4).</summary>
    Task<AttendanceRecord?> GetOpenSignInForPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Returns the person's current open sign-in at a specific site, or null (for sign-out).</summary>
    Task<AttendanceRecord?> GetOpenSignInForPersonAtSiteAsync(Guid personId, Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>Returns the open sign-ins at a site — the workers currently present (MC-12 foundation).</summary>
    Task<IReadOnlyList<AttendanceRecord>> GetOnSiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>Returns open sign-ins older than the cutoff that have not yet been flagged overnight (SF-19).</summary>
    Task<IReadOnlyList<AttendanceRecord>> GetUnflaggedOvernightSignInsAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent records for a site, newest first (the append-only log).</summary>
    Task<IReadOnlyList<AttendanceRecord>> GetBySiteAsync(Guid siteId, int take, CancellationToken cancellationToken = default);

    /// <summary>Returns a person's records occurring in <c>[fromUtc, toUtc)</c>, oldest first — the source for the weekly timesheet roll-up (SUB-7).</summary>
    Task<IReadOnlyList<AttendanceRecord>> GetByPersonInRangeAsync(Guid personId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
