namespace Tedwren.Application.Entitlements;

/// <summary>
/// The catalogue of modules a customer can hold (SF-22, Q2) — the single list of what exists, with each
/// module's default enabled state. These defaults are only the fallback for a company with no product
/// (<c>OrgType == null</c>); a company with a product takes its defaults from <see cref="ProductModuleBundles"/>.
/// The fallback is therefore limited to the shared foundation (workforce, compliance, reports): induction is
/// a main-contractor engine (§6.1, SUB-11) and permits is a paid add-on off for both products, so neither
/// may default on here — otherwise a product-less company re-acquires the very cross-product modules the
/// two-product split removes. A company entitlement record overrides the default.
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
        new("inductions", "Inductions", "Video/quiz inductions and history", DefaultEnabled: false),
        new("time", "Time & Attendance", "Timesheets and corrections", DefaultEnabled: false),
        new("permits", "Permits", "Permit issuance and tracking", DefaultEnabled: false),
        new("reports", "Reports & Analytics", "Dashboards, reports and exports", DefaultEnabled: true),
        new("integrations", "Integrations", "Connected third-party services", DefaultEnabled: false),
        new("forms", "Forms Library", "Custom forms, submissions and assignments", DefaultEnabled: false),
        new("cscs", "CSCS Verification", "Live CSCS Smart Check card verification (paid add-on)", DefaultEnabled: false),
    };

    /// <summary>Returns the catalogue module for a key, or null when the key is unknown (fails closed, Q2).</summary>
    public static CatalogModule? Find(string key) =>
        Modules.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));
}
