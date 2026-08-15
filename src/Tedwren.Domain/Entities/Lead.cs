using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A sales lead — a prospective customer the product team is tracking towards becoming an account (Web Plan
/// §7). Not a tenant/compliance record: it lives in the commercial plane and holds only what's needed to work
/// the pipeline. A lead converts into an account by a manual decision (or by matching the company number
/// captured at sign-up); the account and any affiliate are held as soft references (no cross-database FK).
/// </summary>
public sealed class Lead
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The prospective company's name.</summary>
    public required string CompanyName { get; set; }

    /// <summary>A contact person at the company, if known.</summary>
    public string? ContactName { get; set; }

    /// <summary>Contact email, if known.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Free-text location — a town, area or country (e.g. "Oxford", "Dubai"), just enough to place the contact.</summary>
    public string? Location { get; set; }

    /// <summary>Which product model the lead is for (drives the revenue basis).</summary>
    public LeadModel Model { get; set; } = LeadModel.Unknown;

    /// <summary>Number of sites (main contractor) — a dispersed scheme counts as one site (SF-25/26).</summary>
    public int? NumberOfSites { get; set; }

    /// <summary>Estimated potential annual revenue (admin-entered; pricing is configuration, not hard-coded — PRD §9).</summary>
    public decimal? EstimatedRevenue { get; set; }

    /// <summary>The pipeline stage.</summary>
    public LeadStatus Status { get; set; } = LeadStatus.New;

    /// <summary>The product owner acting as account manager for this lead.</summary>
    public string? AccountManager { get; set; }

    /// <summary>Company registration number, captured to match a lead to a converted account.</summary>
    public string? CompanyNumber { get; set; }

    /// <summary>The affiliate this lead is attributed to, if any (soft reference into the affiliates slice).</summary>
    public Guid? AffiliateId { get; set; }

    /// <summary>The account this lead converted into, if it has (soft reference into the product database).</summary>
    public Guid? ConvertedAccountId { get; set; }

    /// <summary>Where the lead came from (e.g. "demo", "contact", "manual").</summary>
    public string? Source { get; set; }

    /// <summary>When the lead was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the lead was last updated (UTC).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
