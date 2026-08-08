using Tedwren.Abstractions.Common;

namespace Tedwren.Abstractions.Contracts.Attendance;

/// <summary>A worker's sign-in attempt. Location is optional; the site's policy decides what happens without it (SF-15).</summary>
public sealed record SignInRequest(
    Guid PersonId,
    Guid SiteId,
    Guid? PropertyId,
    double? Latitude,
    double? Longitude,
    SignInMethod Method);

/// <summary>
/// The outcome of a sign-in. <see cref="SignedIn"/> is whether the worker is now recorded present.
/// <see cref="Outcome"/> is "Accepted", "Flagged" or "Refused"; <see cref="SignedInElsewhere"/> names the
/// other site when a sign-in is refused because the worker is already present there (SF-18).
/// </summary>
public sealed record SignInResult(bool SignedIn, string Outcome, string? Reason, Guid RecordId, string? SignedInElsewhere);

/// <summary>A worker's sign-out attempt.</summary>
public sealed record SignOutRequest(
    Guid PersonId,
    Guid SiteId,
    double? Latitude,
    double? Longitude,
    SignInMethod Method);

/// <summary>The outcome of a sign-out, including the duration on site when successful (SF-17).</summary>
public sealed record SignOutResult(bool SignedOut, string Outcome, string? Reason, double? DurationHours, Guid RecordId);

/// <summary>A worker currently present on a site.</summary>
public sealed record OnSiteWorkerDto(Guid PersonId, Guid SiteId, Guid? PropertyId, DateTimeOffset SinceUtc, string Outcome);

/// <summary>An attendance record for the site log.</summary>
public sealed record AttendanceRecordDto(
    Guid Id,
    Guid PersonId,
    Guid SiteId,
    Guid? PropertyId,
    string Type,
    string Outcome,
    string Method,
    double? Latitude,
    double? Longitude,
    bool? WithinBoundary,
    string? Reason,
    DateTimeOffset OccurredUtc);
