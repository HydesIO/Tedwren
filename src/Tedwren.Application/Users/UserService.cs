using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Users;

/// <summary>
/// The single implementation of the console user-management rules (SF-20, SF-23, Q2). Data-store agnostic:
/// the same logic runs over the in-memory and Dapper repositories. Access is withdrawn by suspension, never
/// by deleting the account, so the audit trail stays intact (SF-20). The read-only auditor role (SF-23) is
/// modelled through <see cref="RolePermissions.CanWrite"/>.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;

    /// <summary>Creates the service over its repository.</summary>
    public UserService(IUserRepository users) => _users = users;

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

    /// <summary>Invites a new user (SF-20). Rejects a duplicate email so one account maps to one identity.</summary>
    public async Task<Guid> InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
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

        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"A user with email '{email}' already exists.");
        }

        var user = new User
        {
            CompanyId = InMemoryUserStore.OwnerCompanyId,
            Name = name,
            Email = email,
            Role = ParseRole(request.Role),
            Status = UserStatus.Invited,
        };
        await _users.AddAsync(user, cancellationToken);
        return user.Id;
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
