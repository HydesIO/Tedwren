using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IWebhookEventRepository"/> (test-only double), singleton so events persist per host.</summary>
public sealed class InMemoryWebhookEventRepository : IWebhookEventRepository
{
    private readonly ConcurrentDictionary<Guid, WebhookEvent> _events = new();

    /// <summary>Returns the event with this GoCardless event id, or null.</summary>
    public Task<WebhookEvent?> GetByGoCardlessEventIdAsync(string goCardlessEventId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_events.Values.FirstOrDefault(e => e.GoCardlessEventId == goCardlessEventId));

    /// <summary>Returns the most recent events, newest first.</summary>
    public Task<IReadOnlyList<WebhookEvent>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WebhookEvent> rows = _events.Values
            .OrderByDescending(e => e.ReceivedUtc)
            .Take(limit)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Adds an event.</summary>
    public Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _events[webhookEvent.Id] = webhookEvent;
        return Task.CompletedTask;
    }

    /// <summary>Updates an event.</summary>
    public Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _events[webhookEvent.Id] = webhookEvent;
        return Task.CompletedTask;
    }
}
