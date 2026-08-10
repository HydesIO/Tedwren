using Tedwren.Abstractions.Notifications;

namespace Tedwren.Application.Notifications;

/// <summary>
/// Thread-safe in-memory <see cref="INotificationOutbox"/>. Registered as a singleton so the stub senders,
/// tests and the dev/mock endpoint all observe the same recorded messages.
/// </summary>
public sealed class NotificationOutbox : INotificationOutbox
{
    private readonly List<OutboxMessage> _messages = new();
    private readonly Lock _gate = new();

    /// <summary>Records a sent message.</summary>
    public void Record(OutboxMessage message)
    {
        lock (_gate)
        {
            _messages.Add(message);
        }
    }

    /// <summary>The messages recorded so far, oldest first (a snapshot copy).</summary>
    public IReadOnlyList<OutboxMessage> Messages
    {
        get
        {
            lock (_gate)
            {
                return _messages.ToList();
            }
        }
    }

    /// <summary>Clears the recorded messages.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _messages.Clear();
        }
    }
}
