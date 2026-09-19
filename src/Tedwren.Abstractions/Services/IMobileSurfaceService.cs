using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Workforce;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Server-only composition for the operative (mobile) read surface (M3): a single operative's own profile and
/// the company's sites with geofences. Keyed by ids resolved from the operative token (never client-supplied),
/// so an operative only ever reads their own record and their company's sites (R15). Kept separate from the
/// console-facing <see cref="IWorkforceService"/>/<see cref="ISiteService"/> so those stay cohesive (SRP).
/// </summary>
public interface IMobileSurfaceService
{
    /// <summary>Returns the operative's own profile, cards and compliance, or null when they have no engagement in the company.</summary>
    Task<OperativeDetailDto?> GetProfileAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Returns the company's sites each with geofence + dispersed properties, for offline sign-in caching (SF-14/SF-26).</summary>
    Task<IReadOnlyList<MobileSiteDto>> GetSitesAsync(Guid companyId, CancellationToken cancellationToken = default);
}
