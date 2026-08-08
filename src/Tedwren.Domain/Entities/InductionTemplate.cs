namespace Tedwren.Domain.Entities;

/// <summary>
/// A configurable induction definition (MC-3): the ordered capture steps, the quiz (whose answers stay
/// server-side, R5), the pass mark, and how long a completion stays valid (MC-7). Held per company so each
/// customer shapes their own induction.
/// </summary>
public sealed class InductionTemplate
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that owns the template (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>A human name for the induction.</summary>
    public required string Name { get; init; }

    /// <summary>How many days a completion of this induction remains valid (MC-7).</summary>
    public required int ValidityDays { get; init; }

    /// <summary>The configurable capture steps, in order (MC-3/MC-4).</summary>
    public required IReadOnlyList<InductionStep> Steps { get; init; }

    /// <summary>The quiz questions with their server-side answers (R5).</summary>
    public required IReadOnlyList<InductionQuizQuestion> Questions { get; init; }

    /// <summary>The number of correct answers required to pass (MC-4).</summary>
    public required int PassMark { get; init; }
}
