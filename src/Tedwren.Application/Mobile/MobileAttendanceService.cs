using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Mobile;

/// <summary>
/// Reads an operative's live attendance state (M4) from the append-only log. Wraps
/// <see cref="IAttendanceRepository.GetOpenSignInForPersonAsync"/> (the SF-18 cross-site check) and resolves the
/// site name for display. The PersonId comes from the operative token via the endpoint, so this only ever
/// surfaces the operative's own state (R15).
/// </summary>
public sealed class MobileAttendanceService : IMobileAttendanceService
{
    private readonly IAttendanceRepository _attendance;
    private readonly ISiteRepository _sites;

    /// <summary>Creates the service over the attendance + site repositories.</summary>
    public MobileAttendanceService(IAttendanceRepository attendance, ISiteRepository sites)
    {
        _attendance = attendance;
        _sites = sites;
    }

    /// <summary>The operative's current open sign-in, or null when they are not signed in anywhere.</summary>
    public async Task<CurrentAttendanceDto?> GetCurrentAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var open = await _attendance.GetOpenSignInForPersonAsync(personId, cancellationToken);
        if (open is null)
        {
            return null;
        }

        var site = await _sites.GetByIdAsync(open.SiteId, cancellationToken);
        return new CurrentAttendanceDto(open.SiteId, site?.Name ?? "Unknown site", open.PropertyId, open.OccurredUtc);
    }
}
