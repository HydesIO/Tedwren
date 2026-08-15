using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// An affiliate/partner who introduces customers to Tedwren and earns commission on the accounts they bring in
/// (Web Plan §7). Commission is a share of profit, not revenue: a set profit margin is taken from the deal
/// revenue, and the affiliate's rate is applied to that profit (see <see cref="CommissionOn"/>). No raw bank
/// details are held — only a payee reference — keeping parity with the "we never handle raw bank details"
/// billing principle. Lives in the commercial plane; associated accounts are soft references (leads/accounts).
/// </summary>
public sealed class Affiliate
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The affiliate's contact name.</summary>
    public required string Name { get; set; }

    /// <summary>The affiliate's company, if any.</summary>
    public string? Company { get; set; }

    /// <summary>Contact email (setup and agreement notifications go here).</summary>
    public required string ContactEmail { get; set; }

    /// <summary>Lifecycle status.</summary>
    public AffiliateStatus Status { get; set; } = AffiliateStatus.Pending;

    /// <summary>The product owner managing this affiliate relationship.</summary>
    public string? AccountManager { get; set; }

    /// <summary>A non-sensitive payee reference for payouts (never a raw sort code / account number).</summary>
    public string? PayeeReference { get; set; }

    /// <summary>The affiliate's commission rate, as a fraction of profit (e.g. 0.20 = 20%).</summary>
    public decimal AffiliateRatePct { get; set; }

    /// <summary>The profit margin taken from deal revenue before commission (e.g. 0.33 = 33%).</summary>
    public decimal ProfitMarginPct { get; set; }

    /// <summary>When the affiliate was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the affiliate was last updated (UTC).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The commission earned on a deal of the given revenue: profit is <paramref name="dealRevenue"/> ×
    /// <see cref="ProfitMarginPct"/>, and commission is the affiliate's rate of that profit — e.g. £15,000 at a
    /// 33% margin gives £4,950 profit, and a 20% rate gives £990 commission. Rounded to whole pence.
    /// </summary>
    public decimal CommissionOn(decimal dealRevenue) =>
        decimal.Round(dealRevenue * ProfitMarginPct * AffiliateRatePct, 2, MidpointRounding.AwayFromZero);
}
