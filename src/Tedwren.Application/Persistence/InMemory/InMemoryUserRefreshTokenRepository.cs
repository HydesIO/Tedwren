using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IUserRefreshTokenRepository"/> (M8).</summary>
public sealed class InMemoryUserRefreshTokenRepository : IUserRefreshTokenRepository
{
    private readonly ConcurrentDictionary<Guid, UserRefreshToken> _tokens = new();

    public Task<UserRefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tokens.GetValueOrDefault(id));

    public Task AddAsync(UserRefreshToken token, CancellationToken cancellationToken = default)
    {
        _tokens[token.Id] = token;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserRefreshToken token, CancellationToken cancellationToken = default)
    {
        _tokens[token.Id] = token;
        return Task.CompletedTask;
    }

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedUtc, CancellationToken cancellationToken = default)
    {
        foreach (var token in _tokens.Values.Where(t => t.UserId == userId && t.RevokedUtc is null))
        {
            token.RevokedUtc = revokedUtc;
        }

        return Task.CompletedTask;
    }
}
