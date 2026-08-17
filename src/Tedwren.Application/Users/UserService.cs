using System.Security.Cryptography;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Auth;
using Tedwren.Application.Notifications.Email;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Users;

/// <summary>
/// The single implementation of the console user-management rules (SF-20, SF-23, Q2). Data-store agnostic:
/// the same logic runs over the in-memory and Dapper repositories. Access is withdrawn by suspension, never
/// by deleting the account, so the audit trail stays intact (SF-20). The read-only auditor role (SF-23) is
/// modelled through <see cref="RolePermissions.CanWrite"/>. Inviting a user also emails the accept-invite
/// link (best-effort — a delivery failure never loses the invite).
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOptions;

    /// <summary>Creates the service over its repository, the email sender and the email/branding options.</summary>
    public UserService(IUserRepository users, IEmailSender email, EmailOptions emailOptions)
    {
        _users = users;
        _email = email;
        _emailOptions = emailOptions;
    }

    /// <summary>Returns every console user as a list row.</summary>
    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        return users.Select(ToDto).ToList();
    }

    /// <summary>Returns a single user by id, or null.</summary>
    public async Task<UserDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    /// <summary>Returns the selectable access roles, in a stable order, from the single source (the enum).</summary>
    public Task<IReadOnlyList<RoleOption>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoleOption> roles = Enum.GetValues<AccessRole>()
            .Select(r => new RoleOption(r.ToString(), RoleLabel(r), RolePermissions.CanWrite(r)))
            .ToList();
        return Task.FromResult(roles);
    }

    /// <summary>How long an invitation's accept token stays valid.</summary>
    private static readonly TimeSpan InviteTokenLifetime = TimeSpan.FromDays(14);

    /// <summary>Invites a new user (SF-20). Rejects a duplicate email so one account maps to one identity. Mints a one-time accept token.</summary>
    public async Task<InviteUserResult> InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("A user name is required.", nameof(request));
        }

        if (!IsValidEmail(email))
        {
            throw new ArgumentException("A valid email address is required.", nameof(request));
        }

        if (request.CompanyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required to invite a user.", nameof(request));
        }

        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"A user with email '{email}' already exists.");
        }

        var acceptToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var user = new User
        {
            CompanyId = request.CompanyId,
            Name = name,
            Email = email,
            Role = ParseRole(request.Role),
            Status = UserStatus.Invited,
            InviteToken = acceptToken,
            InviteTokenExpiresUtc = DateTimeOffset.UtcNow.Add(InviteTokenLifetime),
        };
        await _users.AddAsync(user, cancellationToken);

        var emailSent = await SendInviteEmailAsync(user, acceptToken, cancellationToken);
        return new InviteUserResult(user.Id, acceptToken, emailSent);
    }

    /// <summary>
    /// Emails the branded accept-invite link (best-effort). Returns true only when a real provider delivered
    /// it: the outbox stub and any delivery failure return false, so the caller falls back to sharing the link.
    /// </summary>
    private async Task<bool> SendInviteEmailAsync(User user, string acceptToken, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = (_emailOptions.ConsoleBaseUrl ?? string.Empty).TrimEnd('/');
            var acceptUrl = $"{baseUrl}/accept-invite?token={acceptToken}";
            var content = InviteEmail.BuildContent(user.Name, acceptUrl, user.InviteTokenExpiresUtc);
            await _email.SendHtmlAsync(user.Email, InviteEmail.Subject, content, cancellationToken);
            return _emailOptions.Provider == EmailProvider.Resend;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best-effort: the user is already created; the accept token is returned so the admin can share
            // the link manually rather than losing the invite to a transient mail failure.
            return false;
        }
    }

    /// <summary>Updates a user's name and role. Null when the user is not found.</summary>
    public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("A user name is required.", nameof(request));
        }

        user.Name = name;
        user.Role = ParseRole(request.Role);
        await _users.UpdateAsync(user, cancellationToken);
        return ToDto(user);
    }

    /// <summary>Minimum admin-set password length.</summary>
    private const int MinPasswordLength = 8;

    /// <summary>
    /// Sets (resets) a user's password (SF-20). Hashes with the shared salted PBKDF2 hasher. An invited
    /// account is activated and its invite token consumed, since a password now makes it usable. Null when
    /// the user is not found.
    /// </summary>
    public async Task<UserDto?> SetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MinPasswordLength)
        {
            throw new ArgumentException($"A password of at least {MinPasswordLength} characters is required.", nameof(newPassword));
        }

        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        user.PasswordSetUtc = DateTimeOffset.UtcNow;

        if (user.Status == UserStatus.Invited)
        {
            user.Status = UserStatus.Active;
            user.InviteToken = null;
            user.InviteTokenExpiresUtc = null;
        }

        await _users.UpdateAsync(user, cancellationToken);
        return ToDto(user);
    }

    /// <summary>Suspends a user, withdrawing access without deleting the account. Null when not found.</summary>
    public Task<UserDto?> SuspendUserAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, UserStatus.Suspended, cancellationToken);

    /// <summary>Reactivates a suspended user. Null when not found.</summary>
    public Task<UserDto?> ReactivateUserAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, UserStatus.Active, cancellationToken);

    /// <summary>Sets a user's status and persists it. Null when the user is not found.</summary>
    private async Task<UserDto?> SetStatusAsync(Guid id, UserStatus status, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Status = status;
        await _users.UpdateAsync(user, cancellationToken);
        return ToDto(user);
    }

    /// <summary>Maps a domain user to its DTO, including friendly role/status labels and the write permission.</summary>
    private static UserDto ToDto(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role.ToString(),
        RoleLabel(user.Role),
        user.Status.ToString(),
        StatusLabel(user.Status),
        RolePermissions.CanWrite(user.Role),
        user.CreatedUtc,
        user.LastActiveUtc);

    /// <summary>Parses a role value (enum name), defaulting to the safest read-only role for unknown input.</summary>
    private static AccessRole ParseRole(string? role) =>
        Enum.TryParse<AccessRole>(role, ignoreCase: true, out var parsed) ? parsed : AccessRole.Auditor;

    /// <summary>The display label for a role.</summary>
    private static string RoleLabel(AccessRole role) => role switch
    {
        AccessRole.Administrator => "Administrator",
        AccessRole.ComplianceManager => "Compliance Manager",
        AccessRole.SiteManager => "Site Manager",
        AccessRole.Auditor => "Auditor (read-only)",
        _ => role.ToString(),
    };

    /// <summary>The display label for an account status.</summary>
    private static string StatusLabel(UserStatus status) => status switch
    {
        UserStatus.Invited => "Invited",
        UserStatus.Active => "Active",
        UserStatus.Suspended => "Suspended",
        _ => status.ToString(),
    };

    /// <summary>Minimal email sanity check, matching the client-side validation.</summary>
    private static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@') && value.Contains('.');
}
