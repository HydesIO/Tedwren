namespace Tedwren.Application.Entitlements;

/// <summary>
/// The catalogue of modules a customer can hold (SF-22, Q2) — the single list of what exists, with each
/// module's default enabled state. Foundation modules default on; paid modules default off, so an
/// unpurchased module is simply absent (never advertised as a locked door). A company entitlement record
/// overrides the default.
/// </summary>
public static class ModuleCatalog
{
    /// <summary>A module in the catalogue.</summary>
    public sealed record CatalogModule(string Key, string Name, string Description, bool DefaultEnabled);

    /// <summary>All known modules.</summary>
    public static IReadOnlyList<CatalogModule> Modules { get; } = new List<CatalogModule>
    {
        new("workforce", "Workforce", "Operative register, profiles and onboarding", DefaultEnabled: true),
        new("compliance", "Compliance", "Qualification tracking and compliance packs", DefaultEnabled: true),
        new("inductions", "Inductions", "Video/quiz inductions and history", DefaultEnabled: true),
        new("time", "Time & Attendance", "Timesheets and corrections", DefaultEnabled: false),
        new("permits", "Permits", "Permit issuance and tracking", DefaultEnabled: true),
        new("reports", "Reports & Analytics", "Dashboards, reports and exports", DefaultEnabled: true),
        new("integrations", "Integrations", "Connected third-party services", DefaultEnabled: false),
        new("forms", "Forms Library", "Custom forms, submissions and assignments", DefaultEnabled: false),
    };

    /// <summary>Returns the catalogue module for a key, or null when the key is unknown (fails closed, Q2).</summary>
    public static CatalogModule? Find(string key) =>
        Modules.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));
}
