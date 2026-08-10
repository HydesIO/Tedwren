using Tedwren.Abstractions.Contracts.Entitlements;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// Deprecated in-proc <see cref="IEntitlementService"/> for the retained (test-only) client <c>Mock</c>
/// mode. Returns the catalogue defaults (foundation modules on; time and integrations off) so navigation
/// gating behaves like the API. The authoritative check is <see cref="ApiEntitlementService"/>.
/// </summary>
public sealed class ClientMockEntitlementService : IEntitlementService
{
    // Mirrors Application ModuleCatalog defaults (kept here so the client has no Application dependency).
    private static readonly IReadOnlyList<ModuleEntitlementDto> Modules = new List<ModuleEntitlementDto>
    {
        new("workforce", "Workforce", "Operative register, profiles and onboarding", true),
        new("compliance", "Compliance", "Qualification tracking and compliance packs", true),
        new("inductions", "Inductions", "Video/quiz inductions and history", true),
        new("time", "Time & Attendance", "Timesheets and corrections", false),
        new("permits", "Permits", "Permit issuance and tracking", true),
        new("reports", "Reports & Analytics", "Dashboards, reports and exports", true),
        new("integrations", "Integrations", "Connected third-party services", false),
    };

    /// <summary>Returns the catalogue with default enabled states.</summary>
    public Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Modules);

    /// <summary>Whether a module is enabled by default; unknown modules fail closed.</summary>
    public Task<bool> IsEnabledAsync(Guid companyId, string moduleKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Modules.FirstOrDefault(m => string.Equals(m.Key, moduleKey, StringComparison.OrdinalIgnoreCase))?.Enabled ?? false);
}
