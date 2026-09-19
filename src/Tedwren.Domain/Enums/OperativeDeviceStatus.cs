namespace Tedwren.Domain.Enums;

/// <summary>
/// The lifecycle state of an operative's bound device (M2). One operative is bound to one active device (a
/// buddy-punching deterrent); an administrator revokes a device when the operative changes phone, freeing them
/// to enrol a new one.
/// </summary>
public enum OperativeDeviceStatus
{
    /// <summary>Active: the device is the operative's current bound device and may hold a session.</summary>
    Active = 0,

    /// <summary>Revoked: the binding was withdrawn (e.g. phone changed); its refresh token no longer works.</summary>
    Revoked = 1,
}
