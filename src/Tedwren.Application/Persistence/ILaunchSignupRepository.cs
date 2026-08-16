using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for launch-list signups (commercial database).</summary>
public interface ILaunchSignupRepository
{
    /// <summary>Persists a new signup.</summary>
    Task AddAsync(LaunchSignup signup, CancellationToken cancellationToken = default);

    /// <summary>Returns the signup for an email (case-insensitive), or null.</summary>
    Task<LaunchSignup?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Returns the signup for an unsubscribe token, or null.</summary>
    Task<LaunchSignup?> GetByUnsubscribeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Removes a signup (admin delete).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns every signup, newest first.</summary>
    Task<IReadOnlyList<LaunchSignup>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing signup (the notified flag/timestamp).</summary>
    Task UpdateAsync(LaunchSignup signup, CancellationToken cancellationToken = default);
}
