using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for console <see cref="User"/> accounts (SF-20/SF-23).</summary>
public interface IUserRepository
{
    /// <summary>Returns all users, ordered by name.</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a user by id, or null.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a user by email (case-insensitive), or null. Used to prevent duplicate invitations.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Returns the user holding this (unexpired) invite token, or null (invite acceptance).</summary>
    Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);

    /// <summary>Persists a new user.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing user (role, name, status).</summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes a user by id (used only by the demo-data teardown).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
