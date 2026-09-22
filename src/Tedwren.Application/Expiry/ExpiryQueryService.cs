using Tedwren.Abstractions;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Jobs;
using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Expiry;

/// <summary>
/// Read-side queries for the expiry engine (SF-21 job visibility + the upcoming-expiries view). The upcoming view
/// unions the same expiry sources the warning engine uses — qualification cards (SF-9), company documents (SUB-4)
/// and inductions (MC-7) — scoped to the signed-in tenant (R15), soonest first. Store agnostic: the same
/// implementation runs over the in-memory and Dapper repositories.
/// </summary>
public sealed class ExpiryQueryService : IExpiryQueryService
{
    private readonly IEnumerable<IExpirySource> _sources;
    private readonly IJobRunRepository _runs;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>
    /// Creates the service over its expiry sources and the job-run repository. <paramref name="currentUser"/> is
    /// optional (supplied by DI) so the upcoming view is scoped to the signed-in tenant (R15); when it is absent
    /// (direct construction) the query runs unscoped.
    /// </summary>
    public ExpiryQueryService(
        IEnumerable<IExpirySource> sources,
        IJobRunRepository runs,
        ICurrentUserService? currentUser = null)
    {
        _sources = sources;
        _runs = runs;
        _currentUser = currentUser;
    }

    /// <summary>Returns items (cards, company documents, inductions) expiring within <paramref name="withinDays"/> days (or already expired), soonest first, scoped to the caller's tenant.</summary>
    public async Task<IReadOnlyList<UpcomingExpiryDto>> GetUpcomingAsync(int withinDays, CancellationToken cancellationToken = default)
    {
        var items = new List<ExpiryItem>();
        foreach (var source in _sources)
        {
            items.AddRange(await source.GetCurrentAsync(cancellationToken));
        }

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var companyId = await ScopedCompanyAsync(cancellationToken);

        return items
            // Tenant scope (R15): only the caller's company's items; unscoped (no tenant / tests) shows all.
            .Where(i => companyId is null || i.CompanyId == companyId)
            // One row per underlying record — a card fans out to one item per engaging company.
            .GroupBy(i => (i.Source, i.SubjectId))
            .Select(g => g.First())
            .Select(i => (Item: i, Days: i.ExpiresOn.DayNumber - today.DayNumber))
            .Where(x => x.Days <= withinDays)
            .OrderBy(x => x.Days)
            .Select(x => new UpcomingExpiryDto(
                x.Item.SubjectId,
                x.Item.PersonId,
                x.Item.Label,
                SourceLabel(x.Item.Source),
                x.Item.ExpiresOn,
                x.Days,
                DaysLabel(x.Days),
                x.Item.PersonName,
                x.Item.PersonName is null ? null : Slug.From(x.Item.PersonName)))
            .ToList();
    }

    /// <summary>The signed-in tenant's company, or null when unresolved (unit tests / unauthenticated → unscoped).</summary>
    private async Task<Guid?> ScopedCompanyAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return null;
        }

        return (await _currentUser.GetCurrentAsync(cancellationToken)).CompanyId;
    }

    /// <summary>Returns the most recent scheduled-job runs (SF-21 visibility).</summary>
    public async Task<IReadOnlyList<JobRunDto>> GetRecentJobRunsAsync(int take, CancellationToken cancellationToken = default)
    {
        var runs = await _runs.GetRecentAsync(take, cancellationToken);
        return runs.Select(ToDto).ToList();
    }

    /// <summary>The human register name for a source, used as a filterable badge on the row.</summary>
    private static string SourceLabel(ExpirySource source) => source switch
    {
        ExpirySource.Card => "Card",
        ExpirySource.CompanyDocument => "Company document",
        ExpirySource.Induction => "Induction",
        _ => "Other",
    };

    /// <summary>Short status label for an upcoming expiry.</summary>
    private static string DaysLabel(int days) => days switch
    {
        < 0 => "Expired",
        0 => "Expires today",
        _ => $"{days} day(s) left",
    };

    /// <summary>Maps a job run to its DTO.</summary>
    private static JobRunDto ToDto(JobRun r) =>
        new(r.Id, r.JobName, r.StartedUtc, r.FinishedUtc, r.Status.ToString(), r.ItemsProcessed, r.NotificationsSent, r.Error);
}
