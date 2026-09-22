using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for the expiry-warning idempotency log (SF-9). <see cref="ExistsAsync"/> is what
/// stops a warning being sent twice — the engine checks it before sending and records after. Keyed by source +
/// subject so cards, company documents (SUB-4) and inductions share one log without colliding.
/// </summary>
public interface INotificationLogRepository
{
    /// <summary>Whether a warning has already been sent for this source/subject/stage/channel/recipient.</summary>
    Task<bool> ExistsAsync(ExpirySource source, Guid subjectId, ExpiryWarningStage stage, NotificationChannel channel, string recipient, CancellationToken cancellationToken = default);

    /// <summary>Records that a warning was sent.</summary>
    Task AddAsync(ExpiryNotification notification, CancellationToken cancellationToken = default);
}
