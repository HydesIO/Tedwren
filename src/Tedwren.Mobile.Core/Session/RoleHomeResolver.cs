using Tedwren.Domain.Enums;

namespace Tedwren.Mobile.Core.Session;

/// <summary>
/// Maps an authenticated principal's role to its home experience — the role-switch that drives the two
/// distinct card menus and dashboards. Operatives carry the mobile <see cref="OperativeRole"/> claim; console
/// users carry an <see cref="AccessRole"/> name.
/// </summary>
public static class RoleHomeResolver
{
    /// <summary>The role string carried by an operative (mobile) token — operatives are not console <c>AccessRole</c> users.</summary>
    public const string OperativeRole = "Operative";

    /// <summary>
    /// Resolves the home for a role string. Operatives get the field home; console roles
    /// (Administrator / ComplianceManager / SiteManager / Auditor) get the manager home. An unknown or missing
    /// role falls back to the least-privileged operative home rather than the elevated manager surface.
    /// </summary>
    public static RoleHome Resolve(string? role)
    {
        if (string.Equals(role, OperativeRole, StringComparison.OrdinalIgnoreCase))
        {
            return RoleHome.Operative;
        }

        return Enum.TryParse<AccessRole>(role, ignoreCase: true, out _) ? RoleHome.Manager : RoleHome.Operative;
    }
}
