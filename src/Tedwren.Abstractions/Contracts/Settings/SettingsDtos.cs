namespace Tedwren.Abstractions.Contracts.Settings;

/// <summary>
/// A company's organisation-wide general settings (System Configuration). Persisted per company; sensible
/// defaults are returned when a company has not saved any yet.
/// </summary>
public sealed record GeneralSettingsDto(
    string OrganisationName,
    string TimeZone,
    bool EnforceGeofencing,
    bool RequirePasscodeOnPackLink,
    bool BlockNonCompliantEntry,
    bool NotifyOnExpiry);
