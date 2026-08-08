using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Notifications;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="INotificationLogRepository"/>. SQL is ANSI-portable across both engines.</summary>
public sealed class NotificationLogRepository : RepositoryBase, INotificationLogRepository
{
    /// <summary>Creates the repository over the connection factory.</summary>
    public NotificationLogRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Whether a matching warning has already been logged (SF-9 idempotency check).</summary>
    public async Task<bool> ExistsAsync(Guid cardId, ExpiryWarningStage stage, NotificationChannel channel, string recipient, CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM ExpiryNotifications " +
            "WHERE CardId = @CardId AND Stage = @Stage AND Channel = @Channel AND Recipient = @Recipient",
            new { CardId = cardId, Stage = (int)stage, Channel = (int)channel, Recipient = recipient },
            cancellationToken);
        return count > 0;
    }

    /// <summary>Records a sent warning.</summary>
    public Task AddAsync(ExpiryNotification notification, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO ExpiryNotifications (Id, CardId, Stage, Channel, Recipient, SentUtc) " +
            "VALUES (@Id, @CardId, @Stage, @Channel, @Recipient, @SentUtc)",
            new
            {
                notification.Id,
                notification.CardId,
                Stage = (int)notification.Stage,
                Channel = (int)notification.Channel,
                notification.Recipient,
                notification.SentUtc,
            },
            cancellationToken);
}
