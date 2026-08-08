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

    /// <summary>Creates the service over its repositories.</summary>
    public ExpiryQueryService(IQualificationCardRepository cards, IQualificationTypeRepository types, IJobRunRepository runs)
    {
        _cards = cards;
        _types = types;
        _runs = runs;
    }

    /// <summary>Returns current cards expiring within <paramref name="withinDays"/> days (or already expired), soonest first.</summary>
    public async Task<IReadOnlyList<UpcomingExpiryDto>> GetUpcomingAsync(int withinDays, CancellationToken cancellationToken = default)
    {
        var cards = await _cards.GetCurrentWithExpiryAsync(cancellationToken);
        var typeNames = (await _types.GetAllAsync(cancellationToken)).ToDictionary(t => t.Id, t => t.Name);
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        return cards
            .Select(c => (Card: c, Days: c.ExpiresOn!.Value.DayNumber - today.DayNumber))
            .Where(x => x.Days <= withinDays)
            .OrderBy(x => x.Days)
            .Select(x => new UpcomingExpiryDto(
                x.Card.Id,
                x.Card.PersonId,
                typeNames.TryGetValue(x.Card.QualificationTypeId, out var name) ? name : "Unknown qualification",
                x.Card.ExpiresOn,
                x.Days,
                Label(x.Days)))
            .ToList();
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
