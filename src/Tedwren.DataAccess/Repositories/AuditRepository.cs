using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IAuditRepository"/> (SF-20). The search SQL is static and null-tolerant (each filter is
/// bypassed when its parameter is null), and free-text matching uses LOWER(...) LIKE for portable
/// case-insensitivity across SQL Server and PostgreSQL.
/// </summary>
public sealed class AuditRepository : RepositoryBase, IAuditRepository
{
    private const string Columns = "Id, CompanyId, OccurredUtc, Actor, Action, Entity, Reference, Category";

    /// <summary>Creates the repository over the connection factory.</summary>
    public AuditRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Searches by optional company, free text and date range, newest first.</summary>
    public async Task<IReadOnlyList<AuditEntry>> SearchAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var pattern = string.IsNullOrWhiteSpace(text) ? null : "%" + text.Trim().ToLowerInvariant() + "%";
        DateTimeOffset? fromUtc = from is { } f ? new DateTimeOffset(f.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
        DateTimeOffset? toUtc = to is { } t ? new DateTimeOffset(t.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM AuditEntries " +
            "WHERE (@CompanyId IS NULL OR CompanyId = @CompanyId) " +
            "AND (@Pattern IS NULL OR LOWER(Actor) LIKE @Pattern OR LOWER(Action) LIKE @Pattern " +
            "OR LOWER(Entity) LIKE @Pattern OR LOWER(Reference) LIKE @Pattern) " +
            "AND (@FromUtc IS NULL OR OccurredUtc >= @FromUtc) " +
            "AND (@ToUtc IS NULL OR OccurredUtc < @ToUtc) " +
            "ORDER BY OccurredUtc DESC",
            new { CompanyId = companyId, Pattern = pattern, FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Appends an audit entry.</summary>
    public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO AuditEntries (Id, CompanyId, OccurredUtc, Actor, Action, Entity, Reference, Category) " +
            "VALUES (@Id, @CompanyId, @OccurredUtc, @Actor, @Action, @Entity, @Reference, @Category)",
            new { entry.Id, entry.CompanyId, entry.OccurredUtc, entry.Actor, entry.Action, entry.Entity, entry.Reference, entry.Category },
            cancellationToken);

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static AuditEntry ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        OccurredUtc = r.OccurredUtc,
        Actor = r.Actor,
        Action = r.Action,
        Entity = r.Entity,
        Reference = r.Reference,
        Category = r.Category,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, DateTimeOffset OccurredUtc, string Actor, string Action, string Entity,
        string? Reference, string? Category);
}
