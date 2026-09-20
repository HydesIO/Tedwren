namespace Tedwren.Abstractions.Contracts.Evidence;

/// <summary>
/// A manager-facing view of an operative's field evidence capture (the M5 <c>EvidenceItem</c>), for the M7
/// review surface. Scoped to the reviewing manager's company (R15). <see cref="PersonName"/> is the company's own
/// recorded name for the capturer (from the engagement — names are never reconciled across companies, PRD §5.1).
/// The photo is referenced only by its opaque image-store id: fetch it through the authorised
/// <c>GET /api/images/{id}</c> route, never a permanent public URL (R9). Times are stored UTC and shown in UK
/// local time (R11).
/// </summary>
public sealed record EvidenceCaptureDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string? Note,
    double? Latitude,
    double? Longitude,
    string? PhotoReference,
    DateTimeOffset CapturedUtc,
    DateTimeOffset CreatedUtc);
