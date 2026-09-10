namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for per-company module entitlement overrides (Q2). A missing override means the
/// catalogue default applies. Scoped by company (R15).
/// </summary>
public interface IEntitlementRepository
{
    /// <summary>Returns the company's module overrides as a key→enabled map (only modules explicitly set).</summary>
    Task<IReadOnlyDictionary<string, bool>> GetOverridesByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Sets (upserts) a company's entitlement for a module.</summary>
    Task SetAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Removes all module-entitlement overrides for a company (used only by the demo-data teardown).</summary>
    Task ClearForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
