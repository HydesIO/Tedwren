namespace Tedwren.Web.Partners;

/// <summary>
/// Options for the partner programme (Web Plan §7.2), bound from the "Partners" section: the commission
/// rate, the clawback window, the referral cookie name and the currency. These are config so the terms
/// can change without a redeploy.
/// </summary>
public sealed class PartnerOptions
{
    /// <summary>Configuration section name this options type binds from.</summary>
    public const string SectionName = "Partners";

    /// <summary>Commission as a fraction of first-year subcontractor revenue (default 20%).</summary>
    public decimal CommissionRate { get; set; } = 0.20m;

    /// <summary>Clawback window in days after a commission is raised (default 90).</summary>
    public int ClawbackDays { get; set; } = 90;

    /// <summary>Cookie that carries a referral code across sessions for attribution.</summary>
    public string ReferralCookieName { get; set; } = "tedwren_ref";

    /// <summary>Currency commissions are denominated in.</summary>
    public string Currency { get; set; } = "GBP";
}
