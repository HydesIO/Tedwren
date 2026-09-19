using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IOtpChallengeRepository"/> (M2).</summary>
public sealed class InMemoryOtpChallengeRepository : IOtpChallengeRepository
{
    private readonly ConcurrentDictionary<Guid, OtpChallenge> _challenges = new();

    public Task<OtpChallenge?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(_challenges.Values
            .Where(c => c.PhoneNumber == phoneNumber)
            .OrderByDescending(c => c.CreatedUtc)
            .FirstOrDefault());

    public Task AddAsync(OtpChallenge challenge, CancellationToken cancellationToken = default)
    {
        _challenges[challenge.Id] = challenge;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OtpChallenge challenge, CancellationToken cancellationToken = default)
    {
        _challenges[challenge.Id] = challenge;
        return Task.CompletedTask;
    }

    public Task DeleteByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        foreach (var entry in _challenges.Where(kv => kv.Value.PhoneNumber == phoneNumber).ToList())
        {
            _challenges.TryRemove(entry.Key, out _);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _challenges.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
