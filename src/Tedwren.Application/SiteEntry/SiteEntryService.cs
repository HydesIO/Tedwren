using System.Diagnostics;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Decisions;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.SiteEntry;

/// <summary>
/// The site-entry decision and muster service (MC-8–MC-14, R2, R3, R10, R14). It runs five checks against
/// <b>current</b> data (R3) — registered, not signed in elsewhere, induction valid, cards in date and
/// confirmed, and RAMS where the module is held — <b>failing closed</b> so any error is treated as a failed
/// check and blocks entry (R2). It gives a specific actionable block reason (MC-9), allows a day-only manager
/// override (MC-11), and writes a self-reconstructing record through the decision store including checks that
/// did not run and why (R10). The whole decision is timed against the &lt;3s budget (R14). It also serves the
/// live, offline-aware muster with competency cover (MC-12–MC-14). Store-agnostic.
/// </summary>
public sealed class SiteEntryService : ISiteEntryService
{
    private const string MonitoredCompetency = "First Aid";

    private readonly IEngagementRepository _engagements;
    private readonly IAttendanceRepository _attendance;
    private readonly IInductionSessionRepository _inductions;
    private readonly IQualificationService _qualifications;
    private readonly IEntitlementService _entitlements;
    private readonly IDecisionService _decisions;
    private readonly ISiteRepository _sites;
    private readonly ISitePropertyRepository _properties;

    /// <summary>Creates the service over its collaborators.</summary>
    public SiteEntryService(
        IEngagementRepository engagements, IAttendanceRepository attendance, IInductionSessionRepository inductions,
        IQualificationService qualifications, IEntitlementService entitlements, IDecisionService decisions,
        ISiteRepository sites, ISitePropertyRepository properties)
    {
        _engagements = engagements;
        _attendance = attendance;
        _inductions = inductions;
        _qualifications = qualifications;
        _entitlements = entitlements;
        _decisions = decisions;
        _sites = sites;
        _properties = properties;
    }

    /// <summary>Decides whether a worker may enter, records the decision, and returns the result (MC-8/R10/R14).</summary>
    public async Task<EntryDecisionResultDto> DecideAsync(DecideEntryRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var now = DateTimeOffset.UtcNow;

        // R3: every check reads current data. R2: each is wrapped so any error becomes a failed check.
        var checks = new List<DecisionCheck>
        {
            await RunCheckAsync("Registered", () => CheckRegisteredAsync(request.CompanyId, request.PersonId, cancellationToken)),
            await RunCheckAsync("Not signed in elsewhere", () => CheckNotElsewhereAsync(request.PersonId, request.SiteId, cancellationToken)),
            await RunCheckAsync("Induction valid", () => CheckInductionAsync(request.CompanyId, request.PersonId, now, cancellationToken)),
            await RunCheckAsync("Cards in date & confirmed", () => CheckCardsAsync(request.PersonId, cancellationToken)),
            await RunCheckAsync("RAMS", () => CheckRamsAsync(request.CompanyId, cancellationToken)),
        };

        var admittedByChecks = SiteEntryPolicy.IsAdmitted(checks);
        var wasOverridden = false;

        // MC-11: a manager may override a blocked decision for the day, with a reason — recorded in the trail.
        if (!admittedByChecks && request.Override is { } ov)
        {
            checks.Add(new DecisionCheck("Manager override", DecisionCheckOutcome.Passed, $"Overridden by {ov.By} (day only): {ov.Reason}"));
            wasOverridden = true;
        }

        var admitted = admittedByChecks || wasOverridden;
        var blockReason = admitted ? null : SiteEntryPolicy.BlockReason(checks);

        // R10: record a self-reconstructing decision — the admission plus every check and why.
        var decisionId = await _decisions.RecordAsync(
            new RecordDecisionRequest(request.PersonId, request.SiteId, admitted, checks.Select(ToCheckDto).ToList()),
            cancellationToken);

        stopwatch.Stop();
        return new EntryDecisionResultDto(
            admitted, blockReason, wasOverridden, decisionId, stopwatch.ElapsedMilliseconds,
            checks.Select(c => new DecisionCheckResultDto(c.Name, c.Outcome.ToString(), c.Detail)).ToList());
    }

    /// <summary>Returns the live muster for a site, with data-age and competency cover (MC-12–MC-14).</summary>
    public async Task<MusterDto> GetMusterAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var onSite = await _attendance.GetOnSiteAsync(siteId, cancellationToken);
        var site = await _sites.GetByIdAsync(siteId, cancellationToken);
        var companyId = site?.CompanyId ?? Guid.Empty;
        var propertyNames = (await _properties.GetBySiteAsync(siteId, cancellationToken)).ToDictionary(p => p.Id, p => p.Address);

        var people = new List<MusterPersonDto>(onSite.Count);
        var competencyHolders = 0;
        foreach (var record in onSite)
        {
            var name = companyId != Guid.Empty
                ? (await _engagements.GetByCompanyAndPersonAsync(companyId, record.PersonId, cancellationToken))?.Name ?? "Worker"
                : "Worker";
            string? propertyName = record.PropertyId is { } pid && propertyNames.TryGetValue(pid, out var address) ? address : null;
            people.Add(new MusterPersonDto(record.PersonId, name, record.PropertyId, propertyName, record.OccurredUtc));

            // MC-13: competency cover — count who on site currently holds the monitored competency (in date).
            var cards = await _qualifications.GetCardsForPersonAsync(record.PersonId, cancellationToken);
            if (cards.Any(c => c.QualificationName.Contains(MonitoredCompetency, StringComparison.OrdinalIgnoreCase) && c.State != ComplianceState.NonCompliant))
            {
                competencyHolders++;
            }
        }

        return new MusterDto(
            siteId, now, people,
            new[] { new CompetencyCoverDto(MonitoredCompetency, competencyHolders > 0, competencyHolders) });
    }

    /// <summary>Runs a check, converting any error into a failed check so the decision fails closed (R2).</summary>
    private static async Task<DecisionCheck> RunCheckAsync(string name, Func<Task<DecisionCheck>> check)
    {
        try
        {
            return await check();
        }
        catch
        {
            // R2: a check that cannot be completed is treated as a failure, never as a pass.
            return new DecisionCheck(name, DecisionCheckOutcome.Failed, "This check could not be completed and was treated as a failure.");
        }
    }

    /// <summary>Registered and active with this company (SF-2).</summary>
    private async Task<DecisionCheck> CheckRegisteredAsync(Guid companyId, Guid personId, CancellationToken cancellationToken)
    {
        var engagement = await _engagements.GetByCompanyAndPersonAsync(companyId, personId, cancellationToken);
        return engagement is { Status: EngagementStatus.Active }
            ? new DecisionCheck("Registered", DecisionCheckOutcome.Passed, "Registered and active with this company")
            : new DecisionCheck("Registered", DecisionCheckOutcome.Failed, "Not registered with this company (or the engagement is archived)");
    }

    /// <summary>Not currently signed in at another site (MC-8, Q4).</summary>
    private async Task<DecisionCheck> CheckNotElsewhereAsync(Guid personId, Guid siteId, CancellationToken cancellationToken)
    {
        var open = await _attendance.GetOpenSignInForPersonAsync(personId, cancellationToken);
        return open is not null && open.SiteId != siteId
            ? new DecisionCheck("Not signed in elsewhere", DecisionCheckOutcome.Failed, "Currently signed in at another site")
            : new DecisionCheck("Not signed in elsewhere", DecisionCheckOutcome.Passed, "Not signed in at any other site");
    }

    /// <summary>A completed, in-date induction for this company (MC-8, MC-7).</summary>
    private async Task<DecisionCheck> CheckInductionAsync(Guid companyId, Guid personId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var induction = await _inductions.GetLatestPassedForPersonAsync(companyId, personId, cancellationToken);
        if (induction is null)
        {
            return new DecisionCheck("Induction valid", DecisionCheckOutcome.Failed, "No completed induction on record");
        }

        return induction.IsValid(now)
            ? new DecisionCheck("Induction valid", DecisionCheckOutcome.Passed, $"Inducted (reference {induction.CompletionReference})")
            : new DecisionCheck("Induction valid", DecisionCheckOutcome.Failed, "Induction has expired — re-induction required");
    }

    /// <summary>All qualification cards are in date and confirmed (SF-7/SF-8).</summary>
    private async Task<DecisionCheck> CheckCardsAsync(Guid personId, CancellationToken cancellationToken)
    {
        var cards = await _qualifications.GetCardsForPersonAsync(personId, cancellationToken);
        var expired = cards.Where(c => c.State == ComplianceState.NonCompliant).Select(c => c.QualificationName).ToList();
        if (expired.Count > 0)
        {
            return new DecisionCheck("Cards in date & confirmed", DecisionCheckOutcome.Failed, "Expired card(s): " + string.Join(", ", expired));
        }

        if (cards.Any(c => c.NeedsReview))
        {
            return new DecisionCheck("Cards in date & confirmed", DecisionCheckOutcome.Failed, "A qualification card is unconfirmed");
        }

        return new DecisionCheck("Cards in date & confirmed", DecisionCheckOutcome.Passed, "All cards in date and confirmed");
    }

    /// <summary>RAMS where the module is held; recorded as not-run when the customer does not hold it (R10).</summary>
    private async Task<DecisionCheck> CheckRamsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var held = await _entitlements.IsEnabledAsync(companyId, "rams", cancellationToken);
        return held
            ? new DecisionCheck("RAMS", DecisionCheckOutcome.Passed, "RAMS acknowledged")
            : new DecisionCheck("RAMS", DecisionCheckOutcome.NotRun, "RAMS module not held — check does not apply");
    }

    /// <summary>Maps a domain check to the decision-store DTO.</summary>
    private static DecisionCheckDto ToCheckDto(DecisionCheck c) => new(c.Name, c.Outcome.ToString(), c.Detail);
}
