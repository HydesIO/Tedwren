using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Audit;
using Tedwren.Application.Decisions;
using Tedwren.Application.Entitlements;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.SiteEntry;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;
using CardVerificationState = Tedwren.Abstractions.Common.CardVerificationState;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the site-entry decision service (MC-8–MC-14, R2, R3, R10): the five-check decision, fail-closed on
/// error (R2), specific block reasons (MC-9), the day-only manager override (MC-11), the self-reconstructing
/// record (R10), and the muster with competency cover (MC-13).
/// </summary>
public sealed class SiteEntryServiceTests
{
    private static readonly Guid Company = Guid.NewGuid();
    private static readonly Guid Site = Guid.NewGuid();

    private sealed class Fixture
    {
        public InMemoryOrganisationStore Org { get; } = new(seed: false);
        public InMemoryAttendanceStore Attendance { get; } = new();
        public InMemoryInductionStore Induction { get; } = new(seed: false);
        public InMemorySiteStore Sites { get; } = new(seed: false);
        public InMemoryDecisionRepository Decisions { get; } = new();
        public FakeQualifications Qualifications { get; } = new();
        public InMemoryEntitlementRepository Entitlements { get; } = new();
        public InMemoryRamsRepository Rams { get; } = new();
        public InMemorySubcontractorOnboardingConfigRepository Configs { get; } = new();
        public InMemoryRamsAcknowledgementRepository Acks { get; } = new();

        /// <summary>Grants the caller company a module (catalogue key) so entitlement-gated checks apply.</summary>
        public Task EnableModuleAsync(string moduleKey) => Entitlements.SetAsync(Company, moduleKey, true);

        public SiteEntryService Build()
        {
            var entitlements = new EntitlementService(Entitlements);
            var decisionService = new DecisionService(Decisions);
            var engagements = new InMemoryEngagementRepository(Org);
            var sites = new InMemorySiteRepository(Sites);
            // The Gate-5 RAMS evaluator, shared with the operative sign-in path (Phase 7).
            var ramsGate = new Rams.RamsGate(sites, entitlements, Configs, Rams, Acks, engagements);
            return new SiteEntryService(
                engagements,
                new InMemoryAttendanceRepository(Attendance),
                new InMemoryInductionSessionRepository(Induction),
                Qualifications,
                entitlements,
                decisionService,
                sites,
                new InMemorySitePropertyRepository(Sites),
                ramsGate);
        }

        public Guid Register(string name = "M. Adeyemi", string? trade = null)
        {
            var personId = Guid.NewGuid();
            Org.Engagements[Guid.NewGuid()] = new Engagement { CompanyId = Company, PersonId = personId, Name = name, Trade = trade };
            return personId;
        }

        /// <summary>Seeds the site the decision is for, owned by the caller company (the site's MC for Gate 5).</summary>
        public void SeedSite() =>
            Sites.Sites[Site] = new Site { Id = Site, CompanyId = Company, Name = "Meridian Tower" };

        /// <summary>Seeds an approved live RAMS for the caller company + a subcontractor config pointing at its family. Returns the family id.</summary>
        public async Task<Guid> SeedApprovedLiveRamsAsync(int version = 1)
        {
            var familyId = Guid.NewGuid();
            await Rams.AddAsync(new RamsSubmission
            {
                CompanyId = Company, FamilyId = familyId, Version = version, Reference = "RAMS-1",
                ContractorName = "Groundworks Co", Title = "Groundworks RAMS", Status = RamsStatus.Approved, IsLive = true,
            });
            await Configs.AddAsync(new SubcontractorOnboardingConfig
            {
                Id = Guid.NewGuid(), InviterCompanyId = Company, SubcontractorCompanyId = Company,
                RamsFamilyId = familyId, CreatedUtc = DateTimeOffset.UtcNow,
            });
            return familyId;
        }

        /// <summary>Records the operative's signature of a live RAMS version (clears Gate 5).</summary>
        public Task SignRamsAsync(Guid personId, Guid familyId, int version = 1) =>
            Acks.AddAsync(new RamsAcknowledgement
            {
                CompanyId = Company, PersonId = personId, FamilyId = familyId, Version = version, SignatureName = "M. Adeyemi",
            });

        public void Induct(Guid personId) =>
            Induction.Sessions[Guid.NewGuid()] = new InductionSession
            {
                TemplateId = Guid.NewGuid(), CompanyId = Company, PersonId = personId, PersonName = "X",
                Status = InductionStatus.Passed, CompletedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(365), LastScore = 3,
            };
    }

    private static DecideEntryRequest Request(Guid personId, ManagerOverrideDto? ov = null) =>
        new(Company, Site, personId, null, ov);

    [Fact] // MC-8 + R10 — a clear worker is admitted and the decision reconstructs from its checks
    public async Task ClearWorker_IsAdmitted_AndRecorded()
    {
        var fx = new Fixture();
        var person = fx.Register();
        fx.Induct(person);
        fx.Qualifications.SetCards(person);   // no cards → nothing expired/unconfirmed

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.True(result.Admitted);
        Assert.Contains(result.Checks, c => c.Name == "RAMS" && c.Outcome == "NotRun");   // module not held (R10)
        var recorded = Assert.Single(await new DecisionService(fx.Decisions).GetForPersonAsync(person));
        Assert.True(recorded.Admitted);
        Assert.Equal(5, recorded.Checks.Count);
    }

    [Fact] // MC-8 (Gate 5) — RAMS passes when the sub has an approved live RAMS and the operative has signed it
    public async Task RamsCheck_Passes_WhenSignedLiveRamsExists()
    {
        var fx = new Fixture();
        await fx.EnableModuleAsync("hse");
        fx.SeedSite();
        var person = fx.Register();
        fx.Induct(person);
        fx.Qualifications.SetCards(person);
        var familyId = await fx.SeedApprovedLiveRamsAsync();
        await fx.SignRamsAsync(person, familyId);

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.True(result.Admitted);
        Assert.Contains(result.Checks, c => c.Name == "RAMS" && c.Outcome == "Passed");
    }

    [Fact] // MC-8 (Gate 5) + MC-9 — an approved live RAMS the operative has not signed blocks with a "must sign" reason
    public async Task RamsCheck_Blocks_WhenLiveRamsNotSigned()
    {
        var fx = new Fixture();
        await fx.EnableModuleAsync("hse");
        fx.SeedSite();
        var person = fx.Register();
        fx.Induct(person);
        fx.Qualifications.SetCards(person);
        await fx.SeedApprovedLiveRamsAsync();   // approved + live, but the operative has not signed it

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.False(result.Admitted);
        Assert.Contains(result.Checks, c => c.Name == "RAMS" && c.Outcome == "Failed");
        Assert.Contains("sign", result.BlockReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // §406/§629 + R10 — with the HSE module not held, the RAMS check records NotRun (never blocks) and reconstructs
    public async Task RamsCheck_NotRun_WhenHseModuleNotHeld()
    {
        var fx = new Fixture();
        fx.SeedSite();
        var person = fx.Register();
        fx.Induct(person);
        fx.Qualifications.SetCards(person);
        await fx.SeedApprovedLiveRamsAsync();   // present, but the module is off so the check does not apply

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.True(result.Admitted);
        Assert.Contains(result.Checks, c => c.Name == "RAMS" && c.Outcome == "NotRun");
    }

    [Fact] // MC-9 — an expired card blocks with a specific reason
    public async Task ExpiredCard_Blocks_WithActionableReason()
    {
        var fx = new Fixture();
        var person = fx.Register();
        fx.Induct(person);
        fx.Qualifications.SetCards(person, (ComplianceState.NonCompliant, "CSCS", false));

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.False(result.Admitted);
        Assert.Contains("CSCS", result.BlockReason);
    }

    [Fact] // MC-8 (Gate 3, SF-11) — a missing legally-mandatory accreditation for the worker's trade blocks, naming it
    public async Task MissingMandatoryAccreditation_Blocks_WithActionableReason()
    {
        var fx = new Fixture();
        var person = fx.Register(trade: "Gas Engineer");
        fx.Induct(person);
        fx.Qualifications.SetCards(person);   // no expired/unconfirmed cards
        fx.Qualifications.SetGate3(person, cleared: false, "Gas Safe");

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.False(result.Admitted);
        Assert.Contains(result.Checks, c => c.Name == "Cards in date & confirmed" && c.Outcome == "Failed");
        Assert.Contains("Gas Safe", result.BlockReason!);
    }

    [Fact] // MC-8 (Gate 3) — advisory/satisfied requirements never block; a cleared Gate 3 admits
    public async Task ClearedGate3_IsAdmitted()
    {
        var fx = new Fixture();
        var person = fx.Register(trade: "Gas Engineer");
        fx.Induct(person);
        fx.Qualifications.SetCards(person);
        fx.Qualifications.SetGate3(person, cleared: true);

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.True(result.Admitted);
        Assert.Contains(result.Checks, c => c.Name == "Cards in date & confirmed" && c.Outcome == "Passed");
    }

    [Fact] // MC-8 — an unregistered worker is blocked
    public async Task UnregisteredWorker_IsBlocked()
    {
        var fx = new Fixture();
        var person = Guid.NewGuid();   // never registered
        fx.Qualifications.SetCards(person);

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.False(result.Admitted);
        Assert.Contains(result.Checks, c => c.Name == "Registered" && c.Outcome == "Failed");
    }

    [Fact] // R2 — a check that errors is treated as a failure (fail-closed)
    public async Task CheckError_FailsClosed()
    {
        var fx = new Fixture();
        var person = fx.Register();
        fx.Induct(person);
        fx.Qualifications.ThrowFor(person);   // the cards check will throw

        var result = await fx.Build().DecideAsync(Request(person));

        Assert.False(result.Admitted);
        Assert.Contains(result.Checks, c => c.Name == "Cards in date & confirmed" && c.Outcome == "Failed");
    }

    [Fact] // MC-11 — a manager override admits a blocked worker and is recorded
    public async Task ManagerOverride_AdmitsBlockedWorker_AndIsRecorded()
    {
        var fx = new Fixture();
        var person = fx.Register();   // not inducted → blocked
        fx.Qualifications.SetCards(person);

        var result = await fx.Build().DecideAsync(Request(person, new ManagerOverrideDto("Dana", "Escorted")));

        Assert.True(result.Admitted);
        Assert.True(result.WasOverridden);
        Assert.Contains(result.Checks, c => c.Name == "Manager override");
    }

    [Fact] // MC-13 — the muster lists on-site workers and reports competency cover
    public async Task Muster_ListsOnSite_AndReportsCompetencyCover()
    {
        var fx = new Fixture();
        var person = fx.Register("First Aider");
        fx.Sites.Sites[Site] = new Site { Id = Site, CompanyId = Company, Name = "Meridian Tower" };
        fx.Attendance.Records[Guid.NewGuid()] = new AttendanceRecord
        {
            PersonId = person, SiteId = Site, Type = AttendanceEventType.SignIn, Outcome = AttendanceOutcome.Accepted,
            OccurredUtc = DateTimeOffset.UtcNow.AddHours(-1),
        };
        fx.Qualifications.SetCards(person, (ComplianceState.Compliant, "First Aid at Work", false));

        var muster = await fx.Build().GetMusterAsync(Site);

        Assert.Single(muster.People);
        var cover = Assert.Single(muster.Competencies);
        Assert.True(cover.Covered);
        Assert.Equal(1, cover.HoldersOnSite);
    }

    /// <summary>Qualification service fake: preset cards per person, with optional error injection.</summary>
    private sealed class FakeQualifications : IQualificationService
    {
        private readonly Dictionary<Guid, List<QualificationCardDto>> _cards = new();
        private readonly Dictionary<Guid, Gate3StatusDto> _gate3 = new();
        private readonly HashSet<Guid> _throw = new();

        public void SetCards(Guid personId, params (ComplianceState State, string Name, bool NeedsReview)[] cards) =>
            _cards[personId] = cards.Select(c => new QualificationCardDto(
                Guid.NewGuid(), personId, Guid.NewGuid(), c.Name, null, null, null, null, null,
                c.State, c.State.ToString(), CardVerificationState.CustomerChecked, "Customer-checked", c.NeedsReview,
                null, null, false)).ToList();

        /// <summary>Presets the Gate-3 (SF-11) result for a person; missing entries are legally-mandatory + unsatisfied.</summary>
        public void SetGate3(Guid personId, bool cleared, params string[] missingMandatory) =>
            _gate3[personId] = new Gate3StatusDto(
                cleared, missingMandatory.Select(m => new Gate3RequirementDto(m, true, false, "Not held")).ToList());

        public void ThrowFor(Guid personId) => _throw.Add(personId);

        public Task<IReadOnlyList<QualificationCardDto>> GetCardsForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
        {
            if (_throw.Contains(personId))
            {
                throw new InvalidOperationException("card store unavailable");
            }

            return Task.FromResult<IReadOnlyList<QualificationCardDto>>(_cards.TryGetValue(personId, out var c) ? c : new List<QualificationCardDto>());
        }

        public Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid> CaptureCardAsync(CaptureCardRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ConfirmCardAsync(ConfirmCardRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid?> RenewCardAsync(RenewCardRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, Guid? companyId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        /// <summary>Returns the preset Gate-3 status, or a cleared status (nothing mandated) when none was set.</summary>
        public Task<Gate3StatusDto> EvaluateGate3Async(Guid personId, string trade, Guid? companyId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_gate3.TryGetValue(personId, out var g) ? g : new Gate3StatusDto(true, Array.Empty<Gate3RequirementDto>()));
        public Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesForManagementAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid> CreateQualificationTypeAsync(CreateQualificationTypeRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateQualificationTypeAsync(Guid id, UpdateQualificationTypeRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteQualificationTypeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TradeQualificationRequirementDto>> GetTradeRequirementsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid> CreateTradeRequirementAsync(CreateTradeRequirementRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateTradeRequirementAsync(Guid id, UpdateTradeRequirementRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteTradeRequirementAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
