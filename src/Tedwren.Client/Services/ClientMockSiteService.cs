using Tedwren.Abstractions;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Services;
using Tedwren.UiComponents.Models;
using Tedwren.UiComponents.SampleData;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ISiteService"/> for client <c>DataSource=Mock</c>. It wraps the existing sample-data services
/// and maps their records to the shared DTOs, so the Sites screen looks exactly as before. Writes are
/// demo-only (the sample data is static), matching the prior "nothing is persisted" behaviour.
/// </summary>
public sealed class ClientMockSiteService : ISiteService
{
    private readonly IListSampleDataService _list;
    private readonly IDetailSampleDataService _detail;

    /// <summary>Creates the service over the sample-data services.</summary>
    public ClientMockSiteService(IListSampleDataService list, IDetailSampleDataService detail)
    {
        _list = list;
        _detail = detail;
    }

    /// <summary>Maps the sample site list to summaries.</summary>
    public Task<IReadOnlyList<SiteSummary>> GetSitesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SiteSummary> sites = _list.GetSites()
            .Select(s => new SiteSummary(
                Guid.Empty, Slug.From(s.Name), s.Name, s.Client, s.Region, s.Operatives,
                s.CompliancePercent, ToState(s.Status), s.StatusLabel, ToRiskState(s.Risk),
                IsDispersed: false, PropertyCount: 0))
            .ToList();
        return Task.FromResult(sites);
    }

    /// <summary>Maps a sample site detail to the DTO, or null when the slug is unknown.</summary>
    public Task<SiteDetailDto?> GetSiteAsync(string slug, CancellationToken cancellationToken = default)
    {
        var site = _detail.GetSite(slug);
        if (site is null)
        {
            return Task.FromResult<SiteDetailDto?>(null);
        }

        // Sample properties carry no coordinates; a nominal boundary keeps the DTO valid for display.
        var properties = site.Properties
            .Select(p => new SitePropertyDto(Guid.Empty, p.Address, p.Units, new GeofenceDto(0, 0, 1)))
            .ToList();

        var dto = new SiteDetailDto(
            Guid.Empty, slug, site.Name, site.Client, site.Region, site.Address,
            HasCompound: true, IsDispersed: site.Properties.Count > 0, Boundary: null,
            ToState(site.Status), site.StatusLabel, site.CompliancePercent, properties);
        return Task.FromResult<SiteDetailDto?>(dto);
    }

    /// <summary>Demo record — sample data is static, so nothing is persisted.</summary>
    public Task<Guid> CreateSiteAsync(CreateSiteRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());

    /// <summary>Demo add — not persisted in mock mode.</summary>
    public Task<Guid?> AddPropertyAsync(AddSitePropertyRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(Guid.NewGuid());

    /// <summary>Maps the sample UI status kind to the neutral compliance state.</summary>
    private static ComplianceState ToState(StatusKind status) => status switch
    {
        StatusKind.Success => ComplianceState.Compliant,
        StatusKind.Warning => ComplianceState.AtRisk,
        StatusKind.Danger => ComplianceState.NonCompliant,
        _ => ComplianceState.Pending,
    };

    /// <summary>Maps the sample UI risk severity to the neutral risk state.</summary>
    private static RiskState ToRiskState(RiskSeverity risk) => risk switch
    {
        RiskSeverity.High => RiskState.High,
        RiskSeverity.Medium => RiskState.Medium,
        _ => RiskState.Low,
    };
}
