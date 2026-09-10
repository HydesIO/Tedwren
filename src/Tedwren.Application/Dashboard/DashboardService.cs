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
    private readonly ICurrentUserService? _currentUser;
    private readonly IAttendanceRepository? _attendance;

    /// <summary>How many days ahead the "upcoming expiries" KPI looks.</summary>
    private const int UpcomingExpiryWindowDays = 30;

    /// <summary>How many recent attendance records to scan when resolving a site's operatives (matches <c>SiteService</c>).</summary>
    private const int AttendanceScanSize = 500;

    /// <summary>
    /// Creates the service over its repositories and the site/expiry services. <paramref name="currentUser"/>
    /// supplies the signed-in tenant so the operative tally is scoped to the caller's company (R15); it is
    /// optional so unit tests that construct the service directly run unscoped. <paramref name="attendance"/>
    /// resolves a site's operatives for the per-site compliance filter (UAT-016) and is likewise optional.
    /// </summary>
    public DashboardService(
        ICompanyRepository companies,
        IEngagementRepository engagements,
        IQualificationCardRepository cards,
        ISiteService sites,
        IExpiryQueryService expiry,
        ICurrentUserService? currentUser = null,
        IAttendanceRepository? attendance = null)
    {
        _companies = companies;
        _engagements = engagements;
        _cards = cards;
        _sites = sites;
        _expiry = expiry;
        _currentUser = currentUser;
        _attendance = attendance;
    }

    /// <summary>Today's date for card-status evaluation (UTC; card expiry is date-only, R11).</summary>
    private static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    /// <summary>Returns the full dashboard summary (KPIs, compliance breakdown, site risk).</summary>
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var breakdown = await ComputeComplianceAsync(siteSlug: null, cancellationToken);
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

    /// <summary>Returns the workforce compliance breakdown, optionally scoped to a single site (UAT-016).</summary>
    public async Task<ComplianceBreakdownDto> GetComplianceAsync(string? siteSlug = null, CancellationToken cancellationToken = default) =>
        await ComputeComplianceAsync(siteSlug, cancellationToken);

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

    /// <summary>
    /// Tallies operatives' compliance state from their current cards (SF-8, R15). With no site it covers every
    /// active operative in the caller's tenant; with a site it covers that site's operatives (UAT-016). The site
    /// is resolved through the role-aware <see cref="ISiteService.GetSiteAsync"/>, so an out-of-scope or unknown
    /// site (e.g. one a Site Manager is not assigned) yields an empty breakdown rather than another site's data.
    /// </summary>
    private async Task<ComplianceBreakdownDto> ComputeComplianceAsync(string? siteSlug, CancellationToken cancellationToken)
    {
        var personIds = string.IsNullOrWhiteSpace(siteSlug)
            ? await TenantPersonIdsAsync(cancellationToken)
            : await SitePersonIdsAsync(siteSlug, cancellationToken);

        return await TallyAsync(personIds, cancellationToken);
    }

    /// <summary>Every active operative's person id across the caller's tenant companies (R15).</summary>
    private async Task<IReadOnlyList<Guid>> TenantPersonIdsAsync(CancellationToken cancellationToken)
    {
        var companies = await ScopedCompaniesAsync(cancellationToken);
        var personIds = new List<Guid>();
        foreach (var company in companies)
        {
            var engagements = await _engagements.GetActiveByCompanyAsync(company.Id, cancellationToken);
            personIds.AddRange(engagements.Select(e => e.PersonId));
        }

        return personIds;
    }

    /// <summary>
    /// The distinct operatives associated with a site, derived from its attendance log (the only operative↔site
    /// link, as <c>SiteService</c> does). Empty when the site is unknown/out of scope or attendance is unwired.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> SitePersonIdsAsync(string siteSlug, CancellationToken cancellationToken)
    {
        // Role-aware resolution: GetSiteAsync returns null for a site outside the caller's tenant/assignment.
        var site = await _sites.GetSiteAsync(siteSlug, cancellationToken);
        if (site is null || _attendance is null)
        {
            return Array.Empty<Guid>();
        }

        var records = await _attendance.GetBySiteAsync(site.Id, AttendanceScanSize, cancellationToken);
        return records.Select(r => r.PersonId).Distinct().ToList();
    }

    /// <summary>Classifies each person once by their lowest current card status and tallies the four buckets (SF-8).</summary>
    private async Task<ComplianceBreakdownDto> TallyAsync(IReadOnlyList<Guid> personIds, CancellationToken cancellationToken)
    {
        int compliant = 0, atRisk = 0, nonCompliant = 0, pending = 0, total = 0;

        // Fetch all cards in one batched read (avoids N+1).
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
