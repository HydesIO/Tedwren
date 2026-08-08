namespace Tedwren.Domain.Entities;

/// <summary>
/// A definition in the qualification-type library (SF-12): the kinds of card/qualification the platform
/// tracks (CSCS card, SSSTS, First Aid at Work, …). A useful default library ships so a new customer is
/// productive quickly. Individual people hold instances of these types as <see cref="QualificationCard"/>s.
/// </summary>
public sealed class QualificationType
{
    /// <summary>Stable identifier (fixed for the default library so requirements and cards can reference it).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Display name (e.g. "CSCS Card").</summary>
    public required string Name { get; set; }

    /// <summary>Grouping category (e.g. "Card", "Health &amp; Safety", "Supervision").</summary>
    public string? Category { get; set; }

    /// <summary>Issuing body (e.g. "CITB", "HSE").</summary>
    public string? Issuer { get; set; }

    /// <summary>Typical validity in months, used as a default when capturing a card of this type.</summary>
    public int DefaultValidityMonths { get; set; }

    /// <summary>Whether this type can be verified live against CSCS (PRD-Phase 1). Informational for now.</summary>
    public bool IsCscsVerifiable { get; set; }

    /// <summary>When the type was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
