using Tedwren.Abstractions.Contracts.Havs;
using Tedwren.Application.Havs;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for the HAVs exposure service (PRD §8.2): derivation of the A(8)/points/band from tool usages,
/// validation, invalid-usage filtering, and company scoping (R15).
/// </summary>
public sealed class HavsExposureServiceTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static HavsExposureService CreateSut() => new(new InMemoryHavsExposureRepository());

    private static CreateHavsExposureRequest NewRequest(params HavsToolUsageDto[] tools) =>
        new("Sam Mason", Today, tools);

    [Fact]
    public async Task Record_DerivesExposure_ReturnsBand()
    {
        var service = CreateSut();

        var dto = await service.RecordAsync(CompanyA, "Supervisor", NewRequest(new HavsToolUsageDto("Breaker", 5.0, 480)));

        Assert.Equal(5.0, dto.DailyExposureA8);
        Assert.Equal(400, dto.ExposurePoints);
        Assert.Equal("AboveLimitValue", dto.Band);
        Assert.Single(dto.ToolUsages);
    }

    [Fact]
    public async Task Record_NoPerson_Throws()
    {
        var service = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => service.RecordAsync(
            CompanyA, "Sup", new CreateHavsExposureRequest("  ", Today, new[] { new HavsToolUsageDto("Drill", 3, 60) })));
    }

    [Fact]
    public async Task Record_NoValidTools_Throws()
    {
        var service = CreateSut();

        // Blank name, zero magnitude and zero minutes are all invalid — nothing to compute.
        await Assert.ThrowsAsync<ArgumentException>(() => service.RecordAsync(
            CompanyA, "Sup", NewRequest(new HavsToolUsageDto("", 0, 0), new HavsToolUsageDto("Drill", 0, 30))));
    }

    [Fact]
    public async Task Record_FiltersInvalidTools_KeepingValidOnes()
    {
        var service = CreateSut();

        var dto = await service.RecordAsync(CompanyA, "Sup", NewRequest(
            new HavsToolUsageDto("Grinder", 3.5, 90),
            new HavsToolUsageDto("Ignored", 0, 0)));

        Assert.Single(dto.ToolUsages);
        Assert.Equal("Grinder", dto.ToolUsages[0].ToolName);
    }

    [Fact]
    public async Task Get_ScopedToCompany()
    {
        var service = CreateSut();
        var dto = await service.RecordAsync(CompanyA, "Sup", NewRequest(new HavsToolUsageDto("Drill", 3, 120)));

        Assert.Null(await service.GetAsync(CompanyB, dto.Id));   // R15
        Assert.NotNull(await service.GetAsync(CompanyA, dto.Id));
    }

    [Fact]
    public async Task List_ScopedToCompany()
    {
        var service = CreateSut();
        await service.RecordAsync(CompanyA, "Sup", NewRequest(new HavsToolUsageDto("Drill", 3, 120)));
        await service.RecordAsync(CompanyB, "Sup", NewRequest(new HavsToolUsageDto("Drill", 3, 120)));

        Assert.Single(await service.ListAsync(CompanyA));   // R15 — CompanyB's record excluded
    }
}
