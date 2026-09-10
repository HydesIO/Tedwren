using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A console user account belonging to a customer company (SF-20, SF-23, Q2). A user has an
/// <see cref="AccessRole"/> that decides what they may do — including the read-only auditor role (SF-23) —
/// and a <see cref="UserStatus"/> so access can be withdrawn without erasing the account or its audit
/// history. Reads and writes are scoped to the owning company (R15).
/// </summary>
public sealed class User
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company the user belongs to (scoped, R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Full name.</summary>
    public required string Name { get; set; }

    /// <summary>Email address — the invitation is sent here and it is the sign-in identity.</summary>
    public required string Email { get; set; }

    /// <summary>Optional mobile contact number, set by the user from their own profile page. Free text.</summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// Optional avatar image reference (an <see cref="Tedwren.Domain"/>-agnostic <c>IImageStore</c> id), served
    /// via the authorised <c>GET /api/images/{id}</c> endpoint like other private assets (R9). Null when unset.
    /// </summary>
    public string? AvatarImageReference { get; set; }

    /// <summary>The console access role, which sets what the user may do (SF-23).</summary>
    public AccessRole Role { get; set; } = AccessRole.Auditor;

    /// <summary>Account lifecycle state (invited / active / suspended).</summary>
    public UserStatus Status { get; set; } = UserStatus.Invited;

    /// <summary>When the account was created / invited (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the user was last active, if ever (UTC).</summary>
    public DateTimeOffset? LastActiveUtc { get; set; }

    /// <summary>The salted password hash, set when the invitee accepts and chooses a password. Null until then.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>When the password was last set (UTC). Null until the invite is accepted.</summary>
    public DateTimeOffset? PasswordSetUtc { get; set; }

    /// <summary>The one-time invite token used to accept the invitation and set a password. Cleared once used.</summary>
    public string? InviteToken { get; set; }

    /// <summary>When the invite token expires (UTC). After this the invite must be re-sent.</summary>
    public DateTimeOffset? InviteTokenExpiresUtc { get; set; }
}
