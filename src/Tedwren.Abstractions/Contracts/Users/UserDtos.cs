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

/// <summary>Request to invite a new console user (SF-20). Sends an invitation to the email address.</summary>
public sealed record InviteUserRequest(string Name, string Email, string Role);

/// <summary>Request to change an existing user's name and role.</summary>
public sealed record UpdateUserRequest(string Name, string Role);

/// <summary>The available access roles, so the UI can populate its role picker from the single source.</summary>
public sealed record RoleOption(string Value, string Label, bool CanWrite);
