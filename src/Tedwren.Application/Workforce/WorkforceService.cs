using Tedwren.Abstractions;
using Tedwren.Abstractions.Common;
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
    private readonly IInductionSessionRepository _inductions;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>
    /// Creates the service over its repositories and the qualification/decision services.
    /// <paramref name="currentUser"/> supplies the signed-in tenant so the register is scoped to the
    /// caller's company (R15); it is optional so unit tests that construct the service directly run unscoped.
    /// <paramref name="inductions"/> lets a main contractor's site-readiness reflect induction validity (MC-8).
    /// </summary>
    public WorkforceService(
        ICompanyRepository companies,
        IEngagementRepository engagements,
        IPersonRepository people,
        IQualificationCardRepository cards,
        IQualificationService qualifications,
        IDecisionService decisions,
        IInductionSessionRepository inductions,
        ICurrentUserService? currentUser = null)
    {
        _companies = companies;
        _engagements = engagements;
        _people = people;
        _cards = cards;
        _qualifications = qualifications;
        _decisions = decisions;
        _inductions = inductions;
        _currentUser = currentUser;
    }

    /// <summary>Today's date for card-status evaluation (UTC; card expiry is date-only, R11).</summary>
    private static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    /// <summary>
    /// Returns the companies in the caller's tenant scope (R15): just the signed-in company when resolved,
    /// otherwise every company (unit tests / unauthenticated run unscoped rather than showing nothing).
    /// </summary>
    private async Task<IReadOnlyList<Company>> ScopedCompaniesAsync(CancellationToken cancellationToken)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        if (_currentUser is null)
        {
            return companies;
        }
        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return user.CompanyId is { } tenant
            ? companies.Where(c => c.Id == tenant).ToList()
            : companies;
    }

    /// <summary>Returns every active operative in the caller's tenant, with compliance and next expiry (R15).</summary>
    public async Task<IReadOnlyList<OperativeListItemDto>> ListOperativesAsync(CancellationToken cancellationToken = default)
    {
        var companies = await ScopedCompaniesAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        // Gather every active engagement first, then fetch all cards in one batched read (avoids per-person N+1).
        // For a main contractor, also load its induction sessions once so site-readiness can reflect a missing or
        // expired induction (MC-8) without a per-operative lookup.
        var rows = new List<(Engagement Engagement, Company Company)>();
        var validInduction = new HashSet<(Guid Company, Guid Person)>();
        foreach (var company in companies)
        {
            var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
            rows.AddRange(engagements.Select(e => (e, company)));

            if (InductionApplies(company))
            {
                var sessions = await _inductions.GetByCompanyAsync(company.Id, cancellationToken);
                foreach (var s in sessions.Where(s => s.IsValid(now)))
                {
                    validInduction.Add((company.Id, s.PersonId));
                }
            }
        }

        var cardsByPerson = await GetCurrentCardsByPersonAsync(rows.Select(r => r.Engagement.PersonId), cancellationToken);

        return rows
            .Select(r =>
            {
                var current = cardsByPerson.GetValueOrDefault(r.Engagement.PersonId) ?? (IReadOnlyList<QualificationCard>)Array.Empty<QualificationCard>();
                var (cardState, _) = ComplianceRollup.FromCards(current, Today);
                var applies = InductionApplies(r.Company);
                var inductionValid = !applies || validInduction.Contains((r.Company.Id, r.Engagement.PersonId));
                var (state, label) = ApplyInduction(cardState, applies, inductionValid);
                var nextExpiry = current
                    .Where(c => c.ExpiresOn is not null)
                    .Select(c => c.ExpiresOn!.Value)
                    .DefaultIfEmpty()
                    .Min();

                return new OperativeListItemDto(
                    r.Engagement.PersonId, r.Engagement.Id, Slug.From(r.Engagement.Name), r.Engagement.Name, r.Engagement.Trade, r.Company.Name,
                    state, label,
                    nextExpiry == default ? null : nextExpiry);
            })
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Returns an operative's full profile by slug, or null when no active operative matches.</summary>
    public async Task<OperativeDetailDto?> GetOperativeBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var companies = await ScopedCompaniesAsync(cancellationToken);
        foreach (var company in companies)
        {
            var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
            var engagement = engagements.FirstOrDefault(e => Slug.From(e.Name) == slug);
            if (engagement is not null)
            {
                return await BuildDetailAsync(company, engagement, cancellationToken);
            }
        }

        return null;
    }

    /// <summary>
    /// Returns an operative's profile by the engaging company and engagement id, or null when none matches.
    /// Opens an operative from the Organisation page, where the operative may belong to a company other than
    /// the signed-in tenant. The engagement read is scoped to the supplied company by the repository (R15).
    /// </summary>
    public async Task<OperativeDetailDto?> GetOperativeByEngagementAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default)
    {
        var engagement = await _engagements.GetAsync(companyId, engagementId, cancellationToken);
        if (engagement is null)
        {
            return null;
        }

        var company = await _companies.GetByIdAsync(companyId, cancellationToken);
        return company is null ? null : await BuildDetailAsync(company, engagement, cancellationToken);
    }

    /// <summary>Composes the full operative-detail shape for one engagement: profile, current-card compliance
    /// (SF-8), qualifications and site-entry history. Shared by the slug and engagement-id lookups.</summary>
    private async Task<OperativeDetailDto> BuildDetailAsync(Company company, Engagement engagement, CancellationToken cancellationToken)
    {
        var person = await _people.GetByIdAsync(engagement.PersonId, cancellationToken);

        var current = await GetCurrentCardsAsync(engagement.PersonId, cancellationToken);
        var (cardState, _) = ComplianceRollup.FromCards(current, Today);

        var cards = await _qualifications.GetCardsForPersonAsync(engagement.PersonId, cancellationToken);
        var qualifications = cards
            .Where(c => !c.IsSuperseded)
            .Select(c => new OperativeQualificationDto(
                c.QualificationName, c.Issuer, c.IssuedOn, c.ExpiresOn, c.State, c.StatusLabel, c.ImageReference))
            .ToList();

        var decisions = await _decisions.GetForPersonAsync(engagement.PersonId, cancellationToken);
        var history = decisions
            .Select(d => new OperativeHistoryDto(
                d.OccurredUtc,
                d.Admitted ? "Site entry — admitted" : "Site entry — blocked",
                d.Checks.FirstOrDefault(c => c.Outcome == "Failed")?.Detail))
            .ToList();

        // Induction status (MC-8): a valid induction is a site-entry condition for a main contractor, but a
        // subcontractor holds no induction (SUB-11). Fold it into the reported status so a card-compliant worker
        // with no induction no longer shows "Compliant" while the gate blocks them (UAT-014).
        var now = DateTimeOffset.UtcNow;
        var applies = InductionApplies(company);
        var latestInduction = applies
            ? await _inductions.GetLatestPassedForPersonAsync(company.Id, engagement.PersonId, cancellationToken)
            : null;
        var inductionValid = latestInduction?.IsValid(now) ?? false;
        var (state, label) = ApplyInduction(cardState, applies, inductionValid);

        return new OperativeDetailDto(
            engagement.PersonId,
            engagement.Id,
            company.Id,
            Slug.From(engagement.Name),
            engagement.Name,
            engagement.Trade,
            company.Name,
            person?.PhoneNumber.Value,
            state,
            label,
            qualifications,
            history,
            applies,
            inductionValid,
            InductionStatusLabel(applies, latestInduction, now));
    }

    /// <summary>Whether induction validity is a site-readiness condition for this company: yes for a main
    /// contractor (MC-8), no for a subcontractor, which holds no induction (SUB-11).</summary>
    private static bool InductionApplies(Company company)
        => company.OrgType == Tedwren.Domain.Enums.OrgType.MainContractor;

    /// <summary>
    /// Folds induction validity into the reported compliance status (UAT-014). When induction applies and is not
    /// valid, an otherwise-compliant worker is reported as "Induction required" (at risk) — they are not
    /// site-ready (MC-8); a worse card state already shows a problem and is kept as-is.
    /// </summary>
    private static (ComplianceState State, string Label) ApplyInduction(ComplianceState cardState, bool applies, bool inductionValid)
    {
        if (!applies || inductionValid)
        {
            return (cardState, ComplianceRollup.Label(cardState));
        }

        return cardState == ComplianceState.Compliant
            ? (ComplianceState.AtRisk, "Induction required")
            : (cardState, ComplianceRollup.Label(cardState));
    }

    /// <summary>A human-readable induction status for the operative record (MC-7/MC-8).</summary>
    private static string InductionStatusLabel(bool applies, InductionSession? latest, DateTimeOffset now)
    {
        if (!applies)
        {
            return "Not applicable";
        }
        if (latest is null)
        {
            return "Not completed";
        }
        return latest.IsValid(now)
            ? (latest.ExpiresUtc is { } expiry ? $"Valid until {expiry:dd MMM yyyy}" : "Valid")
            : "Expired — re-induction required";
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
