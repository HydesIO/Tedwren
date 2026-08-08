using Tedwren.UiComponents.Models;

namespace Tedwren.UiComponents.SampleData;

/// <summary>In-memory list/table data. Illustrative numbers per Plan &amp; Scope §12 Q4.</summary>
public sealed class ListSampleDataService : IListSampleDataService
{
    public IReadOnlyList<Company> GetCompanies() => new List<Company>
    {
        new("Meridian Construction Ltd", "Main Contractor", "General Build", 214, 94, StatusKind.Success, "Compliant"),
        new("Apex Groundworks", "Subcontractor", "Groundworks", 88, 91, StatusKind.Success, "Compliant"),
        new("Kingsway M&E", "Subcontractor", "Mechanical & Electrical", 63, 76, StatusKind.Warning, "At risk"),
        new("Northgate Scaffolding", "Subcontractor", "Scaffolding", 41, 98, StatusKind.Success, "Compliant"),
        new("Harbour Fit-Out Co", "Subcontractor", "Fit-Out", 57, 68, StatusKind.Danger, "Non-compliant"),
        new("Riverside Civils", "Subcontractor", "Civil Engineering", 96, 89, StatusKind.Warning, "At risk"),
        new("Summit Roofing", "Subcontractor", "Roofing", 34, 100, StatusKind.Success, "Compliant"),
    };

    public IReadOnlyList<Operative> GetOperatives() => new List<Operative>
    {
        new("James Fletcher", "Bricklayer", "Meridian Construction Ltd", "Meridian Tower", StatusKind.Danger, "Expired", new DateOnly(2026, 8, 9)),
        new("Sarah Okoro", "Electrician", "Kingsway M&E", "Kingsway Retail", StatusKind.Warning, "Expiring", new DateOnly(2026, 8, 16)),
        new("Lewis Reid", "Labourer", "Apex Groundworks", "Northgate Depot", StatusKind.Warning, "Expiring", new DateOnly(2026, 8, 21)),
        new("Daniel Marsh", "Site Supervisor", "Meridian Construction Ltd", "Meridian Tower", StatusKind.Success, "Compliant", new DateOnly(2026, 8, 28)),
        new("Piotr Nowak", "Scaffolder", "Northgate Scaffolding", "Harbour Point", StatusKind.Success, "Compliant", new DateOnly(2026, 9, 3)),
        new("Maya Adeyemi", "Plumber", "Kingsway M&E", "Kingsway Retail", StatusKind.Success, "Compliant", null),
        new("Tom Bailey", "Carpenter", "Harbour Fit-Out Co", "Harbour Point", StatusKind.Danger, "Non-compliant", new DateOnly(2026, 7, 30)),
        new("Grace Chen", "Civil Engineer", "Riverside Civils", "Riverside Phase 2", StatusKind.Success, "Compliant", new DateOnly(2026, 11, 12)),
    };

    public IReadOnlyList<Site> GetSites() => new List<Site>
    {
        new("Meridian Tower", "Meridian Estates", "London", 214, 94, RiskSeverity.Medium, StatusKind.Warning, "Monitor"),
        new("Kingsway Retail", "Kingsway REIT", "Manchester", 176, 76, RiskSeverity.High, StatusKind.Danger, "At risk"),
        new("Northgate Depot", "Northgate Logistics", "Leeds", 98, 97, RiskSeverity.Low, StatusKind.Success, "Healthy"),
        new("Harbour Point", "Harbour Regeneration", "Bristol", 143, 96, RiskSeverity.Low, StatusKind.Success, "Healthy"),
        new("Riverside Phase 2", "Riverside Homes", "Birmingham", 121, 88, RiskSeverity.Medium, StatusKind.Warning, "Monitor"),
    };

    public IReadOnlyList<QualificationType> GetQualificationTypes() => new List<QualificationType>
    {
        new("CSCS Card", "Card", "CITB", 60, 1284),
        new("SSSTS", "Supervision", "CITB", 60, 142),
        new("SMSTS", "Management", "CITB", 60, 68),
        new("First Aid at Work", "Health & Safety", "HSE", 36, 96),
        new("Asbestos Awareness", "Health & Safety", "UKATA", 12, 411),
        new("Working at Height", "Health & Safety", "IPAF", 60, 233),
        new("Manual Handling", "Health & Safety", "RoSPA", 36, 587),
    };

    public IReadOnlyList<AuditEntry> GetAuditEntries() => new List<AuditEntry>
    {
        new(new DateTime(2026, 8, 4, 9, 12, 0), "Alex Morgan", "Onboarded operative", "M. Adeyemi", "Workforce"),
        new(new DateTime(2026, 8, 4, 8, 48, 0), "System", "RAMS uploaded", "Harbour Point", "Documents"),
        new(new DateTime(2026, 8, 4, 7, 30, 0), "Dana Price", "Issued permit HW-2043", "Meridian Tower", "Permits"),
        new(new DateTime(2026, 8, 3, 17, 5, 0), "System", "Blocked site entry", "Kingsway Retail", "Access"),
        new(new DateTime(2026, 8, 3, 14, 22, 0), "Alex Morgan", "Updated company details", "Kingsway M&E", "Organisation"),
        new(new DateTime(2026, 8, 3, 11, 9, 0), "Priya Shah", "Approved timesheet", "Riverside Phase 2", "Time & Attendance"),
        new(new DateTime(2026, 8, 2, 16, 40, 0), "System", "Qualification expired", "J. Fletcher — CSCS", "Compliance"),
    };
}
