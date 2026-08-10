namespace Tedwren.Domain.Enums;

/// <summary>
/// The lifecycle state of a console user account. A user is <see cref="Invited"/> until they accept, then
/// <see cref="Active"/>; access can be withdrawn without deleting the account by moving it to
/// <see cref="Suspended"/> (SF-20 keeps the audit trail intact — accounts are disabled, not erased).
/// </summary>
public enum UserStatus
{
    /// <summary>Invitation sent; the user has not yet accepted.</summary>
    Invited = 0,

    /// <summary>Active account with console access.</summary>
    Active = 1,

    /// <summary>Access withdrawn; the account and its history are retained.</summary>
    Suspended = 2,
}
