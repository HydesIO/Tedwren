namespace Tedwren.Abstractions.Notifications;

/// <summary>
/// Sends an SMS to a worker (SF-9). A provider interface so the delivery mechanism is pluggable: a stub
/// records to an outbox now; a real provider is wired in PRD-Phase 7 without touching the callers.
/// </summary>
public interface ISmsSender
{
    /// <summary>Sends <paramref name="message"/> to <paramref name="toNumber"/>.</summary>
    Task SendAsync(string toNumber, string message, CancellationToken cancellationToken = default);
}
