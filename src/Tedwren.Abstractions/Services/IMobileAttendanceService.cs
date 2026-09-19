using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Server-only read for an operative's live attendance state (M4): the site they are currently signed in at, if
/// any. Keyed by the PersonId resolved from the operative token (never client-supplied), so an operative only
/// ever sees their own state (R15). The sign-in/out actions themselves reuse the console
/// <see cref="IAttendanceService"/> — this service is only the current-state query the console does not need,
/// kept separate so <see cref="IAttendanceService"/> stays cohesive and the Blazor console is unaffected (SRP).
/// </summary>
public interface IMobileAttendanceService
{
    /// <summary>The operative's current open sign-in (site + since when, SF-18), or null when they are not signed in anywhere.</summary>
    Task<CurrentAttendanceDto?> GetCurrentAsync(Guid personId, CancellationToken cancellationToken = default);
}
