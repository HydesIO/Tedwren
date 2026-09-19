using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A device bound to an operative for the mobile app (M2). Enforces "one operative per device, one active
/// device per operative" (SF-1; a buddy-punching deterrent, not identity proof — face-match at sign-in is PRD
/// Phase 5, R17). Holds the hash of the current device-bound refresh token; biometric unlock is on-device only.
/// </summary>
public sealed class OperativeDevice
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The operative this device is bound to (SF-1). Settable so a revoked device can be re-bound.</summary>
    public required Guid PersonId { get; set; }

    /// <summary>The tenant company the operative's token is scoped to (R15); follows the resolved engagement.</summary>
    public required Guid CompanyId { get; set; }

    /// <summary>The opaque per-install device id supplied by the app; unique across devices.</summary>
    public required string DeviceId { get; init; }

    /// <summary>A human-readable device name (e.g. the phone model), for the admin device list.</summary>
    public string? DeviceName { get; set; }

    /// <summary>Whether the binding is active or has been revoked by an administrator.</summary>
    public OperativeDeviceStatus Status { get; set; } = OperativeDeviceStatus.Active;

    /// <summary>PBKDF2 hash of the current (rotating) refresh token; null once signed out/revoked.</summary>
    public string? RefreshTokenHash { get; set; }

    /// <summary>When the current refresh token expires (UTC); null when there is no live token.</summary>
    public DateTimeOffset? RefreshTokenExpiresUtc { get; set; }

    /// <summary>When the device was first enrolled (UTC).</summary>
    public DateTimeOffset EnrolledUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the device last authenticated (UTC).</summary>
    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the device can currently hold a session: active with an unexpired refresh token at <paramref name="asOf"/>.</summary>
    public bool CanRefresh(DateTimeOffset asOf) =>
        Status == OperativeDeviceStatus.Active && RefreshTokenHash is not null && RefreshTokenExpiresUtc > asOf;
}
