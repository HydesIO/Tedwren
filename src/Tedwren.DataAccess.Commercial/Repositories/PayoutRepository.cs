using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IPayoutRepository"/> for BACS payouts. ANSI-portable across engines.</summary>
public sealed class PayoutRepository : AdminRepositoryBase, IPayoutRepository
{
    private const string Columns =
        "Id, GoCardlessPayoutId, AmountPence, Currency, Status, Reference, ArrivalDate, CreatedUtc, UpdatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public PayoutRepository(IAdminDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Deletes a payout by id (demo-data teardown).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM Payouts WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Returns every payout, most recent first.</summary>
    public async Task<IReadOnlyList<Payout>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM Payouts ORDER BY CreatedUtc DESC", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns the payout with this GoCardless payout id, or null.</summary>
    public async Task<Payout?> GetByGoCardlessPayoutIdAsync(string goCardlessPayoutId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Payouts WHERE GoCardlessPayoutId = @GoCardlessPayoutId",
            new { GoCardlessPayoutId = goCardlessPayoutId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new payout.</summary>
    public Task AddAsync(Payout payout, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Payouts (Id, GoCardlessPayoutId, AmountPence, Currency, Status, Reference, ArrivalDate, CreatedUtc, UpdatedUtc) " +
            "VALUES (@Id, @GoCardlessPayoutId, @AmountPence, @Currency, @Status, @Reference, @ArrivalDate, @CreatedUtc, @UpdatedUtc)",
            ToParameters(payout), cancellationToken);

    /// <summary>Updates an existing payout's mutable fields.</summary>
    public Task UpdateAsync(Payout payout, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Payouts SET AmountPence = @AmountPence, Currency = @Currency, Status = @Status, " +
            "Reference = @Reference, ArrivalDate = @ArrivalDate, UpdatedUtc = @UpdatedUtc WHERE Id = @Id",
            ToParameters(payout), cancellationToken);

    /// <summary>Flattens a payout to Dapper parameters (enum stored as int).</summary>
    private static object ToParameters(Payout p) => new
    {
        p.Id,
        p.GoCardlessPayoutId,
        p.AmountPence,
        p.Currency,
        Status = (int)p.Status,
        p.Reference,
        p.ArrivalDate,
        p.CreatedUtc,
        p.UpdatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static Payout ToEntity(Row r) => new()
    {
        Id = r.Id,
        GoCardlessPayoutId = r.GoCardlessPayoutId,
        AmountPence = r.AmountPence,
        Currency = r.Currency,
        Status = (PayoutStatus)r.Status,
        Reference = r.Reference,
        ArrivalDate = r.ArrivalDate,
        CreatedUtc = r.CreatedUtc,
        UpdatedUtc = r.UpdatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string GoCardlessPayoutId, int AmountPence, string Currency, int Status,
        string? Reference, DateOnly? ArrivalDate, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);
}
