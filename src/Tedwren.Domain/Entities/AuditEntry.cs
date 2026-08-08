namespace Tedwren.Domain.Entities;

/// <summary>
/// One entry in the audit trail (SF-20). Audit search covers name, reference and date range, and exports.
/// Entries are scoped to the customer that owns them (R15).
/// </summary>
public sealed class AuditEntry
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company the entry belongs to (scoped, R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>When the action occurred (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset OccurredUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Who performed the action (a person's name, or "System").</summary>
    public required string Actor { get; init; }

    /// <summary>What was done.</summary>
    public required string Action { get; init; }

    /// <summary>The entity acted upon (a name).</summary>
    public required string Entity { get; init; }

    /// <summary>An optional reference searched alongside the name (SF-20).</summary>
    public string? Reference { get; init; }

    /// <summary>The category/area of the action.</summary>
    public string? Category { get; init; }
}
