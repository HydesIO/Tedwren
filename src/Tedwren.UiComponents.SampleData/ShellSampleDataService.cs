using MudBlazor;
using Tedwren.UiComponents.Models;
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

    public int GetNotificationCount() => Notifications.Count(n => n.Unread);

    public IReadOnlyList<NotificationEntry> GetNotifications() => Notifications;

    private static readonly IReadOnlyList<NotificationEntry> Notifications = new List<NotificationEntry>
    {
        new(Icons.Material.Outlined.WarningAmber, "Qualification expiring", "CSCS card for J. Fletcher expires in 5 days", "12m ago", StatusKind.Warning, Unread: true),
        new(Icons.Material.Outlined.Block, "Site entry blocked", "Non-compliant operative at Kingsway Retail", "48m ago", StatusKind.Danger, Unread: true),
        new(Icons.Material.Outlined.Assignment, "Permit issued", "Hot works permit #HW-2043 at Meridian Tower", "2h ago", StatusKind.Permit, Unread: true),
        new(Icons.Material.Outlined.PersonAddAlt, "New operative onboarded", "M. Adeyemi joined via self-service link", "3h ago", StatusKind.Success),
        new(Icons.Material.Outlined.UploadFile, "Document uploaded", "RAMS submitted for Harbour Point", "5h ago", StatusKind.Info),
        new(Icons.Material.Outlined.VerifiedUser, "Compliance restored", "Riverside Civils back above 90% compliant", "Yesterday", StatusKind.Success),
        new(Icons.Material.Outlined.Schedule, "Timesheet approved", "Week 31 timesheet approved for Riverside Phase 2", "Yesterday", StatusKind.Info),
        new(Icons.Material.Outlined.WarningAmber, "Qualification expiring", "First Aid for S. Okoro expires in 12 days", "Yesterday", StatusKind.Warning),
        new(Icons.Material.Outlined.Hub, "Integration sync", "Sage export completed successfully", "2 days ago", StatusKind.Neutral),
        new(Icons.Material.Outlined.Apartment, "New site added", "Harbour Point Phase 2 created", "2 days ago", StatusKind.Info),
        new(Icons.Material.Outlined.PictureAsPdf, "Report ready", "Monthly compliance report is available", "3 days ago", StatusKind.Neutral),
    };
}
