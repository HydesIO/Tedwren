using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IAttendanceRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryAttendanceRepository : IAttendanceRepository
{
    private readonly InMemoryAttendanceStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryAttendanceRepository(InMemoryAttendanceStore store) => _store = store;

    /// <summary>Appends an attendance record.</summary>
    public Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default)
    {
        _store.Records[record.Id] = record;
        return Task.CompletedTask;
    }

    /// <summary>Returns the person's current open sign-in anywhere, or null.</summary>
    public Task<AttendanceRecord?> GetOpenSignInForPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        Task.FromResult(OpenSignIns().FirstOrDefault(r => r.PersonId == personId));

    /// <summary>Returns the person's current open sign-in at a site, or null.</summary>
    public Task<AttendanceRecord?> GetOpenSignInForPersonAtSiteAsync(Guid personId, Guid siteId, CancellationToken cancellationToken = default) =>
        Task.FromResult(OpenSignIns().FirstOrDefault(r => r.PersonId == personId && r.SiteId == siteId));

    /// <summary>Returns the open sign-ins at a site.</summary>
    public Task<IReadOnlyList<AttendanceRecord>> GetOnSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AttendanceRecord> present = OpenSignIns().Where(r => r.SiteId == siteId).ToList();
        return Task.FromResult(present);
    }

    /// <summary>Returns open sign-ins older than the cutoff that have not yet been flagged overnight.</summary>
    public Task<IReadOnlyList<AttendanceRecord>> GetUnflaggedOvernightSignInsAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
    {
        var flagged = _store.Records.Values
            .Where(r => r.Type == AttendanceEventType.OvernightFlag && r.CorrectsRecordId is not null)
            .Select(r => r.CorrectsRecordId!.Value)
            .ToHashSet();

        IReadOnlyList<AttendanceRecord> overnight = OpenSignIns()
            .Where(r => r.OccurredUtc < cutoffUtc && !flagged.Contains(r.Id))
            .ToList();
        return Task.FromResult(overnight);
    }

    /// <summary>Returns the most recent records for a site, newest first.</summary>
    public Task<IReadOnlyList<AttendanceRecord>> GetBySiteAsync(Guid siteId, int take, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AttendanceRecord> records = _store.Records.Values
            .Where(r => r.SiteId == siteId)
            .OrderByDescending(r => r.OccurredUtc)
            .Take(take)
            .ToList();
        return Task.FromResult(records);
    }

    /// <summary>
    /// The currently-open sign-ins: presence-counting sign-ins with no later sign-out at the same site for
    /// the same person. Newest first.
    /// </summary>
    private IEnumerable<AttendanceRecord> OpenSignIns()
    {
        var signOuts = _store.Records.Values
            .Where(r => r.Type == AttendanceEventType.SignOut && r.CountsForPresence)
            .ToList();

        return _store.Records.Values
            .Where(r => r.Type == AttendanceEventType.SignIn && r.CountsForPresence)
            .Where(signIn => !signOuts.Any(so =>
                so.PersonId == signIn.PersonId && so.SiteId == signIn.SiteId && so.OccurredUtc > signIn.OccurredUtc))
            .OrderByDescending(r => r.OccurredUtc);
    }
}
