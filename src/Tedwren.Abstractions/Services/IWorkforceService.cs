using Tedwren.Abstractions.Contracts.Workforce;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The org-wide workforce read model: lists every active operative across all engaged companies and serves
/// a single operative's profile. Composes the person, engagement, qualification-card and decision data into
/// display shapes so the Workforce register and operative detail render real database data.
/// </summary>
public interface IWorkforceService
{
    /// <summary>Returns every active operative (one row per active engagement), with compliance and next expiry.</summary>
    Task<IReadOnlyList<OperativeListItemDto>> ListOperativesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns an operative's full profile by slug, or null when no active operative matches.</summary>
    Task<OperativeDetailDto?> GetOperativeBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an operative's full profile by the engaging company and engagement id, or null when none matches.
    /// This is how the Organisation page opens an operative: that operative may belong to a company other than
    /// the signed-in tenant, which the tenant-scoped slug lookup cannot reach (the cause of the "no operative
    /// found" error). Scoped by the supplied company id at the repository (R15).
    /// </summary>
    Task<OperativeDetailDto?> GetOperativeByEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default);
}
