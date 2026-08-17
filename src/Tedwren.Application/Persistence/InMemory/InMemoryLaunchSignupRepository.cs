using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="ILaunchSignupRepository"/>.</summary>
public sealed class InMemoryLaunchSignupRepository : ILaunchSignupRepository
{
    private readonly ConcurrentDictionary<Guid, LaunchSignup> _signups = new();

    /// <summary>Persists a new signup.</summary>
    public Task AddAsync(LaunchSignup signup, CancellationToken cancellationToken = default)
    {
        _signups[signup.Id] = signup;
        return Task.CompletedTask;
    }

    /// <summary>Returns the signup for an email (case-insensitive), or null.</summary>
    public Task<LaunchSignup?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_signups.Values.FirstOrDefault(s => string.Equals(s.Email, email, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Returns the signup for an unsubscribe token, or null.</summary>
    public Task<LaunchSignup?> GetByUnsubscribeTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(_signups.Values.FirstOrDefault(s => s.UnsubscribeToken == token));

    /// <summary>Removes a signup.</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _signups.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>Returns every signup, newest first.</summary>
    public Task<IReadOnlyList<LaunchSignup>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LaunchSignup>>(_signups.Values.OrderByDescending(s => s.CreatedUtc).ToList());

    /// <summary>Persists changes to an existing signup.</summary>
    public Task UpdateAsync(LaunchSignup signup, CancellationToken cancellationToken = default)
    {
        _signups[signup.Id] = signup;
        return Task.CompletedTask;
    }
}
