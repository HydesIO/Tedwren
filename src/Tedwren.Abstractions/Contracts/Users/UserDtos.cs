namespace Tedwren.Abstractions.Contracts.Users;

/// <summary>A console user row for the Users list and detail view (SF-20/SF-23).</summary>
public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string RoleLabel,
    string Status,
    string StatusLabel,
    bool CanWrite,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? LastActiveUtc);

/// <summary>
/// Request to invite a new console user (SF-20). Sends an invitation to the email address. The user is
/// created against <paramref name="CompanyId"/> so a newly-provisioned organisation can be given its first
/// administrator during onboarding, rather than every invite landing on a single hard-coded company (R15).
/// </summary>
public sealed record InviteUserRequest(Guid CompanyId, string Name, string Email, string Role);

/// <summary>
/// The outcome of an invite: the new user's id, the one-time token for the accept-invite link, and whether
/// the invitation email was actually sent. <see cref="EmailSent"/> is false when delivery is stubbed
/// (outbox provider) or the send failed — the caller can then fall back to sharing the link manually.
/// </summary>
public sealed record InviteUserResult(Guid UserId, string AcceptToken, bool EmailSent);

/// <summary>Request to change an existing user's name and role.</summary>
public sealed record UpdateUserRequest(string Name, string Role);

/// <summary>The available access roles, so the UI can populate its role picker from the single source.</summary>
public sealed record RoleOption(string Value, string Label, bool CanWrite);
