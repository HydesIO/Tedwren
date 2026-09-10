using Tedwren.Abstractions;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Jobs;

namespace Tedwren.Application.Expiry;

/// <summary>
/// Read-side queries for the expiry engine (SF-21 job visibility + the upcoming-expiries view). Store
/// agnostic — the same implementation runs over the in-memory and Dapper repositories.
/// </summary>
public sealed class ExpiryQueryService : IExpiryQueryService
{
    private readonly IQualificationCardRepository _cards;
    private readonly IQualificationTypeRepository _types;
    private readonly IJobRunRepository _runs;
    private readonly IEngagementRepository? _engagements;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>
    /// Creates the service over its repositories. <paramref name="engagements"/> and <paramref name="currentUser"/>
    /// are optional (supplied by DI) so the upcoming-expiries view is scoped to the signed-in tenant (R15) and can
    /// name the operative who holds each card; when they are absent (direct construction) the query runs unscoped.
    /// </summary>
    public ExpiryQueryService(
        IQualificationCardRepository cards,
        IQualificationTypeRepository types,
        IJobRunRepository runs,
        IEngagementRepository? engagements = null,
        ICurrentUserService? currentUser = null)
    {
        _cards = cards;
        _types = types;
        _runs = runs;
        _engagements = engagements;
        _currentUser = currentUser;
    }

    /// <summary>Returns current cards expiring within <paramref name="withinDays"/> days (or already expired), soonest first.</summary>
    public async Task<IReadOnlyList<UpcomingExpiryDto>> GetUpcomingAsync(int withinDays, CancellationToken cancellationToken = default)
    {
        var cards = await _cards.GetCurrentWithExpiryAsync(cancellationToken);
        var typeNames = (await _types.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id, t => t.Name);
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        // Scope to the signed-in tenant's people (R15): a card counts here only if the person is engaged by the
        // caller's company. This also supplies the operative's recorded name (names live on the engagement, not
        // the person). Null (no tenant resolved / direct construction) leaves the query unscoped.
        var people = await ScopedPeopleAsync(cancellationToken);

        return cards
            .Select(c => (Card: c, Days: c.ExpiresOn!.Value.DayNumber - today.DayNumber))
            .Where(x => x.Days <= withinDays)
            .Where(x => people is null || people.ContainsKey(x.Card.PersonId))
            .OrderBy(x => x.Days)
            .Select(x =>
            {
                var info = people?.GetValueOrDefault(x.Card.PersonId);
                return new UpcomingExpiryDto(
                    x.Card.Id,
                    x.Card.PersonId,
                    typeNames.TryGetValue(x.Card.QualificationTypeId, out var name) ? name : "Unknown qualification",
                    x.Card.ExpiresOn,
                    x.Days,
                    Label(x.Days),
                    info?.Name,
                    info?.Slug);
            })
            .ToList();
    }

    /// <summary>
    /// The people in the signed-in tenant's scope, keyed by person id, with the operative's recorded name and
    /// slug from the active engagement (R15). Returns null when no tenant is resolved, so the query runs unscoped
    /// (unit tests / unauthenticated) rather than returning nothing.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, (string Name, string Slug)>?> ScopedPeopleAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null || _engagements is null)
        {
            return null;
        }

        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        if (user.CompanyId is not { } companyId)
        {
            return null;
        }

        var engagements = await _engagements.GetActiveByCompanyAsync(companyId, cancellationToken);
        return engagements
            .GroupBy(e => e.PersonId)
            .ToDictionary(g => g.Key, g => (g.First().Name, Slug.From(g.First().Name)));
    }

    /// <summary>Returns the most recent scheduled-job runs (SF-21 visibility).</summary>
    public async Task<IReadOnlyList<JobRunDto>> GetRecentJobRunsAsync(int take, CancellationToken cancellationToken = default)
    {
        var runs = await _runs.GetRecentAsync(take, cancellationToken);
        return runs.Select(ToDto).ToList();
    }

    /// <summary>Short status label for an upcoming expiry.</summary>
    private static string Label(int days) => days switch
    {
        < 0 => "Expired",
        0 => "Expires today",
        _ => $"{days} day(s) left",
    };

    /// <summary>Maps a job run to its DTO.</summary>
    private static JobRunDto ToDto(JobRun r) =>
        new(r.Id, r.JobName, r.StartedUtc, r.FinishedUtc, r.Status.ToString(), r.ItemsProcessed, r.NotificationsSent, r.Error);
}
