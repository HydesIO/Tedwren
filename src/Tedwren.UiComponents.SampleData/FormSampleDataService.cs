namespace Tedwren.UiComponents.SampleData;

/// <summary>In-memory option lists and default settings for the form pages.</summary>
public sealed class FormSampleDataService : IFormSampleDataService
{
    public IReadOnlyList<string> GetCompanyTypes() => new[]
    {
        "Main Contractor", "Subcontractor", "Labour Agency", "Consultant",
    };

    public IReadOnlyList<string> GetTrades() => new[]
    {
        "General Build", "Groundworks", "Mechanical & Electrical", "Scaffolding",
        "Fit-Out", "Civil Engineering", "Roofing", "Demolition", "Cladding",
    };

    public IReadOnlyList<string> GetRegions() => new[]
    {
        "London", "Manchester", "Leeds", "Bristol", "Birmingham", "Glasgow", "Cardiff",
    };

    public IReadOnlyList<string> GetSiteNames() => new[]
    {
        "Meridian Tower", "Kingsway Retail", "Northgate Depot", "Harbour Point", "Riverside Phase 2",
    };

    public IReadOnlyList<string> GetOperativeNames() => new[]
    {
        "James Fletcher", "Sarah Okoro", "Lewis Reid", "Daniel Marsh",
        "Piotr Nowak", "Maya Adeyemi", "Tom Bailey", "Grace Chen",
    };

    public IReadOnlyList<string> GetPermitTypes() => new[]
    {
        "Hot Works", "Confined Space", "Working at Height", "Excavation", "Electrical Isolation", "Lifting Operation",
    };

    public IReadOnlyList<string> GetRoles() => new[]
    {
        "Administrator", "Compliance Manager", "Site Manager", "Viewer",
    };

    public IReadOnlyList<PermissionFlag> GetPermissionFlags() => new List<PermissionFlag>
    {
        new("manage_workforce", "Manage workforce", "Add, edit and offboard operatives", true),
        new("manage_compliance", "Manage compliance", "Review qualifications and compliance packs", true),
        new("issue_permits", "Issue permits", "Create and approve site permits", false),
        new("manage_sites", "Manage sites", "Create and configure sites and projects", false),
        new("view_reports", "View reports", "Access reports and analytics", true),
        new("manage_settings", "Manage settings", "Change system configuration and entitlements", false),
    };

    public IReadOnlyList<ModuleEntitlement> GetModuleEntitlements() => new List<ModuleEntitlement>
    {
        new("workforce", "Workforce", "Operative register, profiles and onboarding", true),
        new("compliance", "Compliance", "Qualification tracking and compliance packs", true),
        new("inductions", "Inductions", "Video/quiz inductions and history", true),
        new("time", "Time & Attendance", "Timesheets and corrections", false),
        new("permits", "Permits", "Permit issuance and tracking", true),
        new("reports", "Reports & Analytics", "Dashboards, reports and exports", true),
        new("integrations", "Integrations", "Connected third-party services", false),
    };

    public GeneralSettings GetGeneralSettings() => new(
        OrganisationName: "Tedwren Ltd",
        TimeZone: "Europe/London",
        EnforceGeofencing: true,
        RequirePasscodeOnPackLink: true,
        BlockNonCompliantEntry: true,
        NotifyOnExpiry: false);
}
