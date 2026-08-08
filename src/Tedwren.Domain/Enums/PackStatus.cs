namespace Tedwren.Domain.Enums;

/// <summary>
/// The state of a shared compliance pack (SUB-17–SUB-21). A pack is fixed at send (R7); it can be revoked
/// (SUB-21), superseded by a re-issue (R7), or lapse once past its expiry (SUB-18).
/// </summary>
public enum PackStatus
{
    /// <summary>Live and accessible with the passcode until it expires.</summary>
    Active = 0,

    /// <summary>Withdrawn by the sender — no longer accessible (SUB-21).</summary>
    Revoked = 1,

    /// <summary>Replaced by a newer re-issued pack (R7).</summary>
    Superseded = 2,

    /// <summary>Past its expiry date — no longer accessible (SUB-18).</summary>
    Expired = 3,
}
