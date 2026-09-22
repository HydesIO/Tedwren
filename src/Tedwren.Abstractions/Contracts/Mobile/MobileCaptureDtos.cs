namespace Tedwren.Abstractions.Contracts.Mobile;

/// <summary>
/// The result of a mobile binary upload (M5): the opaque image-store reference the caller then attaches to a
/// record (an evidence item or hazard report). The bytes are served only through the authorised image route, never
/// a permanent public URL (R9).
/// </summary>
public sealed record MobileUploadResultDto(string Reference);

/// <summary>
/// An operative's offline-captured evidence item (M5) — a photo, a note and where/when it was taken. Ungated:
/// available to every operative. <see cref="ClientId"/> is the device-generated id that doubles as the record id
/// and the idempotency key, so a retried sync never creates a duplicate (R4/R16). Times are captured in UTC (R11);
/// <see cref="PhotoReference"/> is the reference returned by <c>/api/mobile/uploads</c> (null when no photo).
/// </summary>
public sealed record MobileReportEvidenceRequest(
    Guid ClientId,
    string? Note,
    double? Latitude,
    double? Longitude,
    string? PhotoReference,
    DateTimeOffset CapturedUtc);

/// <summary>
/// An operative's offline-captured qualification card / accreditation (Subcontractor Onboarding spec Stage 4,
/// Gate 3). The operative picks a <see cref="QualificationTypeId"/> and photographs the card; it is stored for a
/// manager to confirm (SF-6), never auto-accepted. <see cref="ClientId"/> is the device-generated id + idempotency
/// key so a retried sync never duplicates the card (R4/R16); <see cref="PhotoReference"/> is the reference returned
/// by <c>/api/mobile/uploads</c> (null when no photo); <see cref="CapturedUtc"/> stamps the capture (R11, UTC).
/// </summary>
public sealed record MobileCaptureCardRequest(
    Guid ClientId,
    Guid QualificationTypeId,
    string? CardNumber,
    string? HolderName,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    string? PhotoReference,
    DateTimeOffset CapturedUtc);

/// <summary>A stored evidence item for the operative's confirmation / a future review surface. Photo is exposed only as a flag (R9).</summary>
public sealed record EvidenceItemDto(
    Guid Id,
    string? Note,
    double? Latitude,
    double? Longitude,
    bool HasPhoto,
    DateTimeOffset CapturedUtc,
    DateTimeOffset CreatedUtc);

/// <summary>
/// An operative's offline-captured hazard / near-miss report (M5), reusing the existing HSE hazard domain (PRD
/// §8.2) — gated by the <c>hse</c> module. As with evidence, <see cref="ClientId"/> is the device-generated id
/// that doubles as the record id + idempotency key (R4/R16); the photo is pre-uploaded (<see cref="PhotoReference"/>);
/// <see cref="CapturedUtc"/> stamps when it was captured on the device (R11). <see cref="Kind"/>/<see cref="Severity"/>
/// are enum names, parsed leniently server-side.
/// </summary>
public sealed record MobileReportHazardRequest(
    Guid ClientId,
    string Kind,
    string Description,
    string? Location,
    double? Latitude,
    double? Longitude,
    string? PhotoReference,
    string? Severity,
    string? Category,
    DateTimeOffset CapturedUtc);
