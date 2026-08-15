namespace Tedwren.Abstractions.Contracts.Affiliates;

/// <summary>An affiliate as the admin console lists them. Rates are fractions (0.20 = 20%).</summary>
public sealed record AffiliateDto(
    Guid Id,
    string Name,
    string? Company,
    string ContactEmail,
    string Status,
    string? AccountManager,
    string? PayeeReference,
    decimal AffiliateRatePct,
    decimal ProfitMarginPct,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

/// <summary>
/// An account (converted lead) associated with an affiliate. <see cref="EstimatedCommission"/> is the
/// admin-entered estimate; <see cref="EarnedCommission"/> is computed from the account's actual cleared
/// direct-debit payments (net of chargebacks), so it's the real amount owed once money has moved.
/// </summary>
public sealed record AssociatedAccountDto(
    Guid LeadId,
    string CompanyName,
    string Status,
    decimal? EstimatedRevenue,
    decimal EstimatedCommission,
    decimal ClearedRevenue,
    decimal EarnedCommission);

/// <summary>An affiliate's commission position: earned (from cleared revenue), paid out, and still outstanding.</summary>
public sealed record CommissionSummaryDto(decimal Earned, decimal Paid, decimal Outstanding);

/// <summary>A commission payout to an affiliate.</summary>
public sealed record AffiliatePayoutDto(
    Guid Id,
    Guid AffiliateId,
    Guid? LeadId,
    decimal Amount,
    string Currency,
    string Status,
    string? Reference,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? PaidUtc);

/// <summary>Summary of an affiliate's agreement, for the admin detail page.</summary>
public sealed record AffiliateAgreementSummaryDto(
    Guid Id,
    string Token,
    string Status,
    DateTimeOffset? SignedUtc,
    string? SignedByName);

/// <summary>An affiliate with its associated accounts, payouts, commission position and agreement, for the detail page.</summary>
public sealed record AffiliateDetailDto(
    AffiliateDto Affiliate,
    IReadOnlyList<AssociatedAccountDto> AssociatedAccounts,
    IReadOnlyList<AffiliatePayoutDto> Payouts,
    CommissionSummaryDto Commission,
    AffiliateAgreementSummaryDto? Agreement);

/// <summary>Admin request to create an affiliate on a commission plan. Rates are fractions (0.20 = 20%).</summary>
public sealed record CreateAffiliateRequest(
    string Name,
    string ContactEmail,
    string? Company = null,
    string? AccountManager = null,
    string? PayeeReference = null,
    decimal AffiliateRatePct = 0.20m,
    decimal ProfitMarginPct = 0.33m);

/// <summary>Admin request to update an affiliate's details and commission plan.</summary>
public sealed record UpdateAffiliateRequest(
    string Name,
    string ContactEmail,
    string? Company,
    string? Status,
    string? AccountManager,
    string? PayeeReference,
    decimal AffiliateRatePct,
    decimal ProfitMarginPct);

/// <summary>Admin request to raise a payout for an affiliate.</summary>
public sealed record CreatePayoutRequest(decimal Amount, string? Currency = "GBP", Guid? LeadId = null, string? Reference = null);

/// <summary>The public, token-gated view of an affiliate agreement (what the affiliate sees before signing).</summary>
public sealed record AffiliateAgreementViewDto(
    string AffiliateName,
    string? Company,
    string TermsHtml,
    string CountersignatoryName,
    string? CountersignatoryTitle,
    string Status,
    bool Signable,
    DateTimeOffset? SignedUtc,
    string? SignedByName);

/// <summary>The affiliate's signing submission: their drawn signature (PNG data URL) and the name they confirm.</summary>
public sealed record SignAffiliateAgreementRequest(string SignatureDataUrl, string SignedByName);
