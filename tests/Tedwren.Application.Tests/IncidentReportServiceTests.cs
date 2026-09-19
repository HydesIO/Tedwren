using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Safety;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for accident / incident recording (PRD §8.2): reference, the investigation (causes, corrective
/// actions, the RIDDOR flag/category), close-out, and company scoping (R15).
/// </summary>
public sealed class IncidentReportServiceTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    private static IncidentReportService CreateSut() => new(new InMemoryIncidentReportRepository());

    private static ReportIncidentRequest NewReport() =>
        new("Accident", "Operative slipped on wet slab", "Level 1", null, "Jane Smith", "Sprained wrist", "Medium");

    [Fact]
    public async Task Report_AssignsReference_StatusReported()
    {
        var service = CreateSut();

        var dto = await service.ReportAsync(CompanyA, "Manager", NewReport());

        Assert.StartsWith("INC-", dto.Reference);
        Assert.Equal("Reported", dto.Status);
        Assert.Equal("Accident", dto.Kind);
        Assert.False(dto.RiddorReportable);
    }

    [Fact]
    public async Task Report_NoDescription_Throws()
    {
        var service = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ReportAsync(
            CompanyA, "Manager", new ReportIncidentRequest("Accident", "  ", null, null, null, null, "Low")));
    }

    [Fact]
    public async Task Get_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Manager", NewReport());

        Assert.Null(await service.GetAsync(CompanyB, dto.Id));   // R15
        Assert.NotNull(await service.GetAsync(CompanyA, dto.Id));
    }

    [Fact]
    public async Task UpdateInvestigation_SetsCausesAndRiddor_MovesUnderInvestigation_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Manager", NewReport());
        var request = new UpdateIncidentInvestigationRequest(
            "Wet slab not signed", "No wet-works permit", "Introduce permit + signage", true, "Over-7-day incapacitation", "H&S Lead");

        Assert.False(await service.UpdateInvestigationAsync(CompanyB, dto.Id, request));   // R15
        Assert.True(await service.UpdateInvestigationAsync(CompanyA, dto.Id, request));

        var updated = await service.GetAsync(CompanyA, dto.Id);
        Assert.Equal("UnderInvestigation", updated!.Status);
        Assert.Equal("No wet-works permit", updated.RootCause);
        Assert.True(updated.RiddorReportable);
        Assert.Equal("Over-7-day incapacitation", updated.RiddorCategory);
        Assert.Equal("H&S Lead", updated.InvestigatedBy);
    }

    [Fact]
    public async Task UpdateInvestigation_NotReportable_ClearsRiddorCategory()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Manager", NewReport());
        // Category supplied but not reportable — it must not be retained.
        var request = new UpdateIncidentInvestigationRequest(null, null, null, false, "Should be dropped", null);

        await service.UpdateInvestigationAsync(CompanyA, dto.Id, request);

        var updated = await service.GetAsync(CompanyA, dto.Id);
        Assert.False(updated!.RiddorReportable);
        Assert.Null(updated.RiddorCategory);
    }

    [Fact]
    public async Task Close_SetsClosed_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.ReportAsync(CompanyA, "Manager", NewReport());

        Assert.False(await service.CloseAsync(CompanyB, dto.Id));   // R15
        Assert.True(await service.CloseAsync(CompanyA, dto.Id));

        var closed = await service.GetAsync(CompanyA, dto.Id);
        Assert.Equal("Closed", closed!.Status);
        Assert.NotNull(closed.ClosedUtc);
    }
}
