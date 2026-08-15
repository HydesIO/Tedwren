using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IAffiliateRepository"/> for affiliates, payouts and agreements (commercial database).</summary>
public sealed class AffiliateRepository : AdminRepositoryBase, IAffiliateRepository
{
    private const string AffiliateColumns =
        "Id, Name, Company, ContactEmail, Status, AccountManager, PayeeReference, AffiliateRatePct, ProfitMarginPct, CreatedUtc, UpdatedUtc";
    private const string PayoutColumns =
        "Id, AffiliateId, LeadId, Amount, Currency, Status, Reference, CreatedUtc, PaidUtc";
    private const string AgreementColumns =
        "Id, AffiliateId, Token, Status, TermsHtml, CountersignatoryName, CountersignatoryTitle, SignatureDataUrl, SignedByName, SignedUtc, PdfBytes, CreatedUtc";

    /// <summary>Creates the repository over the commercial connection factory.</summary>
    public AffiliateRepository(IAdminDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    // ---- Affiliates ----

    /// <summary>Inserts a new affiliate.</summary>
    public Task AddAsync(Affiliate affiliate, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Affiliates (Id, Name, Company, ContactEmail, Status, AccountManager, PayeeReference, " +
            "AffiliateRatePct, ProfitMarginPct, CreatedUtc, UpdatedUtc) VALUES " +
            "(@Id, @Name, @Company, @ContactEmail, @Status, @AccountManager, @PayeeReference, " +
            "@AffiliateRatePct, @ProfitMarginPct, @CreatedUtc, @UpdatedUtc)",
            AffiliateParameters(affiliate), cancellationToken);

    /// <summary>Returns an affiliate by id, or null.</summary>
    public async Task<Affiliate?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<AffiliateRow>(
            $"SELECT {AffiliateColumns} FROM Affiliates WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToAffiliate(row);
    }

    /// <summary>Returns every affiliate, newest-updated first.</summary>
    public async Task<IReadOnlyList<Affiliate>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<AffiliateRow>(
            $"SELECT {AffiliateColumns} FROM Affiliates ORDER BY UpdatedUtc DESC", null, cancellationToken);
        return rows.Select(ToAffiliate).ToList();
    }

    /// <summary>Updates an affiliate's mutable fields.</summary>
    public Task UpdateAsync(Affiliate affiliate, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Affiliates SET Name = @Name, Company = @Company, ContactEmail = @ContactEmail, Status = @Status, " +
            "AccountManager = @AccountManager, PayeeReference = @PayeeReference, AffiliateRatePct = @AffiliateRatePct, " +
            "ProfitMarginPct = @ProfitMarginPct, UpdatedUtc = @UpdatedUtc WHERE Id = @Id",
            AffiliateParameters(affiliate), cancellationToken);

    // ---- Payouts ----

    /// <summary>Inserts a new payout.</summary>
    public Task AddPayoutAsync(AffiliatePayout payout, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO AffiliatePayouts (Id, AffiliateId, LeadId, Amount, Currency, Status, Reference, CreatedUtc, PaidUtc) " +
            "VALUES (@Id, @AffiliateId, @LeadId, @Amount, @Currency, @Status, @Reference, @CreatedUtc, @PaidUtc)",
            PayoutParameters(payout), cancellationToken);

    /// <summary>Returns a payout by id, or null.</summary>
    public async Task<AffiliatePayout?> GetPayoutAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<PayoutRow>(
            $"SELECT {PayoutColumns} FROM AffiliatePayouts WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToPayout(row);
    }

    /// <summary>Returns an affiliate's payouts, newest first.</summary>
    public async Task<IReadOnlyList<AffiliatePayout>> ListPayoutsAsync(Guid affiliateId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<PayoutRow>(
            $"SELECT {PayoutColumns} FROM AffiliatePayouts WHERE AffiliateId = @AffiliateId ORDER BY CreatedUtc DESC",
            new { AffiliateId = affiliateId }, cancellationToken);
        return rows.Select(ToPayout).ToList();
    }

    /// <summary>Updates a payout's mutable fields (status/paid).</summary>
    public Task UpdatePayoutAsync(AffiliatePayout payout, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE AffiliatePayouts SET Amount = @Amount, Currency = @Currency, Status = @Status, " +
            "Reference = @Reference, PaidUtc = @PaidUtc WHERE Id = @Id",
            PayoutParameters(payout), cancellationToken);

    // ---- Agreements ----

    /// <summary>Inserts a new agreement.</summary>
    public Task AddAgreementAsync(AffiliateAgreement agreement, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO AffiliateAgreements (Id, AffiliateId, Token, Status, TermsHtml, CountersignatoryName, " +
            "CountersignatoryTitle, SignatureDataUrl, SignedByName, SignedUtc, PdfBytes, CreatedUtc) VALUES " +
            "(@Id, @AffiliateId, @Token, @Status, @TermsHtml, @CountersignatoryName, " +
            "@CountersignatoryTitle, @SignatureDataUrl, @SignedByName, @SignedUtc, @PdfBytes, @CreatedUtc)",
            AgreementParameters(agreement), cancellationToken);

    /// <summary>Returns an agreement by its public token, or null.</summary>
    public async Task<AffiliateAgreement?> GetAgreementByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<AgreementRow>(
            $"SELECT {AgreementColumns} FROM AffiliateAgreements WHERE Token = @Token", new { Token = token }, cancellationToken);
        return row is null ? null : ToAgreement(row);
    }

    /// <summary>Returns the most recent agreement for an affiliate, or null.</summary>
    public async Task<AffiliateAgreement?> GetAgreementByAffiliateAsync(Guid affiliateId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<AgreementRow>(
            $"SELECT {AgreementColumns} FROM AffiliateAgreements WHERE AffiliateId = @AffiliateId ORDER BY CreatedUtc DESC",
            new { AffiliateId = affiliateId }, cancellationToken);
        return row is null ? null : ToAgreement(row);
    }

    /// <summary>Updates an agreement's mutable fields (signature, status, PDF).</summary>
    public Task UpdateAgreementAsync(AffiliateAgreement agreement, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE AffiliateAgreements SET Status = @Status, SignatureDataUrl = @SignatureDataUrl, " +
            "SignedByName = @SignedByName, SignedUtc = @SignedUtc, PdfBytes = @PdfBytes WHERE Id = @Id",
            AgreementParameters(agreement), cancellationToken);

    // ---- Mapping ----

    private static object AffiliateParameters(Affiliate a) => new
    {
        a.Id, a.Name, a.Company, a.ContactEmail, Status = (int)a.Status, a.AccountManager, a.PayeeReference,
        a.AffiliateRatePct, a.ProfitMarginPct, a.CreatedUtc, a.UpdatedUtc,
    };

    private static Affiliate ToAffiliate(AffiliateRow r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Company = r.Company,
        ContactEmail = r.ContactEmail,
        Status = (AffiliateStatus)r.Status,
        AccountManager = r.AccountManager,
        PayeeReference = r.PayeeReference,
        AffiliateRatePct = r.AffiliateRatePct,
        ProfitMarginPct = r.ProfitMarginPct,
        CreatedUtc = r.CreatedUtc,
        UpdatedUtc = r.UpdatedUtc,
    };

    private static object PayoutParameters(AffiliatePayout p) => new
    {
        p.Id, p.AffiliateId, p.LeadId, p.Amount, p.Currency, Status = (int)p.Status, p.Reference, p.CreatedUtc, p.PaidUtc,
    };

    private static AffiliatePayout ToPayout(PayoutRow r) => new()
    {
        Id = r.Id,
        AffiliateId = r.AffiliateId,
        LeadId = r.LeadId,
        Amount = r.Amount,
        Currency = r.Currency,
        Status = (AffiliatePayoutStatus)r.Status,
        Reference = r.Reference,
        CreatedUtc = r.CreatedUtc,
        PaidUtc = r.PaidUtc,
    };

    private static object AgreementParameters(AffiliateAgreement a) => new
    {
        a.Id, a.AffiliateId, a.Token, Status = (int)a.Status, a.TermsHtml, a.CountersignatoryName,
        a.CountersignatoryTitle, a.SignatureDataUrl, a.SignedByName, a.SignedUtc, a.PdfBytes, a.CreatedUtc,
    };

    private static AffiliateAgreement ToAgreement(AgreementRow r) => new()
    {
        Id = r.Id,
        AffiliateId = r.AffiliateId,
        Token = r.Token,
        Status = (AffiliateAgreementStatus)r.Status,
        TermsHtml = r.TermsHtml,
        CountersignatoryName = r.CountersignatoryName,
        CountersignatoryTitle = r.CountersignatoryTitle,
        SignatureDataUrl = r.SignatureDataUrl,
        SignedByName = r.SignedByName,
        SignedUtc = r.SignedUtc,
        PdfBytes = r.PdfBytes,
        CreatedUtc = r.CreatedUtc,
    };

    private sealed record AffiliateRow(
        Guid Id, string Name, string? Company, string ContactEmail, int Status, string? AccountManager,
        string? PayeeReference, decimal AffiliateRatePct, decimal ProfitMarginPct, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);

    private sealed record PayoutRow(
        Guid Id, Guid AffiliateId, Guid? LeadId, decimal Amount, string Currency, int Status, string? Reference,
        DateTimeOffset CreatedUtc, DateTimeOffset? PaidUtc);

    private sealed record AgreementRow(
        Guid Id, Guid AffiliateId, string Token, int Status, string TermsHtml, string CountersignatoryName,
        string? CountersignatoryTitle, string? SignatureDataUrl, string? SignedByName, DateTimeOffset? SignedUtc,
        byte[]? PdfBytes, DateTimeOffset CreatedUtc);
}
