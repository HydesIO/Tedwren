using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the shared-foundation identity rules (SF-1/SF-2/SF-3 and R15) on <see cref="OrganisationService"/>,
/// exercised over the real in-memory repositories with a clean (unseeded) store.
/// </summary>
public sealed class OrganisationServiceTests
{
    /// <summary>Builds the service over fresh in-memory repositories and returns the store for assertions.</summary>
    private static (OrganisationService Service, InMemoryOrganisationStore Store) CreateSut()
    {
        var store = new InMemoryOrganisationStore(seed: false);
        var qualificationStore = new InMemoryQualificationStore(seed: false);
        var service = new OrganisationService(
            new InMemoryCompanyRepository(store),
            new InMemoryPersonRepository(store),
            new InMemoryEngagementRepository(store),
            new InMemoryQualificationCardRepository(qualificationStore));
        return (service, store);
    }

    /// <summary>Creates a company and returns its id.</summary>
    private static Task<Guid> AddCompanyAsync(OrganisationService service, string name) =>
        service.CreateCompanyAsync(new CreateCompanyRequest(name, "Subcontractor", "Groundworks", null, null, null, null, null));

    [Fact] // SF-1
    public async Task SamePhoneAcrossTwoCompanies_CreatesOnePerson_WithTwoEngagements()
    {
        var (service, store) = CreateSut();
        var companyA = await AddCompanyAsync(service, "Alpha Ltd");
        var companyB = await AddCompanyAsync(service, "Beta Ltd");

        var first = await service.AddOperativeAsync(new AddOperativeRequest(companyA, "Joe Smith", "07700900123", "Bricklayer", null));
        var second = await service.AddOperativeAsync(new AddOperativeRequest(companyB, "Joseph Smith", "+44 7700 900123", "Labourer", null));

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.PersonId, second.PersonId);   // one person across companies (SF-1)
        Assert.Single(store.People);
        Assert.Equal(2, store.Engagements.Count);        // two engagements
        Assert.NotEqual(first.EngagementId, second.EngagementId);
    }

    [Fact] // SF-2
    public async Task SamePhoneTwiceInOneCompany_IsRefused_NamingTheExistingRecord()
    {
        var (service, _) = CreateSut();
        var company = await AddCompanyAsync(service, "Alpha Ltd");
        await service.AddOperativeAsync(new AddOperativeRequest(company, "Joe Smith", "07700900123", null, null));

        var duplicate = await service.AddOperativeAsync(new AddOperativeRequest(company, "J Smith", "07700900123", null, null));

        Assert.False(duplicate.Succeeded);
        Assert.NotNull(duplicate.Error);
        Assert.Contains("Joe Smith", duplicate.Error);
    }

    [Fact] // SF-3
    public async Task Archive_RemovesFromRegister_KeepsHistory_AndReactivateRestores()
    {
        var (service, store) = CreateSut();
        var company = await AddCompanyAsync(service, "Alpha Ltd");
        var added = await service.AddOperativeAsync(new AddOperativeRequest(company, "Joe Smith", "07700900123", null, null));
        var engagementId = added.EngagementId!.Value;

        var archived = await service.ArchiveEngagementAsync(company, engagementId);
        var afterArchive = await service.GetCompanyAsync("alpha-ltd");

        Assert.True(archived);
        Assert.Empty(afterArchive!.Operatives);          // gone from the register
        Assert.Single(store.Engagements);                // but retained (history survives)
        Assert.Single(store.People);

        var reactivated = await service.ReactivateEngagementAsync(company, engagementId);
        var afterReactivate = await service.GetCompanyAsync("alpha-ltd");

        Assert.True(reactivated);
        Assert.Single(afterReactivate!.Operatives);      // back in the register
    }

    [Fact] // R15
    public async Task Archive_ThroughAnotherCompany_IsRefused()
    {
        var (service, _) = CreateSut();
        var owner = await AddCompanyAsync(service, "Alpha Ltd");
        var other = await AddCompanyAsync(service, "Beta Ltd");
        var added = await service.AddOperativeAsync(new AddOperativeRequest(owner, "Joe Smith", "07700900123", null, null));

        var result = await service.ArchiveEngagementAsync(other, added.EngagementId!.Value);

        Assert.False(result);
    }

    [Fact]
    public async Task GetCompanies_ReturnsCreatedCompany_WithOperativeCount()
    {
        var (service, _) = CreateSut();
        var company = await AddCompanyAsync(service, "Alpha Ltd");
        await service.AddOperativeAsync(new AddOperativeRequest(company, "Joe Smith", "07700900123", null, null));

        var companies = await service.GetCompaniesAsync();

        var alpha = Assert.Single(companies);
        Assert.Equal("alpha-ltd", alpha.Slug);
        Assert.Equal(1, alpha.Operatives);
    }
}
