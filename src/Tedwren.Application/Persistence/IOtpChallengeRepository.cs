using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for pending one-time-code challenges (M2), keyed by mobile number (SF-1).</summary>
public interface IOtpChallengeRepository
{
    /// <summary>Returns the most recent challenge for a canonical E.164 number, or null.</summary>
    Task<OtpChallenge?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Persists a new challenge.</summary>
    Task AddAsync(OtpChallenge challenge, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing challenge (attempt count).</summary>
    Task UpdateAsync(OtpChallenge challenge, CancellationToken cancellationToken = default);

    /// <summary>Deletes any challenges for a number (clears prior codes before issuing a new one).</summary>
    Task DeleteByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Deletes a challenge by id (once consumed or exhausted).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
