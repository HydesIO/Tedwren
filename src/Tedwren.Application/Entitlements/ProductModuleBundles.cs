using Tedwren.Domain.Enums;

namespace Tedwren.Application.Entitlements;

/// <summary>
/// The default module bundle each product ships with (PRD §2 "two products, sold separately"; SF-22). This is
/// the strict PRD split: a company's product decides which catalogue modules are on by default, so a
/// subcontractor and a main contractor get materially different consoles out of the box. A per-company
/// entitlement override (via <c>AdminCompanyModules</c>) still wins over the product default; a company with
/// no product falls back to the catalogue's own <see cref="ModuleCatalog.CatalogModule.DefaultEnabled"/>.
/// </summary>
public static class ProductModuleBundles
{
    /// <summary>Subcontractor product: time &amp; attendance + the compliance pack (send). No induction (§6.1, SUB-11).</summary>
    private static readonly HashSet<string> Subcontractor = new(StringComparer.OrdinalIgnoreCase)
    {
        "workforce", "compliance", "time", "reports",
    };

    /// <summary>Main contractor product: workforce management + inductions + the site-entry decision; receives packs (MC-19).</summary>
    private static readonly HashSet<string> MainContractor = new(StringComparer.OrdinalIgnoreCase)
    {
        "workforce", "compliance", "inductions", "reports",
    };

    /// <summary>
    /// Whether <paramref name="moduleKey"/> is on by default for the given product. <c>permits</c>, <c>forms</c>
    /// and <c>integrations</c> are off for both products by default — purchasable per company via an override.
    /// </summary>
    public static bool IsDefaultEnabled(OrgType orgType, string moduleKey) => orgType switch
    {
        OrgType.Subcontractor => Subcontractor.Contains(moduleKey),
        OrgType.MainContractor => MainContractor.Contains(moduleKey),
        _ => false,
    };
}
