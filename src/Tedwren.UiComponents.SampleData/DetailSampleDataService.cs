using Tedwren.UiComponents.Models;
using MudBlazor;

namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// In-memory detail data, derived from the list sample data plus synthesised
/// detail-level fields. Illustrative only (Plan &amp; Scope §12 Q4).
/// </summary>
public sealed class DetailSampleDataService : IDetailSampleDataService
{
    private readonly IListSampleDataService _list;

    public DetailSampleDataService(IListSampleDataService list) => _list = list;

    public CompanyDetail? GetCompany(string slug)
    {
        var company = _list.GetCompanies().FirstOrDefault(c => Slugs.From(c.Name) == slug);
        if (company is null) return null;

        var operatives = _list.GetOperatives().Where(o => o.Company == company.Name).ToList();

        var documents = new List<CompanyDocument>
        {
            new("Employer's Liability Insurance", "Insurance", StatusKind.Success, "Valid", new DateOnly(2027, 3, 31)),
            new("Public Liability Insurance", "Insurance", StatusKind.Success, "Valid", new DateOnly(2027, 3, 31)),
            new("SSIP Accreditation (CHAS)", "Accreditation", StatusKind.Warning, "Expiring", new DateOnly(2026, 9, 15)),
            new("Health & Safety Policy", "Policy", StatusKind.Success, "Valid", null),
            new("ISO 9001 Certificate", "Accreditation", StatusKind.Danger, "Expired", new DateOnly(2026, 6, 30)),
        };

        return new CompanyDetail(
            company.Name, company.Type, company.Trade, company.Status, company.StatusLabel,
            company.CompliancePercent,
            RegistrationNumber: "09" + Math.Abs(company.Name.GetHashCode() % 1_000_000).ToString("D6"),
            Address: "Unit 4, Kingsway Business Park, London EC2A 4NE",
            ContactName: "Operations Manager",
            ContactEmail: $"contact@{Slugs.From(company.Name)}.co.uk",
            ContactPhone: "+44 20 7946 0000",
            Documents: documents,
            Operatives: operatives);
    }

    public OperativeDetail? GetOperative(string slug)
    {
        var op = _list.GetOperatives().FirstOrDefault(o => Slugs.From(o.Name) == slug);
        if (op is null) return null;

        var quals = new List<QualificationHeld>
        {
            new("CSCS Card", "CITB", new DateOnly(2021, 8, 1), op.NextExpiry ?? new DateOnly(2027, 8, 1),
                op.Status, op.StatusLabel),
            new("Manual Handling", "RoSPA", new DateOnly(2024, 2, 12), new DateOnly(2027, 2, 12),
                StatusKind.Success, "Valid"),
            new("Asbestos Awareness", "UKATA", new DateOnly(2025, 6, 5), new DateOnly(2026, 6, 5),
                StatusKind.Warning, "Expiring"),
        };

        var history = new List<ActivityItem>
        {
            new(Icons.Material.Outlined.PersonAddAlt, "Onboarded", $"Added to {op.Company}", "6 weeks ago", StatusKind.Success),
            new(Icons.Material.Outlined.UploadFile, "Document uploaded", "CSCS card photo submitted", "6 weeks ago", StatusKind.Info),
            new(Icons.Material.Outlined.Login, "Site entry", $"Clocked in at {op.PrimarySite}", "2 days ago", StatusKind.Neutral),
            new(Icons.Material.Outlined.WarningAmber, "Qualification flagged", "Asbestos Awareness expiring soon", "Yesterday", StatusKind.Warning),
        };

        return new OperativeDetail(
            op.Name, op.Trade, op.Company, op.PrimarySite, op.Status, op.StatusLabel,
            Phone: "+44 7700 900" + Math.Abs(op.Name.GetHashCode() % 1000).ToString("D3"),
            Email: $"{Slugs.From(op.Name)}@example.com",
            DateOfBirth: new DateOnly(1988, 4, 17),
            NiNumber: "QQ 12 34 56 C",
            Qualifications: quals,
            History: history);
    }

    public SiteDetail? GetSite(string slug)
    {
        var site = _list.GetSites().FirstOrDefault(s => Slugs.From(s.Name) == slug);
        if (site is null) return null;

        var workforce = _list.GetOperatives().Where(o => o.PrimarySite == site.Name).ToList();

        var properties = new List<SiteProperty>
        {
            new("Plot 1–12, Riverside Gardens", 12, StatusKind.Success, "Handed over"),
            new("Plot 13–28, Riverside Gardens", 16, StatusKind.Warning, "In progress"),
            new("Plot 29–40, Riverside Gardens", 12, StatusKind.Info, "Not started"),
        };

        return new SiteDetail(
            site.Name, site.Client, site.Region,
            Address: $"{site.Name}, {site.Region}",
            Manager: "Site Manager",
            StartDate: new DateOnly(2025, 11, 3),
            Operatives: site.Operatives,
            CompliancePercent: site.CompliancePercent,
            Status: site.Status,
            StatusLabel: site.StatusLabel,
            Workforce: workforce,
            Properties: properties);
    }
}
