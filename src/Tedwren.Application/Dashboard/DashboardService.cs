using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Dashboard;

/// <summary>
/// Aggregates the dashboard read model. Data-store agnostic: it composes the same organisation,
/// qualification-card, site and expiry repositories/services the rest of the application uses. Compliance
/// is derived from current cards via <see cref="ComplianceRollup"/> (SF-8), never invented.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly ICompanyRepository _companies;
    private readonly IEngagementRepository _engagements;
    private readonly IQualificationCardRepository _cards;
    private readonly ISiteService _sites;
    private readonly IExpiryQueryService _expiry;

    /// <summary>How many days ahead the "upcoming expiries" KPI looks.</summary>
    private const int UpcomingExpiryWindowDays = 30;

    /// <summary>Creates the service over its repositories and the site/expiry services.</summary>
    public DashboardService(
        ICompanyRepository companies,
        IEngagementRepository engagements,
        IQualificationCardRepository cards,
        ISiteService sites,
        IExpiryQueryService expiry)
    {
        _companies = companies;
        _engagements = engagements;
        _cards = cards;
        _sites = sites;
        _expiry = expiry;
    }

    /// <summary>Today's date for card-status evaluation (UTC; card expiry is date-only, R11).</summary>
    private static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    /// <summary>Returns the full dashboard summary (KPIs, compliance breakdown, site risk).</summary>
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var breakdown = await ComputeComplianceAsync(cancellationToken);
        var sites = await _sites.GetSitesAsync(cancellationToken);
        var upcoming = await _expiry.GetUpcomingAsync(UpcomingExpiryWindowDays, cancellationToken);
        var companyCount = (await _companies.GetAllAsync(cancellationToken)).Count;

        var kpis = new DashboardKpisDto(
            Companies: companyCount,
            Operatives: breakdown.Total,
            Sites: sites.Count,
            CompliancePercent: breakdown.CompliancePercent,
            UpcomingExpiries: upcoming.Count);

        var siteRisk = sites
            .Select(s => new SiteRiskRowDto(
                s.Name, s.Slug, s.Operatives, s.CompliancePercent, s.State, s.StatusLabel, s.Risk))
            .ToList();

        return new DashboardSummaryDto(kpis, breakdown, siteRisk);
    }

    /// <summary>Returns just the workforce compliance breakdown (for the Compliance page).</summary>
    public async Task<ComplianceBreakdownDto> GetComplianceAsync(CancellationToken cancellationToken = default) =>
        await ComputeComplianceAsync(cancellationToken);

    /// <summary>Tallies every active operative's compliance state from their current cards (SF-8).</summary>
    private async Task<ComplianceBreakdownDto> ComputeComplianceAsync(CancellationToken cancellationToken)
    {
        var companies = await _companies.GetAllAsync(cancellationToken);
        int compliant = 0, atRisk = 0, nonCompliant = 0, pending = 0, total = 0;

        // Collect every active operative's person id, then fetch all cards in one batched read (avoids N+1).
        var personIds = new List<Guid>();
        foreach (var company in companies)
        {
            var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
            personIds.AddRange(engagements.Select(e => e.PersonId));
        }

        var cardsByPerson = (await _cards.GetByPersonsAsync(personIds.Distinct().ToList(), cancellationToken))
            .Where(c => !c.IsSuperseded)
            .GroupBy(c => c.PersonId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<QualificationCard>)g.ToList());

        foreach (var personId in personIds)
        {
            total++;
            var current = cardsByPerson.GetValueOrDefault(personId) ?? (IReadOnlyList<QualificationCard>)Array.Empty<QualificationCard>();
            var (state, _) = ComplianceRollup.FromCards(current, Today);
            switch (state)
            {
                case ComplianceState.Compliant: compliant++; break;
                case ComplianceState.AtRisk: atRisk++; break;
                case ComplianceState.NonCompliant: nonCompliant++; break;
                default: pending++; break;
            }
        }

        double? percent = total == 0 ? null : Math.Round(100.0 * compliant / total);
        return new ComplianceBreakdownDto(percent, compliant, atRisk, nonCompliant, pending, total);
    }
}
