using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IPaymentRepository"/> for direct-debit payments. ANSI-portable across engines.</summary>
public sealed class PaymentRepository : RepositoryBase, IPaymentRepository
{
    private const string Columns =
        "Id, CompanyId, MandateId, GoCardlessPaymentId, AmountPence, Currency, Description, " +
        "Status, ChargeDate, FailureReason, CreatedUtc, UpdatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public PaymentRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns every payment, most recent first.</summary>
    public async Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM Payments ORDER BY CreatedUtc DESC", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a payment by local id, or null.</summary>
    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Payments WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns a company's payments, most recent first.</summary>
    public async Task<IReadOnlyList<Payment>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Payments WHERE CompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns the payment carrying the given GoCardless payment id, or null.</summary>
    public async Task<Payment?> GetByGoCardlessPaymentIdAsync(string goCardlessPaymentId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Payments WHERE GoCardlessPaymentId = @GoCardlessPaymentId",
            new { GoCardlessPaymentId = goCardlessPaymentId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new payment.</summary>
    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Payments (Id, CompanyId, MandateId, GoCardlessPaymentId, AmountPence, Currency, Description, " +
            "Status, ChargeDate, FailureReason, CreatedUtc, UpdatedUtc) " +
            "VALUES (@Id, @CompanyId, @MandateId, @GoCardlessPaymentId, @AmountPence, @Currency, @Description, " +
            "@Status, @ChargeDate, @FailureReason, @CreatedUtc, @UpdatedUtc)",
            ToParameters(payment), cancellationToken);

    /// <summary>Updates an existing payment's mutable fields.</summary>
    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Payments SET GoCardlessPaymentId = @GoCardlessPaymentId, Description = @Description, " +
            "Status = @Status, ChargeDate = @ChargeDate, FailureReason = @FailureReason, UpdatedUtc = @UpdatedUtc " +
            "WHERE Id = @Id",
            ToParameters(payment), cancellationToken);

    /// <summary>Flattens a payment to Dapper parameters (enum stored as int).</summary>
    private static object ToParameters(Payment p) => new
    {
        p.Id,
        p.CompanyId,
        p.MandateId,
        p.GoCardlessPaymentId,
        p.AmountPence,
        p.Currency,
        p.Description,
        Status = (int)p.Status,
        p.ChargeDate,
        p.FailureReason,
        p.CreatedUtc,
        p.UpdatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static Payment ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        MandateId = r.MandateId,
        GoCardlessPaymentId = r.GoCardlessPaymentId,
        AmountPence = r.AmountPence,
        Currency = r.Currency,
        Description = r.Description,
        Status = (PaymentStatus)r.Status,
        ChargeDate = r.ChargeDate,
        FailureReason = r.FailureReason,
        CreatedUtc = r.CreatedUtc,
        UpdatedUtc = r.UpdatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, Guid MandateId, string? GoCardlessPaymentId, int AmountPence, string Currency,
        string? Description, int Status, DateOnly? ChargeDate, string? FailureReason,
        DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);
}
