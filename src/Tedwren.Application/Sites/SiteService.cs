using Tedwren.Abstractions;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Organisation;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Sites;

/// <summary>
/// The single implementation of the sites business rules (SF-6/SF-14/SF-25/SF-26). Data-store agnostic: the
/// same logic runs over the in-memory and Dapper repositories. Recording a site is unlimited and never
/// billed. A site's operative count and compliance are derived from the attendance log (the operatives who
/// have attended, MC-12) and their current cards via <see cref="ComplianceRollup"/> (SF-8) — never invented.
/// </summary>
public sealed class SiteService : ISiteService
{
    private readonly ISiteRepository _sites;
    private readonly ISitePropertyRepository _properties;
    private readonly IAttendanceRepository _attendance;
    private readonly IQualificationCardRepository _cards;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>How many recent attendance records to scan when deriving a site's operatives.</summary>
    private const int AttendanceScanSize = 500;

    /// <summary>
    /// Creates the service over its repositories. <paramref name="currentUser"/> supplies the signed-in
    /// tenant so site queries are scoped to the caller's company (R15/MC-21); it is optional so unit tests
    /// that construct the service directly run unscoped.
    /// </summary>
    public SiteService(
        ISiteRepository sites,
        ISitePropertyRepository properties,
        IAttendanceRepository attendance,
        IQualificationCardRepository cards,
        ICurrentUserService? currentUser = null)
    {
        _sites = sites;
        _properties = properties;
        _attendance = attendance;
        _cards = cards;
        _currentUser = currentUser;
    }

    /// <summary>Today's date for card-status evaluation (UTC; card expiry is date-only, R11).</summary>
    private static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    /// <summary>
    /// Resolves the signed-in caller's tenant company (R15). Null when no current user is wired (unit tests)
    /// or the caller is unauthenticated — both of which run unscoped rather than showing an empty screen.
    /// </summary>
    private async Task<Guid?> ResolveTenantAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return null;
        }
        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return user.CompanyId;
    }

    /// <summary>Returns every site the caller's tenant owns as a list-row summary, with property count and derived compliance (R15).</summary>
    public async Task<IReadOnlyList<SiteSummary>> GetSitesAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        var all = await _sites.GetAllAsync(cancellationToken);
        var sites = tenant is null ? all : all.Where(s => s.CompanyId == tenant).ToList();
        var summaries = new List<SiteSummary>(sites.Count);
        foreach (var site in sites)
        {
            var propertyCount = await _properties.CountBySiteAsync(site.Id, cancellationToken);
            var (operatives, percent, state) = await ComputeSiteComplianceAsync(site.Id, cancellationToken);
            summaries.Add(new SiteSummary(
                site.Id, Slug.From(site.Name), site.Name, site.Client, site.Region,
                operatives, percent, state, ComplianceRollup.Label(state),
                RiskFromState(state), site.IsDispersed, propertyCount));
        }

        return summaries;
    }

    /// <summary>
    /// Derives a site's distinct operatives (from the attendance log, MC-12) and their aggregate compliance
    /// from current cards (SF-8). No attendance → no operatives and a Pending state, never invented.
    /// </summary>
    private async Task<(int Operatives, double? Percent, ComplianceState State)> ComputeSiteComplianceAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var records = await _attendance.GetBySiteAsync(siteId, AttendanceScanSize, cancellationToken);
        var personIds = records.Select(r => r.PersonId).Distinct().ToList();
        if (personIds.Count == 0)
        {
            return (0, null, ComplianceState.Pending);
        }

        var cards = await _cards.GetByPersonsAsync(personIds, cancellationToken);
        var current = cards.Where(c => !c.IsSuperseded).ToList();

        var (state, percent) = ComplianceRollup.FromCards(current, Today);
        return (personIds.Count, percent, state);
    }

    /// <summary>Maps a compliance state to a site risk level for the heatmap.</summary>
    private static RiskState RiskFromState(ComplianceState state) => state switch
    {
        ComplianceState.NonCompliant => RiskState.High,
        ComplianceState.AtRisk => RiskState.Medium,
        _ => RiskState.Low,
    };

    /// <summary>Returns the full site for a slug, or null when no site matches.</summary>
    public async Task<SiteDetailDto?> GetSiteAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        var sites = await _sites.GetAllAsync(cancellationToken);
        var site = sites.FirstOrDefault(s => Slug.From(s.Name) == slug);
        if (site is null)
        {
            return null;
        }

        // MC-21: a site outside the caller's tenant fails visibly (404), never leaks across companies (R15).
        if (tenant is not null && site.CompanyId != tenant)
        {
            return null;
        }

        var properties = await _properties.GetBySiteAsync(site.Id, cancellationToken);
        var (_, percent, state) = await ComputeSiteComplianceAsync(site.Id, cancellationToken);
        return new SiteDetailDto(
            site.Id,
            Slug.From(site.Name),
            site.Name,
            site.Client,
            site.Region,
            site.Address,
            site.HasCompound,
            site.IsDispersed,
            ToDto(site.Boundary),
            state,
            ComplianceRollup.Label(state),
            CompliancePercent: percent,
            properties.Select(p => new SitePropertyDto(p.Id, p.Address, p.Units, ToDto(p.Boundary)!)).ToList());
    }

    /// <summary>Records a site and returns its new identifier (SF-6).</summary>
    public async Task<Guid> CreateSiteAsync(CreateSiteRequest request, CancellationToken cancellationToken = default)
    {
        var site = new Site
        {
            CompanyId = request.CompanyId,
            Name = request.Name,
            Client = request.Client,
            Region = request.Region,
            Address = request.Address,
            HasCompound = request.HasCompound,
            IsDispersed = request.IsDispersed,
            Boundary = ToDomain(request.Boundary),
        };

        await _sites.AddAsync(site, cancellationToken);
        return site.Id;
    }

    /// <summary>Adds a property to a scheme (SF-26), marking the site dispersed. Null if the site is not found.</summary>
    public async Task<Guid?> AddPropertyAsync(AddSitePropertyRequest request, CancellationToken cancellationToken = default)
    {
        var site = await _sites.GetByIdAsync(request.SiteId, cancellationToken);
        if (site is null)
        {
            return null;
        }

        var property = new SiteProperty
        {
            SiteId = site.Id,
            Address = request.Address,
            Units = request.Units,
            Boundary = ToDomain(request.Boundary)!,
        };
        await _properties.AddAsync(property, cancellationToken);

        if (!site.IsDispersed)
        {
            site.IsDispersed = true;
            await _sites.UpdateAsync(site, cancellationToken);
        }

        return property.Id;
    }

    /// <summary>Maps a domain geofence to its DTO (or null).</summary>
    private static GeofenceDto? ToDto(Geofence? boundary) =>
        boundary is null ? null : new GeofenceDto(boundary.CentreLatitude, boundary.CentreLongitude, boundary.RadiusMetres);

    /// <summary>Maps a geofence DTO to the domain value object (or null), validating it in the process.</summary>
    private static Geofence? ToDomain(GeofenceDto? boundary) =>
        boundary is null ? null : new Geofence(boundary.CentreLatitude, boundary.CentreLongitude, boundary.RadiusMetres);
}
