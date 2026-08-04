using MudBlazor;
using Tedwren.UiComponents.Navigation;

namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// In-memory implementation of <see cref="IShellSampleDataService"/>. The nav list mirrors
/// the sidebar / route inventory in the Plan &amp; Scope (§3, §7). Icons use MudBlazor's
/// outline set to match the dashboard's iconography.
/// </summary>
public sealed class ShellSampleDataService : IShellSampleDataService
{
    private static readonly IReadOnlyList<NavItem> Nav = new List<NavItem>
    {
        new("Dashboard",           Icons.Material.Outlined.SpaceDashboard, "/"),
        new("Organisation",        Icons.Material.Outlined.Business,       "/organisation"),
        new("Users",               Icons.Material.Outlined.PeopleOutline,  "/users"),
        new("Workforce",           Icons.Material.Outlined.Engineering,    "/workforce"),
        new("Sites & Projects",    Icons.Material.Outlined.Apartment,      "/sites"),
        new("Compliance",          Icons.Material.Outlined.VerifiedUser,   "/compliance"),
        new("Inductions",          Icons.Material.Outlined.PlayCircle,     "/inductions"),
        new("Time & Attendance",   Icons.Material.Outlined.Schedule,       "/time-attendance"),
        new("Permits",             Icons.Material.Outlined.Assignment,     "/permits"),
        new("Reports & Analytics", Icons.Material.Outlined.BarChart,       "/reports"),
        new("Integrations",        Icons.Material.Outlined.Hub,            "/integrations"),
        new("System Configuration",Icons.Material.Outlined.Settings,       "/system-configuration"),
        new("Audit Log",           Icons.Material.Outlined.History,        "/audit-log"),
    };

    private static readonly IReadOnlyList<string> Platforms = new[]
    {
        "Main Contractor",
        "Subcontractor",
    };

    public IReadOnlyList<NavItem> GetNavItems() => Nav;

    public IReadOnlyList<string> GetPlatforms() => Platforms;

    public string GetSelectedPlatform() => Platforms[0];

    public ShellUser GetCurrentUser() => new("Alex Morgan", "Compliance Manager");

    public ShellEnvironment GetEnvironment() => new("Production", "1.0.0", "1042", IsHealthy: true);

    public int GetNotificationCount() => 3;
}
