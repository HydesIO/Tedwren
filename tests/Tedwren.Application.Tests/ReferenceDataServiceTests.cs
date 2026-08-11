using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Reference;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>Unit tests for the reference-data service over the seeded in-memory repository.</summary>
public sealed class ReferenceDataServiceTests
{
    private static ReferenceDataService CreateService() =>
        new(new InMemoryReferenceDataRepository());

    [Fact]
    public async Task GetList_ReturnsOrderedSeededValues()
    {
        var service = CreateService();

        var companyTypes = await service.GetListAsync(ReferenceListKeys.CompanyTypes);

        Assert.Equal("Main Contractor", companyTypes[0]);   // display order preserved
        Assert.Contains("Subcontractor", companyTypes);
    }

    [Fact]
    public async Task GetList_UnknownKey_ReturnsEmpty()
    {
        var service = CreateService();

        var values = await service.GetListAsync("no-such-list");

        Assert.Empty(values);
    }
}
