using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// The relationship between a <see cref="Person"/> and a <see cref="Company"/> — the company's own
/// view of that person (the name it recorded, trade, internal reference, engagement status). Adding a
/// person already known to the platform under a different company creates another engagement, not
/// another person (SF-1/SF-2). A leaver is archived, not deleted, and can be reactivated intact (SF-3).
/// </summary>
public sealed class Engagement
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company engaging the person.</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The person being engaged.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>The name as recorded by this company (never reconciled across companies; PRD §5.1).</summary>
    public required string Name { get; set; }

    /// <summary>The person's trade as recorded by this company.</summary>
    public string? Trade { get; set; }

    /// <summary>The company's own internal reference for the person, if any.</summary>
    public string? InternalReference { get; set; }

    /// <summary>Whether the engagement is active or archived (SF-3).</summary>
    public EngagementStatus Status { get; set; } = EngagementStatus.Active;

    /// <summary>When the engagement was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the engagement was archived, if it currently is (UTC).</summary>
    public DateTimeOffset? ArchivedUtc { get; set; }
}
