using Tedwren.Abstractions.Contracts.Users;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The console user-management service contract (SF-20, SF-23, Q2), shared by the client and the API so the
/// UI and business logic are identical whether data is served from mock or from the database. Covers
/// listing, inviting, editing role, and suspending/reactivating accounts — access is withdrawn by
/// suspension, never by deleting the account or its audit history.
/// </summary>
public interface IUserService
{
    /// <summary>Returns every console user as a list row.</summary>
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a single user by id, or null when none matches.</summary>
    Task<UserDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the selectable access roles (single source for the role picker).</summary>
    Task<IReadOnlyList<RoleOption>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Invites a new user and returns the new account id (SF-20). The account starts as invited.</summary>
    Task<Guid> InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a user's name and role. Null when the user is not found.</summary>
    Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Suspends a user (withdraws access without deleting). Null when the user is not found.</summary>
    Task<UserDto?> SuspendUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Reactivates a suspended user. Null when the user is not found.</summary>
    Task<UserDto?> ReactivateUserAsync(Guid id, CancellationToken cancellationToken = default);
}
