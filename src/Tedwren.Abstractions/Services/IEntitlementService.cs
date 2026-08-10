using Tedwren.Abstractions.Contracts.Entitlements;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The single authoritative answer to "has this customer bought this" (Q2). Checked server-side and
/// <b>fails closed</b> — an unknown module, or one with no positive entitlement, is not enabled. Drives
/// navigation showing only what the customer has bought (SF-22).
/// </summary>
public interface IEntitlementService
{
    /// <summary>Returns every catalogue module with the company's enabled state.</summary>
    Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Whether a specific module is enabled for the company. Fails closed for unknown modules (Q2).</summary>
    Task<bool> IsEnabledAsync(Guid companyId, string moduleKey, CancellationToken cancellationToken = default);

    /// <summary>Sets (upserts) a company's entitlement for a module. Unknown modules are ignored (Q2).</summary>
    Task SetEnabledAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default);
}
