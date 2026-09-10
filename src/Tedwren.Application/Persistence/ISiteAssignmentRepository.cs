using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for user↔site assignments (MC-21). Scopes which sites a site manager can see (UAT-011).
/// </summary>
public interface ISiteAssignmentRepository
{
    /// <summary>Returns the site ids assigned to a user.</summary>
    Task<IReadOnlyList<Guid>> GetSiteIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Replaces a user's assignments with the given set of sites (add/remove in one operation).</summary>
    Task ReplaceForUserAsync(Guid userId, IReadOnlyList<Guid> siteIds, CancellationToken cancellationToken = default);
}
