namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// Supplies option lists and default settings for the form pages. Interface-first for
/// the later API swap; nothing here persists.
/// </summary>
public interface IFormSampleDataService
{
    IReadOnlyList<string> GetCompanyTypes();
    IReadOnlyList<string> GetTrades();
    IReadOnlyList<string> GetRegions();
    IReadOnlyList<string> GetSiteNames();
    IReadOnlyList<string> GetOperativeNames();
    IReadOnlyList<string> GetPermitTypes();
    IReadOnlyList<string> GetRoles();
    IReadOnlyList<PermissionFlag> GetPermissionFlags();
    IReadOnlyList<ModuleEntitlement> GetModuleEntitlements();
    GeneralSettings GetGeneralSettings();
}

/// <summary>A single yes/no permission, rendered as a toggle.</summary>
public sealed record PermissionFlag(string Key, string Label, string Description, bool Enabled);

/// <summary>A single module on/off entitlement, rendered as a toggle.</summary>
public sealed record ModuleEntitlement(string Key, string Name, string Description, bool Enabled);

/// <summary>Default values for the general settings form.</summary>
public sealed record GeneralSettings(
    string OrganisationName,
    string TimeZone,
    bool EnforceGeofencing,
    bool RequirePasscodeOnPackLink,
    bool BlockNonCompliantEntry,
    bool NotifyOnExpiry);
