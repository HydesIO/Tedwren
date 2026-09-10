namespace Tedwren.DataAccess.Commercial.Ef;

// These flat entity classes exist ONLY to describe the COMMERCIAL database schema to EF Core so it can author
// and apply migrations (DDL). They mirror the hand-written scripts under
// Migrations/Scripts/{SqlServer,Postgres}/Commercial/ (022–032) 1:1 — same names, columns and types — and are
// never used for runtime reads/writes, which stay on the Dapper commercial repositories (DML). See
// docs/ef-migrations.md §8.

/// <summary>Schema row for the <c>Mandates</c> table (direct-debit mandates; R15 — scoped by CompanyId).</summary>
public sealed class MandateRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? GoCardlessBillingRequestId { get; set; }
    public string? GoCardlessMandateId { get; set; }
    public string? GoCardlessCustomerId { get; set; }
    public string? Reference { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumberEnding { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>Payments</c> table (money in minor units; R15 — scoped by CompanyId).</summary>
public sealed class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid MandateId { get; set; }
    public string? GoCardlessPaymentId { get; set; }
    public int AmountPence { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public DateOnly? ChargeDate { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>BillingSubscriptions</c> table (one per company).</summary>
public sealed class BillingSubscriptionRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? MandateId { get; set; }
    public string MeterKey { get; set; } = string.Empty;
    public string? BandKey { get; set; }
    public string? Description { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>WebhookEvents</c> table (inbound GoCardless events, deduped by event id).</summary>
public sealed class WebhookEventRecord
{
    public Guid Id { get; set; }
    public string GoCardlessEventId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public int Outcome { get; set; }
    public string? Detail { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public DateTimeOffset ReceivedUtc { get; set; }
    public DateTimeOffset? ProcessedUtc { get; set; }
}

/// <summary>Schema row for the <c>Payouts</c> table (Tedwren's own BACS settlement; not tenant-scoped).</summary>
public sealed class PayoutRecord
{
    public Guid Id { get; set; }
    public string GoCardlessPayoutId { get; set; } = string.Empty;
    public int AmountPence { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? Reference { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>LaunchSignups</c> table (pre-launch interest; deduped on lower-cased email).</summary>
public sealed class LaunchSignupRecord
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public bool Notified { get; set; }
    public DateTimeOffset? NotifiedUtc { get; set; }
    /// <summary>Persisted computed column <c>LOWER(Email)</c> — the case-insensitive dedupe key.</summary>
    public string EmailLower { get; set; } = string.Empty;
    public bool Unsubscribed { get; set; }
    public string? UnsubscribeToken { get; set; }
}

/// <summary>Schema row for the <c>Leads</c> table (sales pipeline; soft Guid links, no cross-DB FK).</summary>
public sealed class LeadRecord
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? Location { get; set; }
    public int Model { get; set; }
    public int? NumberOfSites { get; set; }
    public decimal? EstimatedRevenue { get; set; }
    public int Status { get; set; }
    public string? AccountManager { get; set; }
    public string? CompanyNumber { get; set; }
    public Guid? AffiliateId { get; set; }
    public Guid? ConvertedAccountId { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>LeadNotes</c> table (append-only activity log per lead).</summary>
public sealed class LeadNoteRecord
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string? Author { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? StatusChange { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Schema row for the <c>Affiliates</c> table (introducers; rates as fractions).</summary>
public sealed class AffiliateRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? AccountManager { get; set; }
    public string? PayeeReference { get; set; }
    public decimal AffiliateRatePct { get; set; }
    public decimal ProfitMarginPct { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

/// <summary>Schema row for the <c>AffiliatePayouts</c> table (commission owed/paid; no bank details).</summary>
public sealed class AffiliatePayoutRecord
{
    public Guid Id { get; set; }
    public Guid AffiliateId { get; set; }
    public Guid? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
    public int Status { get; set; }
    public string? Reference { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? PaidUtc { get; set; }
}

/// <summary>Schema row for the <c>AffiliateAgreements</c> table (token-gated terms + countersigned PDF; R9).</summary>
public sealed class AffiliateAgreementRecord
{
    public Guid Id { get; set; }
    public Guid AffiliateId { get; set; }
    public string Token { get; set; } = string.Empty;
    public int Status { get; set; }
    public string TermsHtml { get; set; } = string.Empty;
    public string CountersignatoryName { get; set; } = string.Empty;
    public string? CountersignatoryTitle { get; set; }
    public string? SignatureDataUrl { get; set; }
    public string? SignedByName { get; set; }
    public DateTimeOffset? SignedUtc { get; set; }
    public byte[]? PdfBytes { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ExpiresUtc { get; set; }
}
