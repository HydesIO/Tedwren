using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A recorded site-entry decision (R10): whether a worker was admitted, and the full set of checks — which
/// ran, what each returned, and which did not run and why — so the decision can be reconstructed months
/// later. The store is introduced here; the main-contractor gate (Phase 17) writes the decisions.
/// </summary>
public sealed class SiteEntryDecision
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The worker the decision concerns.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>The site the decision was made for.</summary>
    public required Guid SiteId { get; init; }

    /// <summary>Whether the worker was admitted.</summary>
    public required bool Admitted { get; init; }

    /// <summary>The checks that made up the decision (R10).</summary>
    public IReadOnlyList<DecisionCheck> Checks { get; init; } = Array.Empty<DecisionCheck>();

    /// <summary>When the decision was made (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset OccurredUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>One check within a site-entry decision (R10).</summary>
/// <param name="Name">The check's name (e.g. "Induction valid", "Cards in date").</param>
/// <param name="Outcome">Whether it passed, failed, or did not run.</param>
/// <param name="Detail">What it returned, or why it did not run.</param>
public sealed record DecisionCheck(string Name, DecisionCheckOutcome Outcome, string? Detail);
