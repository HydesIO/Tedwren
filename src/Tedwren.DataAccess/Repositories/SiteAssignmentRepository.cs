using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ISiteAssignmentRepository"/> for user↔site assignments (MC-21/UAT-011).</summary>
public sealed class SiteAssignmentRepository : RepositoryBase, ISiteAssignmentRepository
{
    /// <summary>Creates the repository over the connection factory.</summary>
    public SiteAssignmentRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns the site ids assigned to a user.</summary>
    public async Task<IReadOnlyList<Guid>> GetSiteIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await QueryAsync<Guid>(
            "SELECT SiteId FROM SiteAssignments WHERE UserId = @UserId",
            new { UserId = userId }, cancellationToken);

    /// <summary>Replaces a user's assignments with the given set of sites (delete-then-insert).</summary>
    public async Task ReplaceForUserAsync(Guid userId, IReadOnlyList<Guid> siteIds, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync("DELETE FROM SiteAssignments WHERE UserId = @UserId", new { UserId = userId }, cancellationToken);
        foreach (var siteId in siteIds.Distinct())
        {
            await ExecuteAsync(
                "INSERT INTO SiteAssignments (Id, UserId, SiteId, CreatedUtc) VALUES (@Id, @UserId, @SiteId, @CreatedUtc)",
                new { Id = Guid.NewGuid(), UserId = userId, SiteId = siteId, CreatedUtc = DateTimeOffset.UtcNow },
                cancellationToken);
        }
    }
}
