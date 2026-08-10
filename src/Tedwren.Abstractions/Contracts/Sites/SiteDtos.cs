using Tedwren.Abstractions.Common;

namespace Tedwren.Abstractions.Contracts.Sites;

/// <summary>A circular boundary (SF-14): centre point and radius in metres.</summary>
public sealed record GeofenceDto(double CentreLatitude, double CentreLongitude, double RadiusMetres);

/// <summary>A site row for the Sites list. Field names mirror the existing UI record.</summary>
public sealed record SiteSummary(
    Guid Id,
    string Slug,
    string Name,
    string? Client,
    string? Region,
    int Operatives,
    double? CompliancePercent,
    ComplianceState State,
    string StatusLabel,
    RiskState Risk,
    bool IsDispersed,
    int PropertyCount);

/// <summary>A property within a dispersed scheme (SF-26).</summary>
public sealed record SitePropertyDto(Guid Id, string Address, int Units, GeofenceDto Boundary);

/// <summary>Full site record for the detail page/endpoint.</summary>
public sealed record SiteDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Client,
    string? Region,
    string? Address,
    bool HasCompound,
    bool IsDispersed,
    GeofenceDto? Boundary,
    ComplianceState State,
    string StatusLabel,
    double? CompliancePercent,
    IReadOnlyList<SitePropertyDto> Properties);

/// <summary>Request to record a site (SF-6). Recording a site is unlimited and never billed.</summary>
public sealed record CreateSiteRequest(
    Guid CompanyId,
    string Name,
    string? Client,
    string? Region,
    string? Address,
    bool HasCompound,
    bool IsDispersed,
    GeofenceDto? Boundary);

/// <summary>Request to add a property to a dispersed scheme (SF-26). Nothing is installed at the property.</summary>
public sealed record AddSitePropertyRequest(Guid SiteId, string Address, int Units, GeofenceDto Boundary);
