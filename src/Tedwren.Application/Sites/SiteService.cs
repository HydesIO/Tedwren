using Tedwren.Abstractions;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Sites;

/// <summary>
/// The single implementation of the sites business rules (SF-6/SF-14/SF-25/SF-26). Data-store agnostic: the
/// same logic runs over the in-memory and Dapper repositories. Recording a site is unlimited and never
/// billed. Operative counts and compliance are <see cref="ComplianceState.Pending"/> until attendance
/// exists (Phase 12) — never invented.
/// </summary>
public sealed class SiteService : ISiteService
{
    private readonly ISiteRepository _sites;
    private readonly ISitePropertyRepository _properties;

    /// <summary>Creates the service over its repositories.</summary>
    public SiteService(ISiteRepository sites, ISitePropertyRepository properties)
    {
        _sites = sites;
        _properties = properties;
    }

    /// <summary>Returns every site as a list-row summary, with its property count.</summary>
    public async Task<IReadOnlyList<SiteSummary>> GetSitesAsync(CancellationToken cancellationToken = default)
    {
        var sites = await _sites.GetAllAsync(cancellationToken);
        var summaries = new List<SiteSummary>(sites.Count);
        foreach (var site in sites)
        {
            var propertyCount = await _properties.CountBySiteAsync(site.Id, cancellationToken);
            summaries.Add(new SiteSummary(
                site.Id, Slug.From(site.Name), site.Name, site.Client, site.Region,
                Operatives: 0, CompliancePercent: null, ComplianceState.Pending, "Pending",
                RiskState.Low, site.IsDispersed, propertyCount));
        }

        return summaries;
    }

    /// <summary>Returns the full site for a slug, or null when no site matches.</summary>
    public async Task<SiteDetailDto?> GetSiteAsync(string slug, CancellationToken cancellationToken = default)
    {
        var sites = await _sites.GetAllAsync(cancellationToken);
        var site = sites.FirstOrDefault(s => Slug.From(s.Name) == slug);
        if (site is null)
        {
            return null;
        }

        var properties = await _properties.GetBySiteAsync(site.Id, cancellationToken);
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
            ComplianceState.Pending,
            "Pending",
            CompliancePercent: null,
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
