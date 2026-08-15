using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for stored GoCardless webhook events. Deduped by GoCardless event id so processing is
/// idempotent (a re-delivered webhook never applies twice).
/// </summary>
public interface IWebhookEventRepository
{
    /// <summary>Returns the stored event with this GoCardless event id, or null (the dedupe check).</summary>
    Task<WebhookEvent?> GetByGoCardlessEventIdAsync(string goCardlessEventId, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent events, newest first, capped at <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<WebhookEvent>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new event.</summary>
    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing event (its processing outcome).</summary>
    Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
