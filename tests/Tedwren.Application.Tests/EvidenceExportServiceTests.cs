using System.IO.Compression;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Application.Evidence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for the unified compliance evidence export (PRD §8.2): the per-section record counts, company
/// scoping (R15), and the generated ZIP (a manifest plus one CSV per section with the records' data).
/// </summary>
public sealed class EvidenceExportServiceTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    private sealed record Fixture(
        EvidenceExportService Service,
        InMemoryPermitRepository Permits,
        InMemoryRamsRepository Rams);

    private static Fixture CreateSut()
    {
        var permits = new InMemoryPermitRepository();
        var rams = new InMemoryRamsRepository();
        var service = new EvidenceExportService(
            permits, rams, new InMemoryAssetRepository(), new InMemoryHazardReportRepository(),
            new InMemoryIncidentReportRepository(), new InMemoryHavsExposureRepository(),
            new InMemoryDocumentDistributionRepository());
        return new Fixture(service, permits, rams);
    }

    [Fact]
    public async Task Summary_CountsPerSection_ScopedToCompany()
    {
        var f = CreateSut();
        await f.Permits.AddAsync(new Permit { CompanyId = CompanyA, PermitType = "Hot Works" });
        await f.Permits.AddAsync(new Permit { CompanyId = CompanyB, PermitType = "Confined Space" });   // other tenant
        await f.Rams.AddAsync(new RamsSubmission { CompanyId = CompanyA, Reference = "RAMS-1", ContractorName = "Apex", Title = "Excavation" });

        var summary = await f.Service.GetSummaryAsync(CompanyA);

        Assert.Equal(1, SectionCount(summary, "Permits"));            // R15 — CompanyB's permit excluded
        Assert.Equal(1, SectionCount(summary, "RAMS"));
        Assert.Equal(0, SectionCount(summary, "Plant & Equipment"));
        Assert.Equal(2, summary.TotalRecords);
        Assert.Equal(7, summary.Sections.Count);                     // every evidence section is listed
    }

    [Fact]
    public async Task BuildZip_ContainsManifestAndSectionCsvs_WithData()
    {
        var f = CreateSut();
        await f.Permits.AddAsync(new Permit { CompanyId = CompanyA, PermitType = "Hot Works", SiteName = "Meridian Tower" });

        var file = await f.Service.BuildZipAsync(CompanyA);

        Assert.Equal("application/zip", file.ContentType);
        Assert.EndsWith(".zip", file.FileName);

        using var archive = new ZipArchive(new MemoryStream(file.Content));
        var entries = archive.Entries.Select(e => e.FullName).ToList();
        Assert.Contains("manifest.txt", entries);
        Assert.Contains("permits.csv", entries);

        var permitsCsv = ReadEntry(archive, "permits.csv");
        Assert.Contains("Hot Works", permitsCsv);
        Assert.Contains("Meridian Tower", permitsCsv);
    }

    [Fact]
    public async Task BuildZip_CrossTenant_DataExcluded()
    {
        var f = CreateSut();
        await f.Permits.AddAsync(new Permit { CompanyId = CompanyB, PermitType = "Confined Space", SiteName = "Other Site" });

        var file = await f.Service.BuildZipAsync(CompanyA);

        using var archive = new ZipArchive(new MemoryStream(file.Content));
        Assert.DoesNotContain("Confined Space", ReadEntry(archive, "permits.csv"));   // R15
    }

    private static int SectionCount(EvidenceSummaryDto summary, string name) =>
        summary.Sections.Single(s => s.Name == name).Count;

    private static string ReadEntry(ZipArchive archive, string name)
    {
        using var reader = new StreamReader(archive.GetEntry(name)!.Open());
        return reader.ReadToEnd();
    }
}
