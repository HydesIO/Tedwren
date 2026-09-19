using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Sites;

namespace Tedwren.Abstractions.Contracts.Mobile;

/// <summary>
/// A site an operative can sign in at, carrying its geofence and any dispersed properties (SF-14/SF-26), so the
/// device can cache boundaries for offline sign-in (M3 read cache; used by M4 attendance). Folds the boundary
/// that <see cref="SiteSummary"/> omits into a list shape.
/// </summary>
public sealed record MobileSiteDto(
    Guid Id,
    string Slug,
    string Name,
    string? Region,
    bool HasCompound,
    bool IsDispersed,
    GeofenceDto? Boundary,
    IReadOnlyList<SitePropertyDto> Properties);

/// <summary>
/// The operative home dashboard overview (M3): identity, compliance and hours this week (SUB-27). The signed-in
/// state and current site are populated in M4 (attendance); the forms-due count in M6 (forms engine).
/// </summary>
public sealed record OperativeDashboardDto(
    string Name,
    ComplianceState ComplianceState,
    string ComplianceLabel,
    decimal HoursThisWeek,
    DateOnly? NextExpiry,
    int FormsDue,
    bool SignedIn,
    Guid? CurrentSiteId,
    string? CurrentSiteName);
