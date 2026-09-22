namespace Tedwren.Application.Expiry;

/// <summary>
/// A source of expiry-bearing records for the SF-9 warning engine, the SUB-5 weekly digest and the
/// upcoming-expiries read. Each implementation projects one register (cards, company documents, inductions) into
/// source-neutral <see cref="ExpiryItem"/>s — one per (record × responsible company), with recipients resolved —
/// so the three consumers stay source-agnostic. Implementations return <b>every</b> current expiry-bearing item
/// (including already-expired ones, so the "on expiry" and "day after" stages still fire); each consumer applies
/// its own date window (the schedule stages, the 60-day digest, or the read horizon).
/// </summary>
public interface IExpirySource
{
    /// <summary>Every current expiry-bearing item this source knows about, each with its recipients resolved.</summary>
    Task<IReadOnlyList<ExpiryItem>> GetCurrentAsync(CancellationToken cancellationToken = default);
}
