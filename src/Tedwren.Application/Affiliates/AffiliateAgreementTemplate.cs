using System.Net;
using System.Text;

namespace Tedwren.Application.Affiliates;

/// <summary>A single titled clause in the affiliate agreement.</summary>
/// <param name="Heading">The clause heading.</param>
/// <param name="Body">The clause body text.</param>
public sealed record AgreementClause(string Heading, string Body);

/// <summary>
/// Builds the affiliate-agreement content from an affiliate's commission plan (Web Plan §7). One source of the
/// clauses drives both the HTML the affiliate reads/signs and the PDF that is generated on signing, so the two
/// can never drift. Commission is expressed as profit-share: a profit margin is taken from deal revenue and the
/// affiliate's rate is applied to that profit.
/// </summary>
public static class AffiliateAgreementTemplate
{
    /// <summary>Builds the ordered clauses for an affiliate on the given commission plan.</summary>
    public static IReadOnlyList<AgreementClause> BuildClauses(string affiliateName, string? company, decimal ratePct, decimal marginPct)
    {
        var who = string.IsNullOrWhiteSpace(company) ? affiliateName : $"{affiliateName} ({company})";
        var rate = Percent(ratePct);
        var margin = Percent(marginPct);

        return new List<AgreementClause>
        {
            new("1. Parties",
                $"This affiliate agreement is between Tedwren Ltd (\"Tedwren\") and {who} (\"the Affiliate\")."),
            new("2. Appointment",
                "Tedwren appoints the Affiliate to introduce prospective customers to Tedwren's products. The Affiliate is an independent contractor, not an employee or agent, and may not bind Tedwren."),
            new("3. Commission",
                $"Commission is a share of profit, not revenue. For each introduced customer that becomes a paying account, Tedwren applies a profit margin of {margin} to the deal revenue and pays the Affiliate {rate} of that profit. For example, on £15,000 of revenue at a {margin} margin (£{Example(marginPct)} profit), the Affiliate earns £{Example(marginPct, ratePct)}."),
            new("4. Payment",
                "Commission becomes payable once the customer's payment has cleared. Payouts are made to the Affiliate's nominated payee reference. Tedwren does not hold the Affiliate's bank details."),
            new("5. Term & termination",
                "Either party may end this agreement on written notice. Commission already earned on cleared payments remains payable; introductions in progress are settled in good faith."),
            new("6. Conduct",
                "The Affiliate will represent Tedwren honestly and will not make misleading claims. The Affiliate must never offer or accept any inducement in connection with site access."),
            new("7. Confidentiality & data",
                "Each party will keep the other's confidential information private and handle any personal data in line with applicable data-protection law."),
            new("8. Acceptance",
                "By signing below, the Affiliate confirms they have read and agree to these terms."),
        };
    }

    /// <summary>Renders the clauses (plus the parties heading) as a self-contained HTML fragment for the signing page.</summary>
    public static string BuildHtml(string affiliateName, string? company, decimal ratePct, decimal marginPct)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"agreement\">");
        sb.Append("<h1>Tedwren Affiliate Agreement</h1>");
        foreach (var clause in BuildClauses(affiliateName, company, ratePct, marginPct))
        {
            sb.Append("<section><h2>").Append(Encode(clause.Heading)).Append("</h2><p>")
              .Append(Encode(clause.Body)).Append("</p></section>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>Formats a fraction as a whole-number percentage (0.20 → "20%").</summary>
    private static string Percent(decimal fraction) => $"{decimal.Round(fraction * 100, 2, MidpointRounding.AwayFromZero):0.##}%";

    /// <summary>The worked example profit on £15,000 at the given margin.</summary>
    private static string Example(decimal marginPct) => $"{decimal.Round(15000m * marginPct, 0):N0}";

    /// <summary>The worked example commission on £15,000 at the given margin and rate.</summary>
    private static string Example(decimal marginPct, decimal ratePct) => $"{decimal.Round(15000m * marginPct * ratePct, 0):N0}";

    /// <summary>HTML-encodes clause text so it is safe to inline.</summary>
    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
