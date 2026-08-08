using Tedwren.Domain.Notifications;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="INotificationLogRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryNotificationLogRepository : INotificationLogRepository
{
    private readonly InMemoryExpiryStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryNotificationLogRepository(InMemoryExpiryStore store) => _store = store;

    /// <summary>Whether a matching warning has already been logged.</summary>
    public Task<bool> ExistsAsync(Guid cardId, ExpiryWarningStage stage, NotificationChannel channel, string recipient, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Notifications.Any(n =>
            n.CardId == cardId && n.Stage == stage && n.Channel == channel && n.Recipient == recipient));

    /// <summary>Records a sent warning.</summary>
    public Task AddAsync(ExpiryNotification notification, CancellationToken cancellationToken = default)
    {
        _store.Notifications.Add(notification);
        return Task.CompletedTask;
    }
}
