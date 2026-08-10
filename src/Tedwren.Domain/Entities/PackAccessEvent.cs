using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A recorded recipient interaction with a compliance pack — an open or a download (SUB-20). Access events are
/// append-only, giving the sender an audit trail of who engaged with what they sent.
/// </summary>
public sealed class PackAccessEvent
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The pack that was accessed.</summary>
    public required Guid PackId { get; init; }

    /// <summary>The kind of interaction.</summary>
    public required PackAccessKind Kind { get; init; }

    /// <summary>When the interaction occurred (UTC).</summary>
    public DateTimeOffset OccurredUtc { get; init; } = DateTimeOffset.UtcNow;
}
