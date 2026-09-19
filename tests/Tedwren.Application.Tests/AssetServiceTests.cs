using Tedwren.Abstractions.Contracts.Assets;
using Tedwren.Application.Assets;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for the plant &amp; equipment register service (PRD §8.2): CRUD, company scoping (R15), and the
/// certification status derived from the expiry date.
/// </summary>
public sealed class AssetServiceTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    private static AssetService CreateSut() => new(new InMemoryAssetRepository());

    private static CreateAssetRequest NewAsset(string name = "Tower Crane", DateOnly? certExpiry = null) =>
        new(name, "Crane", "SN1", "Site A", "Bob", certExpiry, null, null);

    [Fact]
    public async Task Create_ThenList_ReturnsActiveAsset()
    {
        var service = CreateSut();

        var id = await service.CreateAsync(CompanyA, NewAsset());
        var list = await service.ListForCompanyAsync(CompanyA);

        Assert.Equal(id, Assert.Single(list).Id);
        Assert.Equal("Active", list[0].Status);
    }

    [Fact]
    public async Task Create_BlankName_Throws()
    {
        var service = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(CompanyA, NewAsset(name: "   ")));
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var service = CreateSut();
        var id = await service.CreateAsync(CompanyA, NewAsset());

        var ok = await service.UpdateAsync(CompanyA, id, new UpdateAssetRequest(
            "Excavator", "Digger", "SN2", "Site B", "Sue", null, null, "note"));

        Assert.True(ok);
        var asset = (await service.ListForCompanyAsync(CompanyA)).Single();
        Assert.Equal("Excavator", asset.Name);
        Assert.Equal("Digger", asset.AssetType);
    }

    [Fact]
    public async Task Update_OtherCompany_ReturnsFalse()
    {
        var service = CreateSut();
        var id = await service.CreateAsync(CompanyA, NewAsset());

        // R15: Company B cannot update Company A's asset.
        Assert.False(await service.UpdateAsync(CompanyB, id, new UpdateAssetRequest("X", null, null, null, null, null, null, null)));
    }

    [Fact]
    public async Task Retire_SetsRetired_ScopedToCompany()
    {
        var service = CreateSut();
        var id = await service.CreateAsync(CompanyA, NewAsset());

        Assert.False(await service.RetireAsync(CompanyB, id));   // R15
        Assert.True(await service.RetireAsync(CompanyA, id));
        Assert.Equal("Retired", (await service.ListForCompanyAsync(CompanyA)).Single().Status);
    }

    [Fact]
    public async Task CertificationStatus_NoDate_IsNoCertificate()
    {
        var service = CreateSut();
        await service.CreateAsync(CompanyA, NewAsset(certExpiry: null));

        Assert.Equal("No certificate", (await service.ListForCompanyAsync(CompanyA)).Single().CertificationStatus);
    }

    [Fact]
    public async Task CertificationStatus_PastDate_IsExpired()
    {
        var service = CreateSut();
        await service.CreateAsync(CompanyA, NewAsset(certExpiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));

        Assert.Equal("Expired", (await service.ListForCompanyAsync(CompanyA)).Single().CertificationStatus);
    }

    [Fact]
    public async Task CertificationStatus_WithinWindow_IsExpiringSoon()
    {
        var service = CreateSut();
        await service.CreateAsync(CompanyA, NewAsset(certExpiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10))));

        Assert.Equal("Expiring soon", (await service.ListForCompanyAsync(CompanyA)).Single().CertificationStatus);
    }

    [Fact]
    public async Task CertificationStatus_FarFuture_IsValid()
    {
        var service = CreateSut();
        await service.CreateAsync(CompanyA, NewAsset(certExpiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(200))));

        Assert.Equal("Valid", (await service.ListForCompanyAsync(CompanyA)).Single().CertificationStatus);
    }
}
