using MudBlazor;
using Tedwren.UiComponents.Navigation;

namespace Tedwren.Client.Services;

/// <summary>
/// Static application shell chrome — the navigation/route inventory, the platform switcher list and the
/// environment badge. This is fixed app configuration (not tenant data), so it lives in the client rather
/// than behind the API. Replaces the shell parts of the former sample-data service.
/// </summary>
public static class ShellChrome
{
    /// <summary>The sidebar navigation / route inventory.</summary>
    public static IReadOnlyList<NavItem> NavItems { get; } = new List<NavItem>
    {
        new("Dashboard",           Icons.Material.Outlined.SpaceDashboard, "/"),
        new("Organisation",        Icons.Material.Outlined.Business,       "/organisation"),
        new("Users",               Icons.Material.Outlined.PeopleOutline,  "/users"),
        new("Workforce",           Icons.Material.Outlined.Engineering,    "/workforce"),
        new("Sites & Projects",    Icons.Material.Outlined.Apartment,      "/sites"),
        new("Site Gate",           Icons.Material.Outlined.Login,          "/site-gate"),
        new("Attendance",          Icons.Material.Outlined.HowToReg,       "/attendance"),
        new("Compliance",          Icons.Material.Outlined.VerifiedUser,   "/compliance"),
        new("Compliance Packs",    Icons.Material.Outlined.FolderShared,   "/compliance-packs"),
        new("Inductions",          Icons.Material.Outlined.PlayCircle,     "/inductions"),
        new("Induction Records",   Icons.Material.Outlined.FactCheck,      "/induction-records"),
        new("Time & Attendance",   Icons.Material.Outlined.Schedule,       "/time-attendance"),
        new("Permits",             Icons.Material.Outlined.Assignment,     "/permits"),
        new("Reports & Analytics", Icons.Material.Outlined.BarChart,       "/reports"),
        new("Integrations",        Icons.Material.Outlined.Hub,            "/integrations"),
        new("System Configuration",Icons.Material.Outlined.Settings,       "/system-configuration"),
        new("Audit Log",           Icons.Material.Outlined.History,        "/audit-log"),
    };

    /// <summary>The platform-switcher options.</summary>
    public static IReadOnlyList<string> Platforms { get; } = new[] { "Main Contractor", "Subcontractor" };

    /// <summary>The default selected platform.</summary>
    public static string DefaultPlatform => Platforms[0];

    /// <summary>The environment badge shown in the sidebar footer.</summary>
    public static (string Name, string Version, string Build, bool IsHealthy) Environment { get; }
        = ("Production", "1.0.0", "1042", true);
}
