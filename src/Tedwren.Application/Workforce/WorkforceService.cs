using Tedwren.Abstractions;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Workforce;

/// <summary>
/// The org-wide workforce read model. Data-store agnostic: it composes the same company, engagement,
/// person, qualification-card and decision repositories the rest of the application uses, so the Workforce
/// register and operative detail show real database data. Compliance is derived via <see cref="ComplianceRollup"/>
/// from current cards (SF-8), never invented.
/// </summary>
public sealed class WorkforceService : IWorkforceService
{
    private readonly ICompanyRepository _companies;
    private readonly IEngagementRepository _engagements;
    private readonly IPersonRepository _people;
    private readonly IQualificationCardRepository _cards;
    private readonly IQualificationService _qualifications;
    private readonly IDecisionService _decisions;

    /// <summary>Creates the service over its repositories and the qualification/decision services.</summary>
    public WorkforceService(
        ICompanyRepository companies,
        IEngagementRepository engagements,
        IPersonRepository people,
        IQualificationCardRepository cards,
        IQualificationService qualifications,
        IDecisionService decisions)
    {
        _companies = companies;
        _engagements = engagements;
        _people = people;
        _cards = cards;
        _qualifications = qualifications;
        _decisions = decisions;
    }

    /// <summary>Today's date for card-status evaluation (UTC; card expiry is date-only, R11).</summary>
    private static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    /// <summary>Returns every active operative across all companies, with compliance and next expiry.</summary>
    public async Task<IReadOnlyList<OperativeListItemDto>> ListOperativesAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);

        // Gather every active engagement first, then fetch all cards in one batched read (avoids per-person N+1).
        var rows = new List<(Engagement Engagement, string Company)>();
        foreach (var company in companies)
        {
            var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
            rows.AddRange(engagements.Select(e => (e, company.Name)));
        }

        var cardsByPerson = await GetCurrentCardsByPersonAsync(rows.Select(r => r.Engagement.PersonId), cancellationToken);

        return rows
            .Select(r =>
            {
                var current = cardsByPerson.GetValueOrDefault(r.Engagement.PersonId) ?? (IReadOnlyList<QualificationCard>)Array.Empty<QualificationCard>();
                var (state, _) = ComplianceRollup.FromCards(current, Today);
                var nextExpiry = current
                    .Where(c => c.ExpiresOn is not null)
                    .Select(c => c.ExpiresOn!.Value)
                    .DefaultIfEmpty()
                    .Min();

                return new OperativeListItemDto(
                    r.Engagement.PersonId, r.Engagement.Id, Slug.From(r.Engagement.Name), r.Engagement.Name, r.Engagement.Trade, r.Company,
                    state, ComplianceRollup.Label(state),
                    nextExpiry == default ? null : nextExpiry);
            })
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Returns an operative's full profile by slug, or null when no active operative matches.</summary>
    public async Task<OperativeDetailDto?> GetOperativeBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        foreach (var company in companies)
        {
            var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
            var engagement = engagements.FirstOrDefault(e => Slug.From(e.Name) == slug);
            if (engagement is null)
            {
                continue;
            }

            var person = await _people.GetByIdAsync(engagement.PersonId, cancellationToken);

            var current = await GetCurrentCardsAsync(engagement.PersonId, cancellationToken);
            var (state, _) = ComplianceRollup.FromCards(current, Today);

            var cards = await _qualifications.GetCardsForPersonAsync(engagement.PersonId, cancellationToken);
            var qualifications = cards
                .Where(c => !c.IsSuperseded)
                .Select(c => new OperativeQualificationDto(
                    c.QualificationName, c.Issuer, c.IssuedOn, c.ExpiresOn, c.State, c.StatusLabel))
                .ToList();

            var decisions = await _decisions.GetForPersonAsync(engagement.PersonId, cancellationToken);
            var history = decisions
                .Select(d => new OperativeHistoryDto(
                    d.OccurredUtc,
                    d.Admitted ? "Site entry — admitted" : "Site entry — blocked",
                    d.Checks.FirstOrDefault(c => c.Outcome == "Failed")?.Detail))
                .ToList();

            return new OperativeDetailDto(
                engagement.PersonId,
                Slug.From(engagement.Name),
                engagement.Name,
                engagement.Trade,
                company.Name,
                person?.PhoneNumber.Value,
                state,
                ComplianceRollup.Label(state),
                qualifications,
                history);
        }

        return null;
    }

    /// <summary>Returns a person's current (non-superseded) cards — the input to the compliance roll-up (SF-8/SF-10).</summary>
    private async Task<IReadOnlyList<QualificationCard>> GetCurrentCardsAsync(Guid personId, CancellationToken cancellationToken)
    {
        var cards = await _cards.GetByPersonAsync(personId, cancellationToken);
        return cards.Where(c => !c.IsSuperseded).ToList();
    }

    /// <summary>Batched: current (non-superseded) cards grouped by person, from one read (avoids N+1).</summary>
    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<QualificationCard>>> GetCurrentCardsByPersonAsync(IEnumerable<Guid> personIds, CancellationToken cancellationToken)
    {
        var ids = personIds.Distinct().ToList();
        var cards = await _cards.GetByPersonsAsync(ids, cancellationToken);
        return cards
            .Where(c => !c.IsSuperseded)
            .GroupBy(c => c.PersonId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<QualificationCard>)g.ToList());
    }
}
