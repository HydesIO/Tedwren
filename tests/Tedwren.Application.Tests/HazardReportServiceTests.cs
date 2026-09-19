using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Safety;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for hazard / near-miss reporting (PRD §8.2): reference, string-enum parsing, the triage lifecycle
/// (assign → close with a required note), leading-indicator statistics, and company scoping (R15).
/// </summary>
public sealed class HazardReportServiceTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    private static HazardReportService CreateSut() => new(new InMemoryHazardReportRepository(), new InMemoryImageStore());

    private static ReportHazardRequest NewReport(string kind = "NearMiss", string severity = "Medium") =>
        new(kind, "Scaffold tie missing on level 3", "Level 3, east core", null, null, null, null, severity, "Working at height");

    [Fact]
    public async Task Report_AssignsReference_StatusOpen()
    {
        var service = CreateSut();

        var dto = await service.ReportAsync(CompanyA, "Site Op", NewReport());

        Assert.StartsWith("HAZ-", dto.Reference);
        Assert.Equal("Open", dto.Status);
        Assert.Equal("NearMiss", dto.Kind);
    }

    [Fact]
    public async Task Report_ParsesKindAndSeverity_CaseInsensitive()
    {
        var service = CreateSut();

        var dto = await service.ReportAsync(CompanyA, "Op", NewReport(kind: "hazard", severity: "high"));

        Assert.Equal("Hazard", dto.Kind);
        Assert.Equal("High", dto.Severity);
    }

    [Fact]
    public async Task Report_NoDescription_Throws()
    {
        var service = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ReportAsync(
            CompanyA, "Op", new ReportHazardRequest("NearMiss", "  ", null, null, null, null, null, "Low", null)));
    }

    [Fact]
    public async Task Get_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Op", NewReport());

        Assert.Null(await service.GetAsync(CompanyB, dto.Id));   // R15
        Assert.NotNull(await service.GetAsync(CompanyA, dto.Id));
    }

    [Fact]
    public async Task Assign_SetsAssigned_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Op", NewReport());

        Assert.False(await service.AssignAsync(CompanyB, dto.Id, "Foreman", null));   // R15
        Assert.True(await service.AssignAsync(CompanyA, dto.Id, "Foreman", "Access"));

        var updated = await service.GetAsync(CompanyA, dto.Id);
        Assert.Equal("Assigned", updated!.Status);
        Assert.Equal("Foreman", updated.AssignedTo);
        Assert.Equal("Access", updated.Category);
    }

    [Fact]
    public async Task Assign_NoAssignee_Throws()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Op", NewReport());

        await Assert.ThrowsAsync<ArgumentException>(() => service.AssignAsync(CompanyA, dto.Id, "  ", null));
    }

    [Fact]
    public async Task Close_RequiresNote_Throws()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Op", NewReport());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CloseAsync(CompanyA, dto.Id, "   "));
    }

    [Fact]
    public async Task Close_SetsClosed_WithNote()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Op", NewReport());

        Assert.True(await service.CloseAsync(CompanyA, dto.Id, "Tie re-fitted and checked"));

        var closed = await service.GetAsync(CompanyA, dto.Id);
        Assert.Equal("Closed", closed!.Status);
        Assert.Equal("Tie re-fitted and checked", closed.ClosureNote);
        Assert.NotNull(closed.ClosedUtc);
    }

    [Fact]
    public async Task Stats_CountByStatusAndKind_ScopedToCompany()
    {
        var service = CreateSut();
        var a = await service.ReportAsync(CompanyA, "Op", NewReport(kind: "Hazard", severity: "High"));
        await service.ReportAsync(CompanyA, "Op", NewReport(kind: "NearMiss"));
        await service.ReportAsync(CompanyB, "Op", NewReport());                 // other tenant — excluded
        await service.AssignAsync(CompanyA, a.Id, "Foreman", null);

        var stats = await service.GetStatsAsync(CompanyA);

        Assert.Equal(2, stats.Total);
        Assert.Equal(1, stats.Open);
        Assert.Equal(1, stats.Assigned);
        Assert.Equal(1, stats.Hazard);
        Assert.Equal(1, stats.NearMiss);
        Assert.Equal(1, stats.High);
    }
}
