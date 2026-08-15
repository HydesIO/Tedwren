using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IBillingSubscriptionRepository"/> for company billing subscriptions. ANSI-portable.</summary>
public sealed class BillingSubscriptionRepository : RepositoryBase, IBillingSubscriptionRepository
{
    private const string Columns =
        "Id, CompanyId, MandateId, MeterKey, BandKey, Description, Status, CreatedUtc, UpdatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public BillingSubscriptionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns every subscription, most recent first.</summary>
    public async Task<IReadOnlyList<BillingSubscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>($"SELECT {Columns} FROM BillingSubscriptions ORDER BY CreatedUtc DESC", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a company's subscription, or null.</summary>
    public async Task<BillingSubscription?> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM BillingSubscriptions WHERE CompanyId = @CompanyId",
            new { CompanyId = companyId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new subscription.</summary>
    public Task AddAsync(BillingSubscription subscription, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO BillingSubscriptions (Id, CompanyId, MandateId, MeterKey, BandKey, Description, Status, CreatedUtc, UpdatedUtc) " +
            "VALUES (@Id, @CompanyId, @MandateId, @MeterKey, @BandKey, @Description, @Status, @CreatedUtc, @UpdatedUtc)",
            ToParameters(subscription), cancellationToken);

    /// <summary>Updates an existing subscription's mutable fields.</summary>
    public Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE BillingSubscriptions SET MandateId = @MandateId, MeterKey = @MeterKey, BandKey = @BandKey, " +
            "Description = @Description, Status = @Status, UpdatedUtc = @UpdatedUtc WHERE Id = @Id",
            ToParameters(subscription), cancellationToken);

    /// <summary>Flattens a subscription to Dapper parameters (enum stored as int).</summary>
    private static object ToParameters(BillingSubscription s) => new
    {
        s.Id,
        s.CompanyId,
        s.MandateId,
        s.MeterKey,
        s.BandKey,
        s.Description,
        Status = (int)s.Status,
        s.CreatedUtc,
        s.UpdatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static BillingSubscription ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        MandateId = r.MandateId,
        MeterKey = r.MeterKey,
        BandKey = r.BandKey,
        Description = r.Description,
        Status = (SubscriptionStatus)r.Status,
        CreatedUtc = r.CreatedUtc,
        UpdatedUtc = r.UpdatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, Guid? MandateId, string MeterKey, string? BandKey, string? Description,
        int Status, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);
}
