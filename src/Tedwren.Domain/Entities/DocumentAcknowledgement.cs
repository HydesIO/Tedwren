namespace Tedwren.Domain.Entities;

/// <summary>
/// One recipient's line in a <see cref="DocumentDistribution"/>'s completion matrix (PRD §8.2): who the document
/// went to and whether they have acknowledged (signed) receipt. Scoped to the distributing company (R15); an
/// acknowledgement is recorded, not deleted (append-style).
/// </summary>
public sealed class DocumentAcknowledgement
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The distribution this line belongs to.</summary>
    public required Guid DistributionId { get; init; }

    /// <summary>The distributing company (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The recipient's name.</summary>
    public required string RecipientName { get; set; }

    /// <summary>The recipient's person id, when known.</summary>
    public Guid? PersonId { get; init; }

    /// <summary>When the recipient acknowledged/signed for it, or null while pending.</summary>
    public DateTimeOffset? AcknowledgedUtc { get; set; }
}
