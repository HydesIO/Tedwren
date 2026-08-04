using Tedwren.UiComponents.Models;

namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// Supplies sample row data for the list / table pages (Organisation, Workforce, Sites,
/// Compliance library, Audit Log). Interface-first for the later API swap.
/// </summary>
public interface IListSampleDataService
{
    IReadOnlyList<Company> GetCompanies();
    IReadOnlyList<Operative> GetOperatives();
    IReadOnlyList<Site> GetSites();
    IReadOnlyList<QualificationType> GetQualificationTypes();
    IReadOnlyList<AuditEntry> GetAuditEntries();
}

public sealed record Company(
    string Name,
    string Type,
    string Trade,
    int Operatives,
    double CompliancePercent,
    StatusKind Status,
    string StatusLabel);

public sealed record Operative(
    string Name,
    string Trade,
    string Company,
    string PrimarySite,
    StatusKind Status,
    string StatusLabel,
    DateOnly? NextExpiry);

public sealed record Site(
    string Name,
    string Client,
    string Region,
    int Operatives,
    double CompliancePercent,
    RiskSeverity Risk,
    StatusKind Status,
    string StatusLabel);

public sealed record QualificationType(
    string Name,
    string Category,
    string Issuer,
    int ValidMonths,
    int HeldBy);

public sealed record AuditEntry(
    DateTime Timestamp,
    string Actor,
    string Action,
    string Entity,
    string Category);
