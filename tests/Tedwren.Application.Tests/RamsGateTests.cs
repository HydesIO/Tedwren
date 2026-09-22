using Tedwren.Application.Entitlements;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Rams;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the shared Gate-5 RAMS evaluator (<see cref="RamsGate"/>, Subcontractor Onboarding spec): it is
/// NotApplicable when the HSE module is not held or the worker has no subcontractor RAMS obligation, NoApprovedRams
/// until an approved live RAMS exists (§506), MustSign until the operative signs the current live version, and
/// Signed once they have. Used by both the manager decision and the operative's own sign-in, so the two never diverge.
/// </summary>
public sealed class RamsGateTests
{
    private sealed class Fixture
    {
        public Guid Company { get; } = Guid.NewGuid();
        public Guid SiteId { get; } = Guid.NewGuid();
        public Guid Person { get; } = Guid.NewGuid();

        public InMemorySiteStore Sites { get; } = new(seed: false);
        public InMemoryOrganisationStore Org { get; } = new(seed: false);
        public InMemoryEntitlementRepository Entitlements { get; } = new();
        public InMemorySubcontractorOnboardingConfigRepository Configs { get; } = new();
        public InMemoryRamsRepository Rams { get; } = new();
        public InMemoryRamsAcknowledgementRepository Acks { get; } = new();

        public RamsGate Build() => new(
            new InMemorySiteRepository(Sites),
            new EntitlementService(Entitlements),
            Configs,
            Rams,
            Acks,
            new InMemoryEngagementRepository(Org));

        /// <summary>Seeds the site (owned by the MC) and the operative's active engagement under the same company.</summary>
        public void SeedSiteAndWorker()
        {
            Sites.Sites[SiteId] = new Site { Id = SiteId, CompanyId = Company, Name = "Meridian Tower" };
            Org.Engagements[Guid.NewGuid()] = new Engagement { CompanyId = Company, PersonId = Person, Name = "Alex Operative" };
        }

        public Task EnableHseAsync() => Entitlements.SetAsync(Company, "hse", true);

        /// <summary>Seeds a subcontractor config for the worker's company; RAMS family optional.</summary>
        public Task SeedConfigAsync(Guid? ramsFamilyId) =>
            Configs.AddAsync(new SubcontractorOnboardingConfig
            {
                Id = Guid.NewGuid(), InviterCompanyId = Company, SubcontractorCompanyId = Company,
                RamsFamilyId = ramsFamilyId, CreatedUtc = DateTimeOffset.UtcNow,
            });

        public Task SeedLiveRamsAsync(Guid familyId, int version = 1, RamsStatus status = RamsStatus.Approved, bool isLive = true) =>
            Rams.AddAsync(new RamsSubmission
            {
                CompanyId = Company, FamilyId = familyId, Version = version, Reference = "RAMS-1",
                ContractorName = "Groundworks Co", Title = "Groundworks RAMS", Status = status, IsLive = isLive,
            });

        public Task SignAsync(Guid familyId, int version = 1) =>
            Acks.AddAsync(new RamsAcknowledgement
            {
                CompanyId = Company, PersonId = Person, FamilyId = familyId, Version = version, SignatureName = "Alex Operative",
            });
    }

    [Fact] // Unknown site → not enforced (the decision still records it, never blocks on it).
    public async Task NotApplicable_WhenSiteUnknown()
    {
        var fx = new Fixture();
        var result = await fx.Build().EvaluateAsync(fx.Person, fx.SiteId);
        Assert.Equal(RamsGateStatus.NotApplicable, result.Status);
    }

    [Fact] // §406 — the HSE module is not held, so RAMS does not apply.
    public async Task NotApplicable_WhenHseNotHeld()
    {
        var fx = new Fixture();
        fx.SeedSiteAndWorker();
        await fx.SeedConfigAsync(Guid.NewGuid());

        var result = await fx.Build().EvaluateAsync(fx.Person, fx.SiteId);

        Assert.Equal(RamsGateStatus.NotApplicable, result.Status);
    }

    [Fact] // The MC's own crew / a worker with no subcontractor config → not enforced.
    public async Task NotApplicable_WhenNoConfig()
    {
        var fx = new Fixture();
        fx.SeedSiteAndWorker();
        await fx.EnableHseAsync();

        var result = await fx.Build().EvaluateAsync(fx.Person, fx.SiteId);

        Assert.Equal(RamsGateStatus.NotApplicable, result.Status);
    }

    [Fact] // §506 — a config with no RAMS family yet means no approved RAMS; work cannot start.
    public async Task NoApprovedRams_WhenConfigHasNoFamily()
    {
        var fx = new Fixture();
        fx.SeedSiteAndWorker();
        await fx.EnableHseAsync();
        await fx.SeedConfigAsync(ramsFamilyId: null);

        var result = await fx.Build().EvaluateAsync(fx.Person, fx.SiteId);

        Assert.Equal(RamsGateStatus.NoApprovedRams, result.Status);
    }

    [Fact] // §506 — a submitted-but-not-approved RAMS is not a live approved one; still blocked.
    public async Task NoApprovedRams_WhenLiveRamsNotApproved()
    {
        var fx = new Fixture();
        fx.SeedSiteAndWorker();
        await fx.EnableHseAsync();
        var familyId = Guid.NewGuid();
        await fx.SeedConfigAsync(familyId);
        await fx.SeedLiveRamsAsync(familyId, status: RamsStatus.Submitted, isLive: false);

        var result = await fx.Build().EvaluateAsync(fx.Person, fx.SiteId);

        Assert.Equal(RamsGateStatus.NoApprovedRams, result.Status);
    }

    [Fact] // Spec Gate 5 — an approved live RAMS the operative has not signed → they must sign it (carries the live id).
    public async Task MustSign_WhenUnsigned()
    {
        var fx = new Fixture();
        fx.SeedSiteAndWorker();
        await fx.EnableHseAsync();
        var familyId = Guid.NewGuid();
        await fx.SeedConfigAsync(familyId);
        await fx.SeedLiveRamsAsync(familyId);

        var result = await fx.Build().EvaluateAsync(fx.Person, fx.SiteId);

        Assert.Equal(RamsGateStatus.MustSign, result.Status);
        Assert.NotNull(result.LiveRamsId);
    }

    [Fact] // R3 — a signature for an earlier version does not clear the current live version; must re-sign.
    public async Task MustSign_WhenSignedVersionIsStale()
    {
        var fx = new Fixture();
        fx.SeedSiteAndWorker();
        await fx.EnableHseAsync();
        var familyId = Guid.NewGuid();
        await fx.SeedConfigAsync(familyId);
        await fx.SeedLiveRamsAsync(familyId, version: 2);
        await fx.SignAsync(familyId, version: 1);   // signed v1, but v2 is live

        var result = await fx.Build().EvaluateAsync(fx.Person, fx.SiteId);

        Assert.Equal(RamsGateStatus.MustSign, result.Status);
    }

    [Fact] // Cleared — an approved live RAMS signed on its current version admits.
    public async Task Signed_WhenCurrentVersionSigned()
    {
        var fx = new Fixture();
        fx.SeedSiteAndWorker();
        await fx.EnableHseAsync();
        var familyId = Guid.NewGuid();
        await fx.SeedConfigAsync(familyId);
        await fx.SeedLiveRamsAsync(familyId, version: 2);
        await fx.SignAsync(familyId, version: 2);

        var result = await fx.Build().EvaluateAsync(fx.Person, fx.SiteId);

        Assert.Equal(RamsGateStatus.Signed, result.Status);
    }
}
