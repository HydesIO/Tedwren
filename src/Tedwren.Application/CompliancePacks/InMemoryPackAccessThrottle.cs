using System.Collections.Concurrent;

namespace Tedwren.Application.CompliancePacks;

/// <summary>
/// In-memory <see cref="IPackAccessThrottle"/> (registered as a singleton so state is shared across requests).
/// After <see cref="MaxFailures"/> failed passcode attempts for a token, that token is locked out for
/// <see cref="LockoutMinutes"/>. A single host is sufficient today; a distributed cache would back this in a
/// multi-instance deployment.
/// </summary>
public sealed class InMemoryPackAccessThrottle : IPackAccessThrottle
{
    /// <summary>Failed attempts before lockout.</summary>
    public const int MaxFailures = 5;

    /// <summary>How long a locked-out token stays locked.</summary>
    public const int LockoutMinutes = 15;

    private readonly ConcurrentDictionary<string, State> _byToken = new(StringComparer.Ordinal);
    private readonly Func<DateTimeOffset> _now;

    /// <summary>Creates the throttle using the system clock.</summary>
    public InMemoryPackAccessThrottle() : this(() => DateTimeOffset.UtcNow)
    {
    }

    /// <summary>Creates the throttle with an injectable clock (for tests).</summary>
    public InMemoryPackAccessThrottle(Func<DateTimeOffset> now) => _now = now;

    /// <summary>Whether the token is currently locked out.</summary>
    public bool IsLockedOut(string token) =>
        _byToken.TryGetValue(token, out var state) && state.LockedUntil is { } until && until > _now();

    /// <summary>Records a failure; locks the token once the failure threshold is reached.</summary>
    public void RegisterFailure(string token)
    {
        _byToken.AddOrUpdate(
            token,
            _ => new State(1, null),
            (_, existing) =>
            {
                var failures = existing.Failures + 1;
                var lockedUntil = failures >= MaxFailures ? _now().AddMinutes(LockoutMinutes) : existing.LockedUntil;
                return new State(failures, lockedUntil);
            });
    }

    /// <summary>Clears the token's failure state after a success.</summary>
    public void Reset(string token) => _byToken.TryRemove(token, out _);

    /// <summary>Per-token failure count and optional lockout expiry.</summary>
    private sealed record State(int Failures, DateTimeOffset? LockedUntil);
}
