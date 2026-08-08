using Tedwren.Domain.ValueObjects;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A person, identified across all companies by their mobile number (SF-1): one person means one
/// underlying record, however many companies engage them. The person's name is deliberately NOT held
/// here — each company records its own name for the person on the <see cref="Engagement"/>, and names
/// are never reconciled (PRD §5.1).
/// </summary>
public sealed class Person
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The normalised mobile number that identifies this person (SF-1).</summary>
    public required PhoneNumber PhoneNumber { get; init; }

    /// <summary>When the person record was first created (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
