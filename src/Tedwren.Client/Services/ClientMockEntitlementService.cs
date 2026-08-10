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
    // Static + mutable so a toggle persists for the browser session, matching the API's set behaviour.
    private static readonly Dictionary<string, ModuleEntitlementDto> Modules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["workforce"] = new("workforce", "Workforce", "Operative register, profiles and onboarding", true),
        ["compliance"] = new("compliance", "Compliance", "Qualification tracking and compliance packs", true),
        ["inductions"] = new("inductions", "Inductions", "Video/quiz inductions and history", true),
        ["time"] = new("time", "Time & Attendance", "Timesheets and corrections", false),
        ["permits"] = new("permits", "Permits", "Permit issuance and tracking", true),
        ["reports"] = new("reports", "Reports & Analytics", "Dashboards, reports and exports", true),
        ["integrations"] = new("integrations", "Integrations", "Connected third-party services", false),
    };

    /// <summary>Returns the catalogue with current enabled states.</summary>
    public Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ModuleEntitlementDto>>(Modules.Values.ToList());

    /// <summary>Whether a module is enabled; unknown modules fail closed.</summary>
    public Task<bool> IsEnabledAsync(Guid companyId, string moduleKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Modules.TryGetValue(moduleKey, out var m) && m.Enabled);

    /// <summary>Sets a module's enabled state for the session.</summary>
    public Task SetEnabledAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default)
    {
        if (Modules.TryGetValue(moduleKey, out var m))
        {
            Modules[moduleKey] = m with { Enabled = enabled };
        }

        return Task.CompletedTask;
    }
}
