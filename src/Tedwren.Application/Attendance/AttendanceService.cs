using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;
using AbstractionsMethod = Tedwren.Abstractions.Common.SignInMethod;
using DomainMethod = Tedwren.Domain.Enums.SignInMethod;

namespace Tedwren.Application.Attendance;

/// <summary>
/// The single implementation of the sign-in / sign-out rules (SF-13–SF-19). Data-store agnostic. It verifies
/// the captured location against the site (or property) boundary (SF-14), applies the customer's location
/// policy when there is none (SF-15), records every attempt including refusals (SF-16), refuses a sign-in
/// while the worker is present elsewhere (SF-18), and computes the duration on sign-out (SF-17). The QR and
/// no-compound link routes produce identical records (SF-13/SF-25). The log is append-only (R4); times are
/// stored as UTC instants (R11).
/// </summary>
public sealed class AttendanceService : IAttendanceService
{
    private readonly ISiteRepository _sites;
    private readonly ISitePropertyRepository _properties;
    private readonly IAttendanceRepository _attendance;
    private readonly IEngagementRepository? _engagements;

    /// <summary>
    /// Creates the service over its repositories. <paramref name="engagements"/> resolves a worker's display
    /// name for the muster/log (the name is per-engagement, cross-company; F12); it is optional so unit tests
    /// that construct the service directly run without it (names then fall back to a neutral placeholder).
    /// </summary>
    public AttendanceService(ISiteRepository sites, ISitePropertyRepository properties, IAttendanceRepository attendance,
        IEngagementRepository? engagements = null)
    {
        _sites = sites;
        _properties = properties;
        _attendance = attendance;
        _engagements = engagements;
    }

    /// <summary>Records a sign-in attempt and returns its outcome.</summary>
    public async Task<SignInResult> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        var site = await _sites.GetByIdAsync(request.SiteId, cancellationToken);
        if (site is null)
        {
            var refused = await AppendAsync(request.PersonId, request.SiteId, request.PropertyId, AttendanceEventType.SignIn,
                AttendanceOutcome.Refused, request, within: null, "Unknown site.", cancellationToken);
            return new SignInResult(false, nameof(AttendanceOutcome.Refused), "Unknown site.", refused.Id, null);
        }

        // SF-18 (Q4): a worker cannot be present at two sites at once — look across all sites, name only.
        var openElsewhere = await _attendance.GetOpenSignInForPersonAsync(request.PersonId, cancellationToken);
        if (openElsewhere is not null)
        {
            var otherSite = openElsewhere.SiteId == site.Id ? site : await _sites.GetByIdAsync(openElsewhere.SiteId, cancellationToken);
            var otherName = otherSite?.Name ?? "another site";
            var reason = openElsewhere.SiteId == site.Id ? "Already signed in here." : $"Already signed in at {otherName}.";
            var refused = await AppendAsync(request.PersonId, site.Id, request.PropertyId, AttendanceEventType.SignIn,
                AttendanceOutcome.Refused, request, within: null, reason, cancellationToken);
            return new SignInResult(false, nameof(AttendanceOutcome.Refused), reason,
                refused.Id, openElsewhere.SiteId == site.Id ? null : otherName);
        }

        var (outcome, within, outcomeReason) = await EvaluateAsync(site, request, cancellationToken);
        var record = await AppendAsync(request.PersonId, site.Id, request.PropertyId, AttendanceEventType.SignIn,
            outcome, request, within, outcomeReason, cancellationToken);
        return new SignInResult(record.CountsForPresence, outcome.ToString(), outcomeReason, record.Id, null);
    }

    /// <summary>Records a sign-out attempt and returns its outcome, with the duration on site (SF-17).</summary>
    public async Task<SignOutResult> SignOutAsync(SignOutRequest request, CancellationToken cancellationToken = default)
    {
        var open = await _attendance.GetOpenSignInForPersonAtSiteAsync(request.PersonId, request.SiteId, cancellationToken);
        if (open is null)
        {
            var refused = new AttendanceRecord
            {
                PersonId = request.PersonId,
                SiteId = request.SiteId,
                Type = AttendanceEventType.SignOut,
                Outcome = AttendanceOutcome.Refused,
                Method = ToDomain(request.Method),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Reason = "Not currently signed in at this site.",
            };
            await _attendance.AddAsync(refused, cancellationToken);
            return new SignOutResult(false, nameof(AttendanceOutcome.Refused), refused.Reason, null, refused.Id);
        }

        var now = DateTimeOffset.UtcNow;
        var signOut = new AttendanceRecord
        {
            PersonId = request.PersonId,
            SiteId = request.SiteId,
            PropertyId = open.PropertyId,
            Type = AttendanceEventType.SignOut,
            Outcome = AttendanceOutcome.Accepted,
            Method = ToDomain(request.Method),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CorrectsRecordId = open.Id,
            OccurredUtc = now,
        };
        await _attendance.AddAsync(signOut, cancellationToken);

        var duration = Math.Round((now - open.OccurredUtc).TotalHours, 2);
        return new SignOutResult(true, nameof(AttendanceOutcome.Accepted), null, duration, signOut.Id);
    }

    /// <summary>Returns the workers currently present on a site.</summary>
    public async Task<IReadOnlyList<OnSiteWorkerDto>> GetOnSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var open = await _attendance.GetOnSiteAsync(siteId, cancellationToken);
        var names = await ResolveWorkerNamesAsync(open.Select(r => r.PersonId), cancellationToken);
        return open
            .Select(r => new OnSiteWorkerDto(r.PersonId, r.SiteId, r.PropertyId, r.OccurredUtc, r.Outcome.ToString(),
                names.GetValueOrDefault(r.PersonId, UnknownWorker)))
            .ToList();
    }

    /// <summary>Returns the recent attendance records for a site.</summary>
    public async Task<IReadOnlyList<AttendanceRecordDto>> GetSiteRecordsAsync(Guid siteId, int take, CancellationToken cancellationToken = default)
    {
        var records = await _attendance.GetBySiteAsync(siteId, take, cancellationToken);
        var names = await ResolveWorkerNamesAsync(records.Select(r => r.PersonId), cancellationToken);
        return records.Select(r => ToDto(r, names.GetValueOrDefault(r.PersonId, UnknownWorker))).ToList();
    }

    /// <summary>Shown when a worker's name cannot be resolved (e.g. a historical log row with no active engagement).</summary>
    private const string UnknownWorker = "Unknown operative";

    /// <summary>
    /// Resolves display names for a set of person ids (F12). A worker's name is held per-company on the
    /// engagement (Person holds none), and a site's attendance is cross-company, so the name is taken from any
    /// of the person's active engagements. Deduplicates first, so this is one lookup per distinct person, not
    /// per record. Returns an empty map when no engagement repository is wired (direct-construction unit tests).
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, string>> ResolveWorkerNamesAsync(IEnumerable<Guid> personIds, CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string>();
        if (_engagements is null)
        {
            return names;
        }

        foreach (var personId in personIds.Distinct())
        {
            var engagements = await _engagements.GetActiveByPersonAsync(personId, cancellationToken);
            var name = engagements.Select(e => e.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
            if (!string.IsNullOrWhiteSpace(name))
            {
                names[personId] = name;
            }
        }

        return names;
    }

    /// <summary>Decides the sign-in outcome from the location and the site's policy (SF-14/SF-15).</summary>
    private async Task<(AttendanceOutcome Outcome, bool? Within, string? Reason)> EvaluateAsync(
        Site site, SignInRequest request, CancellationToken cancellationToken)
    {
        var boundary = await ResolveBoundaryAsync(site, request.PropertyId, cancellationToken);

        if (request.Latitude is double lat && request.Longitude is double lng)
        {
            if (boundary is null)
            {
                return (AttendanceOutcome.Flagged, null, "No boundary configured for this site; recorded and flagged.");
            }

            var within = boundary.Contains(lat, lng);
            return within
                ? (AttendanceOutcome.Accepted, true, null)
                : (AttendanceOutcome.Refused, false, "You are outside the site boundary.");
        }

        // No location — the customer setting decides (SF-15).
        return site.LocationPolicy == LocationPolicy.Refuse
            ? (AttendanceOutcome.Refused, null, "A location is required to sign in at this site.")
            : (AttendanceOutcome.Flagged, null, "Signed in without a verified location; flagged for review.");
    }

    /// <summary>Resolves the boundary to verify against: the property's for a dispersed scheme, else the site's.</summary>
    private async Task<Geofence?> ResolveBoundaryAsync(Site site, Guid? propertyId, CancellationToken cancellationToken)
    {
        if (propertyId is not { } id)
        {
            return site.Boundary;
        }

        var property = (await _properties.GetBySiteAsync(site.Id, cancellationToken)).FirstOrDefault(p => p.Id == id);
        return property?.Boundary ?? site.Boundary;
    }

    /// <summary>Appends a sign-in attempt record with the captured location and outcome.</summary>
    private async Task<AttendanceRecord> AppendAsync(
        Guid personId, Guid siteId, Guid? propertyId, AttendanceEventType type, AttendanceOutcome outcome,
        SignInRequest request, bool? within, string? reason, CancellationToken cancellationToken)
    {
        var record = new AttendanceRecord
        {
            PersonId = personId,
            SiteId = siteId,
            PropertyId = propertyId,
            Type = type,
            Outcome = outcome,
            Method = ToDomain(request.Method),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            WithinBoundary = within,
            Reason = reason,
        };
        await _attendance.AddAsync(record, cancellationToken);
        return record;
    }

    /// <summary>Maps the neutral method enum to the domain enum.</summary>
    private static DomainMethod ToDomain(AbstractionsMethod method) =>
        method == AbstractionsMethod.AssignmentLink ? DomainMethod.AssignmentLink : DomainMethod.QrScan;

    /// <summary>Maps an attendance record to its DTO, with the resolved worker display name (F12).</summary>
    private static AttendanceRecordDto ToDto(AttendanceRecord r, string workerName) => new(
        r.Id, r.PersonId, r.SiteId, r.PropertyId, r.Type.ToString(), r.Outcome.ToString(), r.Method.ToString(),
        r.Latitude, r.Longitude, r.WithinBoundary, r.Reason, r.OccurredUtc, workerName);
}
