using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IEntitlementRepository"/> for per-company module overrides (Q2).</summary>
public sealed class EntitlementRepository : RepositoryBase, IEntitlementRepository
{
    /// <summary>Creates the repository over the connection factory.</summary>
    public EntitlementRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Removes all module-entitlement overrides for a company (demo-data teardown).</summary>
    public Task ClearForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM ModuleEntitlements WHERE CompanyId = @CompanyId", new { CompanyId = companyId }, cancellationToken);

    /// <summary>Returns the company's module overrides.</summary>
    public async Task<IReadOnlyDictionary<string, bool>> GetOverridesByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            "SELECT ModuleKey, Enabled FROM ModuleEntitlements WHERE CompanyId = @CompanyId",
            new { CompanyId = companyId }, cancellationToken);
        return rows.ToDictionary(r => r.ModuleKey, r => r.Enabled, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Upserts a company's entitlement for a module (update-then-insert; portable across engines).</summary>
    public async Task SetAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var updated = await ExecuteAsync(
            "UPDATE ModuleEntitlements SET Enabled = @Enabled, UpdatedUtc = @UpdatedUtc " +
            "WHERE CompanyId = @CompanyId AND ModuleKey = @ModuleKey",
            new { CompanyId = companyId, ModuleKey = moduleKey, Enabled = enabled, UpdatedUtc = now }, cancellationToken);

        if (updated == 0)
        {
            await ExecuteAsync(
                "INSERT INTO ModuleEntitlements (Id, CompanyId, ModuleKey, Enabled, UpdatedUtc) " +
                "VALUES (@Id, @CompanyId, @ModuleKey, @Enabled, @UpdatedUtc)",
                new { Id = Guid.NewGuid(), CompanyId = companyId, ModuleKey = moduleKey, Enabled = enabled, UpdatedUtc = now },
                cancellationToken);
        }
    }

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(string ModuleKey, bool Enabled);
}
