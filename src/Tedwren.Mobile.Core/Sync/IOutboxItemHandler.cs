namespace Tedwren.Mobile.Core.Sync;

/// <summary>
/// Handles one kind of outbox item — performing its network work when the sync engine drains it (M5). Keeping the
/// engine handler-agnostic lets M6 (forms) add its own handler without touching the drain loop. A handler throws on
/// failure; the engine classifies transient vs permanent and applies retry/backoff.
/// </summary>
public interface IOutboxItemHandler
{
    /// <summary>The <see cref="OutboxItem.Kind"/> this handler processes.</summary>
    string Kind { get; }

    /// <summary>Performs the item's work (upload the photo if needed, then submit). Throws on failure.</summary>
    Task ExecuteAsync(OutboxItem item, IOutboxStore store, CancellationToken cancellationToken = default);
}
