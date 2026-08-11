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
}
