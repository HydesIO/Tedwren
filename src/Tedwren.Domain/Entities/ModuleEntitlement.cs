namespace Tedwren.Domain.Entities;

/// <summary>
/// A customer's entitlement to a module (SF-22, Q2). Entitlements are the single authoritative answer to
/// "has this customer bought this", checked server-side and failing closed. Navigation shows only what the
/// customer has bought — an unpurchased module is not visible as a locked door.
/// </summary>
public sealed class ModuleEntitlement
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company the entitlement belongs to (scoped, R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The module key (e.g. "time", "compliance").</summary>
    public required string ModuleKey { get; init; }

    /// <summary>Whether the module is enabled for the company.</summary>
    public bool Enabled { get; set; }

    /// <summary>When the entitlement was last changed (UTC).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
