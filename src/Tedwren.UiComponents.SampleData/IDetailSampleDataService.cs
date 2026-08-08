using Tedwren.UiComponents.Models;

namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// Supplies richer, per-record data for the detail pages, keyed by slug. Interface-first
/// for the later API swap. Returns null when a slug doesn't match a sample record.
/// </summary>
public interface IDetailSampleDataService
{
    CompanyDetail? GetCompany(string slug);
    OperativeDetail? GetOperative(string slug);
    SiteDetail? GetSite(string slug);
}

public sealed record CompanyDetail(
    string Name,
    string Type,
    string Trade,
    StatusKind Status,
    string StatusLabel,
    double CompliancePercent,
    string RegistrationNumber,
    string Address,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    IReadOnlyList<CompanyDocument> Documents,
    IReadOnlyList<Operative> Operatives);

public sealed record CompanyDocument(
    string Name,
    string Type,
    StatusKind Status,
    string StatusLabel,
    DateOnly? ExpiresOn);

public sealed record OperativeDetail(
    string Name,
    string Trade,
    string Company,
    string PrimarySite,
    StatusKind Status,
    string StatusLabel,
    string Phone,
    string Email,
    DateOnly DateOfBirth,
    string NiNumber,
    IReadOnlyList<QualificationHeld> Qualifications,
    IReadOnlyList<ActivityItem> History);

public sealed record QualificationHeld(
    string Name,
    string Issuer,
    DateOnly ObtainedOn,
    DateOnly? ExpiresOn,
    StatusKind Status,
    string StatusLabel);

public sealed record SiteDetail(
    string Name,
    string Client,
    string Region,
    string Address,
    string Manager,
    DateOnly StartDate,
    int Operatives,
    double CompliancePercent,
    StatusKind Status,
    string StatusLabel,
    IReadOnlyList<Operative> Workforce,
    IReadOnlyList<SiteProperty> Properties);

public sealed record SiteProperty(
    string Address,
    int Units,
    StatusKind Status,
    string StatusLabel);
