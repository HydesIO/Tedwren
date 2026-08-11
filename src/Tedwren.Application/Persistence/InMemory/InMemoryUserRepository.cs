using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IUserRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly InMemoryUserStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryUserRepository(InMemoryUserStore store) => _store = store;

    /// <summary>Returns all users ordered by name.</summary>
    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<User> users = _store.Users.Values.OrderBy(u => u.Name).ToList();
        return Task.FromResult(users);
    }

    /// <summary>Returns a user by id, or null.</summary>
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Users.GetValueOrDefault(id));

    /// <summary>Returns a user by email (case-insensitive), or null.</summary>
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Users.Values
            .FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Returns the user holding this invite token, or null.</summary>
    public Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Users.Values
            .FirstOrDefault(u => u.InviteToken is not null && u.InviteToken == inviteToken));

    /// <summary>Adds a user to the store.</summary>
    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _store.Users[user.Id] = user;
        return Task.CompletedTask;
    }

    /// <summary>Updates a user in the store.</summary>
    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _store.Users[user.Id] = user;
        return Task.CompletedTask;
    }
}
