using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Subcontractors;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Subcontractors;
using Tedwren.Application.Trades;
using Xunit;
using DomainOrgType = Tedwren.Domain.Enums.OrgType;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the main-contractor subcontractor set-up & configuration flow (Subcontractor Onboarding spec Stage 1
/// / §4): setup creates the subcontractor company + invite + configuration, the configuration round-trips and is
/// tenant-scoped (R15), and the trade-onboarding view surfaces the configured required-document headings when a
/// configuration exists (falling back to the default set otherwise — phase independence with the plain flow).
/// </summary>
public sealed class SubcontractorOnboardingServiceTests
{
    private static readonly Guid MainContractor = Guid.Parse("77777777-7777-4777-8777-000000000001");
    private static readonly Guid OtherContractor = Guid.Parse("88888888-8888-4888-8888-000000000002");

    /// <summary>A fixed-tenant current-user stub so the service scopes to a main contractor (R15).</summary>
    private sealed class StubCurrentUser : ICurrentUserService
    {
        private readonly Guid _companyId;
        public StubCurrentUser(Guid companyId) => _companyId = companyId;
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("MC Admin", "Administrator", _companyId, false, null, Guid.NewGuid()));
    }

    private sealed record Sut(
        SubcontractorOnboardingService Subs,
        TradeOnboardingService Trades,
        InMemorySubcontractorOnboardingConfigRepository Configs,
        InMemoryCompanyRepository Companies,
        InMemoryTradeInviteRepository Invites);

    /// <summary>Builds the subcontractor + trade onboarding services over shared in-memory repositories, scoped to a tenant.</summary>
    private static Sut CreateSut(Guid tenant)
    {
        var orgStore = new InMemoryOrganisationStore(seed: false);
        var qualStore = new InMemoryQualificationStore(seed: false);
        var companies = new InMemoryCompanyRepository(orgStore);
        var documents = new InMemoryCompanyDocumentRepository(orgStore);
        var organisation = new OrganisationService(
            companies, documents, new InMemoryPersonRepository(orgStore),
            new InMemoryEngagementRepository(orgStore), new InMemoryQualificationCardRepository(qualStore));
        var invites = new InMemoryTradeInviteRepository();
        var configs = new InMemorySubcontractorOnboardingConfigRepository();
        var currentUser = new StubCurrentUser(tenant);

        var subs = new SubcontractorOnboardingService(organisation, invites, configs, currentUser);
        var trades = new TradeOnboardingService(
            invites, companies, documents, organisation, new InMemoryImageStore(),
            audit: null, currentUser: currentUser, email: null, subcontractorConfigs: configs);

        return new Sut(subs, trades, configs, companies, invites);
    }

    private static SetupSubcontractorRequest SampleRequest(
        bool requirePasscode = false, IReadOnlyList<RequiredDocumentSelection>? documents = null) => new(
        CompanyName: "Apex Electrical Ltd",
        Trade: "Electrical",
        RegistrationNumber: "09876543",
        ContactName: "Sam Taylor",
        ContactEmail: "sam@apex.example",
        ContactPhone: "+44 7700 900000",
        AccessPeriodMonths: 6,
        RequiredDocuments: documents ?? new[]
        {
            new RequiredDocumentSelection("Employer's Liability Insurance", true),
            new RequiredDocumentSelection("Risk Assessments & Method Statements (RAMS)", false),
        },
        SsstsRequired: true,
        SmstsRequired: false,
        InductionValidityDays: 365,
        InductionPassMark: 4,
        InductionAttemptLimit: 3,
        RamsReviewCycleMonths: 6,
        RequirePasscode: requirePasscode);

    [Fact] // Setup creates a subcontractor company, an invite (in the inviting tenant) and a configuration.
    public async Task Setup_CreatesSubcontractorCompany_InviteAndConfig()
    {
        var sut = CreateSut(MainContractor);

        var result = await sut.Subs.SetupAsync(SampleRequest(requirePasscode: true));

        Assert.NotEqual(Guid.Empty, result.SubcontractorCompanyId);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.False(string.IsNullOrWhiteSpace(result.Passcode));   // a passcode was required

        var company = await sut.Companies.GetByIdAsync(result.SubcontractorCompanyId);
        Assert.NotNull(company);
        Assert.Equal(DomainOrgType.Subcontractor, company!.OrgType);

        var invite = await sut.Invites.GetByIdAsync(result.TradeInviteId);
        Assert.NotNull(invite);
        Assert.Equal(MainContractor, invite!.InviterCompanyId);
        Assert.Equal(result.SubcontractorCompanyId, invite.CompanyId);

        var config = await sut.Configs.GetBySubcontractorCompanyAsync(result.SubcontractorCompanyId);
        Assert.NotNull(config);
        Assert.Equal(6, config!.AccessPeriodMonths);
        Assert.Equal(2, config.RequiredDocuments.Count);
        Assert.Single(config.RequiredDocuments, d => d.RequiredBeforeWork);
        Assert.True(config.SsstsRequired);
        Assert.False(config.SmstsRequired);
        Assert.Equal(6, config.RamsReviewCycleMonths);
        Assert.Equal(4, config.InductionPassMark);
    }

    [Fact] // R15 — a configuration is readable by its own inviting tenant, but not by another.
    public async Task GetBySubcontractor_ScopedToInvitingTenant()
    {
        var sut = CreateSut(MainContractor);
        var result = await sut.Subs.SetupAsync(SampleRequest());

        var mine = await sut.Subs.GetBySubcontractorAsync(result.SubcontractorCompanyId);
        Assert.NotNull(mine);
        Assert.Equal(2, mine!.RequiredDocuments.Count);

        // Another main contractor sharing the same config store cannot read it.
        var intruder = new SubcontractorOnboardingService(
            new OrganisationService(
                new InMemoryCompanyRepository(new InMemoryOrganisationStore(seed: false)),
                new InMemoryCompanyDocumentRepository(new InMemoryOrganisationStore(seed: false)),
                new InMemoryPersonRepository(new InMemoryOrganisationStore(seed: false)),
                new InMemoryEngagementRepository(new InMemoryOrganisationStore(seed: false)),
                new InMemoryQualificationCardRepository(new InMemoryQualificationStore(seed: false))),
            sut.Invites, sut.Configs, new StubCurrentUser(OtherContractor));
        Assert.Null(await intruder.GetBySubcontractorAsync(result.SubcontractorCompanyId));
    }

    [Fact] // The trade view surfaces the configured required-document headings when a configuration exists.
    public async Task TradeView_UsesConfiguredHeadings_WhenConfigExists()
    {
        var sut = CreateSut(MainContractor);
        var docs = new[]
        {
            new RequiredDocumentSelection("Risk Assessments & Method Statements (RAMS)", true),
            new RequiredDocumentSelection("Public Liability Insurance", false),
        };
        var result = await sut.Subs.SetupAsync(SampleRequest(documents: docs));

        var view = await sut.Trades.GetByTokenAsync(result.Token, passcode: null);

        Assert.NotNull(view);
        Assert.Equal(
            new[] { "Risk Assessments & Method Statements (RAMS)", "Public Liability Insurance" },
            view!.RequestedDocumentTypes);
    }

    [Fact] // A plain trade invite (no configuration) falls back to the default requested-document set.
    public async Task TradeView_FallsBackToDefaults_WhenNoConfig()
    {
        var sut = CreateSut(MainContractor);

        var link = await sut.Trades.InviteTradeAsync(
            new Tedwren.Abstractions.Contracts.Trades.CreateTradeInviteRequest(
                "Plain Trade Ltd", "Subcontractor", "Groundworks", "Jo", "jo@plain.example", RequirePasscode: false),
            createdByUserId: null);

        var view = await sut.Trades.GetByTokenAsync(link.Token, passcode: null);

        Assert.NotNull(view);
        Assert.Equal(new[] { "Registration", "RAMS", "Insurance", "Accreditation" }, view!.RequestedDocumentTypes);
    }
}
