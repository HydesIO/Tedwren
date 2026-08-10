using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// An operative's run through an induction (MC-1–MC-7). It tracks which steps have been completed (MC-4), the
/// quiz outcome across attempts (MC-6), the signature and the separately-captured consent (MC-5/MC-20), and —
/// on completion — a reference and validity window (MC-7). Unlike the append-only logs, a session's workflow
/// fields change as the operative progresses; a re-induction supersedes the prior session (MC-7).
/// </summary>
public sealed class InductionSession
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The template being taken.</summary>
    public required Guid TemplateId { get; init; }

    /// <summary>The company that owns the induction (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The operative.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>The operative's name as recorded by the company.</summary>
    public required string PersonName { get; init; }

    /// <summary>Where the induction is in its lifecycle.</summary>
    public InductionStatus Status { get; set; } = InductionStatus.InProgress;

    /// <summary>When the induction was started (UTC).</summary>
    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When it was completed (UTC), if passed (MC-5).</summary>
    public DateTimeOffset? CompletedUtc { get; set; }

    /// <summary>The completion reference issued on passing (MC-5).</summary>
    public string? CompletionReference { get; set; }

    /// <summary>When the completion expires (MC-7).</summary>
    public DateTimeOffset? ExpiresUtc { get; set; }

    /// <summary>How many quiz attempts have been made (MC-6).</summary>
    public int AttemptCount { get; set; }

    /// <summary>The score of the most recent quiz attempt.</summary>
    public int? LastScore { get; set; }

    /// <summary>The ids of the steps completed so far (MC-4).</summary>
    public List<string> CompletedStepIds { get; set; } = new();

    /// <summary>The signature captured on completion (MC-5).</summary>
    public string? SignatureName { get; set; }

    /// <summary>Whether the operative gave the separate, optional consent (MC-20).</summary>
    public bool ConsentGiven { get; set; }

    /// <summary>Why a manager reset the induction, if they did (MC-6).</summary>
    public string? ResetReason { get; set; }

    /// <summary>The session that superseded this one on re-induction (MC-7).</summary>
    public Guid? SupersededBySessionId { get; set; }

    /// <summary>Whether the completion is currently valid at a point in time (MC-7).</summary>
    public bool IsValid(DateTimeOffset now) =>
        Status == InductionStatus.Passed && ExpiresUtc is { } expiry && now <= expiry;
}
