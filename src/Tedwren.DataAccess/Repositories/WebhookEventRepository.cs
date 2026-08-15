using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IWebhookEventRepository"/> for stored GoCardless webhook events. ANSI-portable.</summary>
public sealed class WebhookEventRepository : RepositoryBase, IWebhookEventRepository
{
    private const string Columns =
        "Id, GoCardlessEventId, ResourceType, Action, ResourceId, Outcome, Detail, RawJson, ReceivedUtc, ProcessedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public WebhookEventRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns the event with this GoCardless event id, or null (the dedupe check).</summary>
    public async Task<WebhookEvent?> GetByGoCardlessEventIdAsync(string goCardlessEventId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM WebhookEvents WHERE GoCardlessEventId = @GoCardlessEventId",
            new { GoCardlessEventId = goCardlessEventId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the most recent events, newest first. Ordered in SQL; capped in memory to stay portable.</summary>
    public async Task<IReadOnlyList<WebhookEvent>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM WebhookEvents ORDER BY ReceivedUtc DESC", null, cancellationToken);
        return rows.Take(limit).Select(ToEntity).ToList();
    }

    /// <summary>Inserts a new event.</summary>
    public Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO WebhookEvents (Id, GoCardlessEventId, ResourceType, Action, ResourceId, Outcome, Detail, RawJson, ReceivedUtc, ProcessedUtc) " +
            "VALUES (@Id, @GoCardlessEventId, @ResourceType, @Action, @ResourceId, @Outcome, @Detail, @RawJson, @ReceivedUtc, @ProcessedUtc)",
            ToParameters(webhookEvent), cancellationToken);

    /// <summary>Updates an event's processing outcome.</summary>
    public Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE WebhookEvents SET Outcome = @Outcome, Detail = @Detail, ProcessedUtc = @ProcessedUtc WHERE Id = @Id",
            ToParameters(webhookEvent), cancellationToken);

    /// <summary>Flattens an event to Dapper parameters (enum stored as int).</summary>
    private static object ToParameters(WebhookEvent e) => new
    {
        e.Id,
        e.GoCardlessEventId,
        e.ResourceType,
        e.Action,
        e.ResourceId,
        Outcome = (int)e.Outcome,
        e.Detail,
        e.RawJson,
        e.ReceivedUtc,
        e.ProcessedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static WebhookEvent ToEntity(Row r) => new()
    {
        Id = r.Id,
        GoCardlessEventId = r.GoCardlessEventId,
        ResourceType = r.ResourceType,
        Action = r.Action,
        ResourceId = r.ResourceId,
        Outcome = (WebhookProcessingOutcome)r.Outcome,
        Detail = r.Detail,
        RawJson = r.RawJson,
        ReceivedUtc = r.ReceivedUtc,
        ProcessedUtc = r.ProcessedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string GoCardlessEventId, string ResourceType, string Action, string? ResourceId,
        int Outcome, string? Detail, string RawJson, DateTimeOffset ReceivedUtc, DateTimeOffset? ProcessedUtc);
}
