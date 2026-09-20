namespace Tedwren.Abstractions.Contracts.Mobile;

/// <summary>
/// A form assigned to the operative, resolved for the mobile "forms due" inbox (M6). Each carries the assignment's
/// scope + schedule and the <see cref="TemplateVersionId"/> of the family's latest <b>published</b> version — an
/// immutable id the device caches and fills offline. The client computes due/overdue from <see cref="Schedule"/>
/// plus its own last-submitted history (one-device-per-operative makes local history authoritative).
/// </summary>
public sealed record MobileFormAssignmentDto(
    Guid AssignmentId,
    Guid FamilyId,
    Guid TemplateVersionId,
    string FormName,
    string Scope,
    Guid? SiteId,
    string? SiteName,
    string Schedule);
