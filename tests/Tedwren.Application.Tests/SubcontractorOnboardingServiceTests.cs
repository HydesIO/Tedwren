using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Subcontractors;
using Tedwren.Abstractions.Contracts.Trades;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Inductions;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Rams;
using Tedwren.Application.Subcontractors;
using Tedwren.Application.Trades;
using Tedwren.Domain.Entities;
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
        RamsService Rams,
        InductionService Inductions,
        InMemorySubcontractorOnboardingConfigRepository Configs,
        InMemoryCompanyRepository Companies,
        InMemoryCompanyDocumentRepository Documents,
        InMemoryTradeInviteRepository Invites);

    /// <summary>Builds the subcontractor + trade onboarding services (plus the shared RAMS + induction services they compose) over shared in-memory repositories, scoped to a tenant.</summary>
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
        var ramsRepo = new InMemoryRamsRepository();
        var rams = new RamsService(ramsRepo, new InMemoryImageStore());
        var inductionStore = new InMemoryInductionStore();
        var inductions = new InductionService(new InMemoryInductionTemplateRepository(inductionStore), new InMemoryInductionSessionRepository(inductionStore));
        var currentUser = new StubCurrentUser(tenant);

        var subs = new SubcontractorOnboardingService(organisation, invites, configs, documents, currentUser, audit: null, rams: ramsRepo, inductions: inductions);
        var trades = new TradeOnboardingService(
            invites, companies, documents, organisation, new InMemoryImageStore(),
            audit: null, currentUser: currentUser, email: null, subcontractorConfigs: configs, rams: rams);

        return new Sut(subs, trades, rams, inductions, configs, companies, documents, invites);
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
            sut.Invites, sut.Configs, sut.Documents, new StubCurrentUser(OtherContractor));
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

    /// <summary>A present, in-date, file-backed document for a heading — satisfies Gate 1 for that heading.</summary>
    private static CompanyDocument ValidDoc(Guid companyId, string heading) => new()
    {
        CompanyId = companyId,
        Name = heading,
        Type = heading,
        FileReference = "blob-" + Guid.NewGuid().ToString("N"),
        ExpiresOn = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
    };

    [Fact] // Gate 1 stays closed until the "required before work" document is present and in date (spec §2).
    public async Task Gate1_ClearsWhenRequiredBeforeWorkDocumentValid()
    {
        var sut = CreateSut(MainContractor);
        var result = await sut.Subs.SetupAsync(SampleRequest());   // "Employer's Liability Insurance" is required-before-work

        var before = await sut.Subs.EvaluateGate1Async(result.SubcontractorCompanyId);
        Assert.False(before.Cleared);
        Assert.Contains(before.Requirements, r => r.Heading == "Employer's Liability Insurance" && !r.Satisfied && r.Issue == "Not uploaded");

        await sut.Documents.AddAsync(ValidDoc(result.SubcontractorCompanyId, "Employer's Liability Insurance"));

        var after = await sut.Subs.EvaluateGate1Async(result.SubcontractorCompanyId);
        Assert.True(after.Cleared);
    }

    [Fact] // Fail-closed (R2): an operative cannot be added via the link until Gate 1 clears, then it succeeds.
    public async Task AddOperativeByLink_GatedByGate1()
    {
        var sut = CreateSut(MainContractor);
        var result = await sut.Subs.SetupAsync(SampleRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Trades.AddOperativeByLinkAsync(result.Token, passcode: null,
                new AddTradeOperativeRequest("Jo Bloggs", "+447700900123", "Electrical")));

        await sut.Documents.AddAsync(ValidDoc(result.SubcontractorCompanyId, "Employer's Liability Insurance"));

        var view = await sut.Trades.AddOperativeByLinkAsync(result.Token, passcode: null,
            new AddTradeOperativeRequest("Jo Bloggs", "+447700900123", "Electrical"));
        Assert.NotNull(view);
        Assert.True(view!.Gate1!.Cleared);
    }

    [Fact] // With no "required before work" headings, Gate 1 clears vacuously and operatives can be added.
    public async Task Gate1_VacuousWhenNoRequiredBeforeWork()
    {
        var sut = CreateSut(MainContractor);
        var docs = new[] { new RequiredDocumentSelection("Public Liability Insurance", false) };
        var result = await sut.Subs.SetupAsync(SampleRequest(documents: docs));

        var gate = await sut.Subs.EvaluateGate1Async(result.SubcontractorCompanyId);
        Assert.True(gate.Cleared);
        Assert.Empty(gate.Requirements);

        var view = await sut.Trades.AddOperativeByLinkAsync(result.Token, passcode: null,
            new AddTradeOperativeRequest("Sam", "+447700900999", null));
        Assert.NotNull(view);
    }

    [Fact] // Spec Stage 2→3 — a RAMS-heading upload from a configured subcontractor lands in the MC's RAMS review queue.
    public async Task RamsUpload_BridgedIntoReviewQueue()
    {
        var sut = CreateSut(MainContractor);
        var result = await sut.Subs.SetupAsync(SampleRequest());   // RAMS is one of the configured headings

        await sut.Trades.SubmitDocumentAsync(result.Token, passcode: null,
            new SubmitTradeDocumentRequest("Risk Assessments & Method Statements (RAMS)", "Excavation RAMS", null, null, null, null));

        // The submission is queued under the reviewing main contractor (R15), not the subcontractor.
        var item = Assert.Single(await sut.Rams.GetReviewQueueAsync(MainContractor));
        Assert.Equal("Apex Electrical Ltd", item.ContractorName);
        Assert.Equal("Submitted", item.Status);
        Assert.Empty(await sut.Rams.GetReviewQueueAsync(result.SubcontractorCompanyId));

        // The RAMS family was recorded on the configuration so later uploads become new versions.
        var config = await sut.Configs.GetBySubcontractorCompanyAsync(result.SubcontractorCompanyId);
        Assert.NotNull(config!.RamsFamilyId);
    }

    [Fact] // A non-RAMS upload is not bridged into the RAMS queue.
    public async Task NonRamsUpload_NotBridged()
    {
        var sut = CreateSut(MainContractor);
        var result = await sut.Subs.SetupAsync(SampleRequest());

        await sut.Trades.SubmitDocumentAsync(result.Token, passcode: null,
            new SubmitTradeDocumentRequest("Employer's Liability Insurance", "EL certificate", null, null, null, null));

        Assert.Empty(await sut.Rams.GetReviewQueueAsync(MainContractor));
    }

    [Fact] // Spec §4 — a subcontractor's live RAMS is flagged due once its review cycle elapses (beyond PRD, informational only).
    public async Task RamsReviewDue_DetectedAfterCycleElapses()
    {
        var sut = CreateSut(MainContractor);
        var result = await sut.Subs.SetupAsync(SampleRequest());   // RamsReviewCycleMonths = 6

        // Upload + approve a RAMS so there is a live approved version to date the cycle from.
        await sut.Trades.SubmitDocumentAsync(result.Token, passcode: null,
            new SubmitTradeDocumentRequest("Risk Assessments & Method Statements (RAMS)", "RAMS", null, null, null, null));
        var queued = Assert.Single(await sut.Rams.GetReviewQueueAsync(MainContractor));
        await sut.Rams.ApproveAsync(MainContractor, queued.Id, "MC Admin");

        // Within the cycle — nothing due yet.
        Assert.Empty(await sut.Subs.GetSubcontractorsDueForRamsReviewAsync(DateTimeOffset.UtcNow.AddMonths(3)));

        // After the cycle — the subcontractor is flagged for re-review.
        var due = Assert.Single(await sut.Subs.GetSubcontractorsDueForRamsReviewAsync(DateTimeOffset.UtcNow.AddMonths(7)));
        Assert.Equal(result.SubcontractorCompanyId, due.SubcontractorCompanyId);
        Assert.Equal(6, due.ReviewCycleMonths);
    }

    [Fact] // Gate 4 — setup creates and links the main contractor's own induction template (§6.1).
    public async Task Setup_CreatesAndLinksMcInductionTemplate()
    {
        var sut = CreateSut(MainContractor);

        var result = await sut.Subs.SetupAsync(SampleRequest());

        var config = await sut.Configs.GetBySubcontractorCompanyAsync(result.SubcontractorCompanyId);
        Assert.NotNull(config!.InductionTemplateId);

        var templates = await sut.Inductions.GetTemplatesAsync(MainContractor);
        Assert.Contains(templates, t => t.Id == config.InductionTemplateId);
    }

    [Fact] // The induction is the MC's own — a second subcontractor reuses the same template, not a duplicate (§6.1).
    public async Task Setup_ReusesExistingMcInductionTemplate()
    {
        var sut = CreateSut(MainContractor);

        var first = await sut.Subs.SetupAsync(SampleRequest());
        var second = await sut.Subs.SetupAsync(SampleRequest());

        var c1 = await sut.Configs.GetBySubcontractorCompanyAsync(first.SubcontractorCompanyId);
        var c2 = await sut.Configs.GetBySubcontractorCompanyAsync(second.SubcontractorCompanyId);
        Assert.NotNull(c1!.InductionTemplateId);
        Assert.Equal(c1.InductionTemplateId, c2!.InductionTemplateId);
        Assert.Single(await sut.Inductions.GetTemplatesAsync(MainContractor));   // one MC induction, reused
    }
}
