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

        public SiteEntryService Build()
        {
            var entitlements = new EntitlementService(new InMemoryEntitlementRepository());
            var decisionService = new DecisionService(Decisions);
            return new SiteEntryService(
                new InMemoryEngagementRepository(Org),
                new InMemoryAttendanceRepository(Attendance),
                new InMemoryInductionSessionRepository(Induction),
                Qualifications,
                entitlements,
                decisionService,
                new InMemorySiteRepository(Sites),
                new InMemorySitePropertyRepository(Sites));
        }

        public Guid Register(string name = "M. Adeyemi")
        {
            var personId = Guid.NewGuid();
            Org.Engagements[Guid.NewGuid()] = new Engagement { CompanyId = Company, PersonId = personId, Name = name };
            return personId;
        }

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
        private readonly HashSet<Guid> _throw = new();

        public void SetCards(Guid personId, params (ComplianceState State, string Name, bool NeedsReview)[] cards) =>
            _cards[personId] = cards.Select(c => new QualificationCardDto(
                Guid.NewGuid(), personId, Guid.NewGuid(), c.Name, null, null, null, null, null,
                c.State, c.State.ToString(), CardVerificationState.CustomerChecked, "Customer-checked", c.NeedsReview,
                null, null, false)).ToList();

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
        public Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
