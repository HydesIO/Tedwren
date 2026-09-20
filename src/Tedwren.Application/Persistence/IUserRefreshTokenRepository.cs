using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence for console user refresh tokens (M8), keyed by the token's public selector (its id). Scoped by company (R15).</summary>
public interface IUserRefreshTokenRepository
{
    /// <summary>Returns a refresh token by its id (the token's public selector), or null.</summary>
    Task<UserRefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a newly-issued refresh token.</summary>
    Task AddAsync(UserRefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Persists a rotation/revocation of an existing refresh token.</summary>
    Task UpdateAsync(UserRefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Revokes every live token for a user (e.g. on password reset) by stamping <paramref name="revokedUtc"/>.</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedUtc, CancellationToken cancellationToken = default);
}
