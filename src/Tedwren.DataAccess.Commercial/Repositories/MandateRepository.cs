using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IMandateRepository"/> for direct-debit mandates. ANSI-portable across engines.</summary>
public sealed class MandateRepository : AdminRepositoryBase, IMandateRepository
{
    private const string Columns =
        "Id, CompanyId, GoCardlessBillingRequestId, GoCardlessMandateId, GoCardlessCustomerId, " +
        "Reference, BankName, AccountNumberEnding, Status, CreatedUtc, UpdatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public MandateRepository(IAdminDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Deletes a mandate by id (demo-data teardown).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM Mandates WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Returns every mandate, most recent first.</summary>
    public async Task<IReadOnlyList<Mandate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM Mandates ORDER BY CreatedUtc DESC", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a mandate by local id, or null.</summary>
    public async Task<Mandate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Mandates WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the company's most recent mandate, or null. Ordered in SQL; taken in memory to stay portable.</summary>
    public async Task<Mandate?> GetCurrentForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Mandates WHERE CompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the mandate carrying the given GoCardless mandate id, or null.</summary>
    public async Task<Mandate?> GetByGoCardlessMandateIdAsync(string goCardlessMandateId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Mandates WHERE GoCardlessMandateId = @GoCardlessMandateId",
            new { GoCardlessMandateId = goCardlessMandateId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new mandate.</summary>
    public Task AddAsync(Mandate mandate, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Mandates (Id, CompanyId, GoCardlessBillingRequestId, GoCardlessMandateId, GoCardlessCustomerId, " +
            "Reference, BankName, AccountNumberEnding, Status, CreatedUtc, UpdatedUtc) " +
            "VALUES (@Id, @CompanyId, @GoCardlessBillingRequestId, @GoCardlessMandateId, @GoCardlessCustomerId, " +
            "@Reference, @BankName, @AccountNumberEnding, @Status, @CreatedUtc, @UpdatedUtc)",
            ToParameters(mandate), cancellationToken);

    /// <summary>Updates an existing mandate's mutable fields.</summary>
    public Task UpdateAsync(Mandate mandate, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Mandates SET GoCardlessBillingRequestId = @GoCardlessBillingRequestId, " +
            "GoCardlessMandateId = @GoCardlessMandateId, GoCardlessCustomerId = @GoCardlessCustomerId, " +
            "Reference = @Reference, BankName = @BankName, AccountNumberEnding = @AccountNumberEnding, " +
            "Status = @Status, UpdatedUtc = @UpdatedUtc WHERE Id = @Id",
            ToParameters(mandate), cancellationToken);

    /// <summary>Flattens a mandate to Dapper parameters (enum stored as int).</summary>
    private static object ToParameters(Mandate m) => new
    {
        m.Id,
        m.CompanyId,
        m.GoCardlessBillingRequestId,
        m.GoCardlessMandateId,
        m.GoCardlessCustomerId,
        m.Reference,
        m.BankName,
        m.AccountNumberEnding,
        Status = (int)m.Status,
        m.CreatedUtc,
        m.UpdatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static Mandate ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        GoCardlessBillingRequestId = r.GoCardlessBillingRequestId,
        GoCardlessMandateId = r.GoCardlessMandateId,
        GoCardlessCustomerId = r.GoCardlessCustomerId,
        Reference = r.Reference,
        BankName = r.BankName,
        AccountNumberEnding = r.AccountNumberEnding,
        Status = (MandateStatus)r.Status,
        CreatedUtc = r.CreatedUtc,
        UpdatedUtc = r.UpdatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, string? GoCardlessBillingRequestId, string? GoCardlessMandateId,
        string? GoCardlessCustomerId, string? Reference, string? BankName, string? AccountNumberEnding,
        int Status, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);
}
