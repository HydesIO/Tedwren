using Tedwren.Web.Qa;
using Tedwren.Web.Tests.Support;

namespace Tedwren.Web.Tests;

/// <summary>
/// W8 pre-launch content gate (Web Plan §8, §14). Two responsibilities: prove the shipped site is clean
/// (the enforcement gate crawlers and reviewers rely on), and prove each lint rule actually fires on a
/// seeded breach so the gate is not silently green because it never matches anything.
/// </summary>
public sealed class ContentLintTests
{
    /// <summary>The gate: the real content JSON and Razor views carry no lint breach.</summary>
    [Fact]
    public void ShippedContentAndViews_HaveNoLintViolations()
    {
        var violations = ContentLint.ScanWebProject(RepoPaths.WebProject);

        Assert.True(
            violations.Count == 0,
            "Content lint found breaches:\n" +
            string.Join("\n", violations.Select(v => $"  [{v.Rule}] {v.File}:{v.Line} — {v.Snippet}")));
    }

    /// <summary>A price symbol typed into copy is caught — prices must come from the pricing source.</summary>
    [Theory]
    [InlineData("\"Tagline\": \"From £10 a month\"")]
    [InlineData("<p>Just $99 a year</p>")]
    public void HardcodedPriceSymbol_IsCaught(string line)
    {
        var hits = ContentLint.ScanLines("seed.json", new[] { line });

        Assert.Contains(hits, v => v.Rule == "hardcoded-price");
    }

    /// <summary>Absolute-compliance claims are caught (Plan §8.1).</summary>
    [Theory]
    [InlineData("We guarantee compliance on every site.")]
    [InlineData("This ensures you stay compliant.")]
    [InlineData("Stay 100% compliant with Tedwren.")]
    [InlineData("Your sites are fully compliant, always.")]
    public void AbsoluteComplianceClaim_IsCaught(string line)
    {
        var hits = ContentLint.ScanLines("seed.cshtml", new[] { line });

        Assert.Contains(hits, v => v.Rule == "absolute-compliance");
    }

    /// <summary>CSCS rivalry / replacement / on-demand-verification phrasing is caught (Plan §8.2).</summary>
    [Theory]
    [InlineData("A replacement for CSCS you can trust.")]
    [InlineData("Use it instead of CSCS.")]
    [InlineData("The smarter alternative to CSCS.")]
    [InlineData("On-demand verification of any worker's cards.")]
    [InlineData("A rival to the CSCS Digital Skills Passport.")]
    public void CscsRivalryPhrasing_IsCaught(string line)
    {
        var hits = ContentLint.ScanLines("seed.json", new[] { line });

        Assert.Contains(hits, v => v.Rule == "cscs-positioning");
    }

    /// <summary>The legitimate understated CSCS add-on line is NOT flagged (Plan §6.2).</summary>
    [Theory]
    [InlineData("CSCS card check, when you need it")]
    [InlineData("Add card verification as an option alongside your compliance record — no fuss, no upsell.")]
    public void LegitimateCscsAddOnLine_IsNotFlagged(string line)
    {
        var hits = ContentLint.ScanLines("seed.json", new[] { line });

        Assert.DoesNotContain(hits, v => v.Rule == "cscs-positioning");
    }

    /// <summary>A reviewed allowlist entry exempts an otherwise-matching line from every rule.</summary>
    [Fact]
    public void AllowlistedLine_IsExempted()
    {
        var line = "Our SLA-backed uptime helps ensure the service is there when you need it.";

        var withoutAllow = ContentLint.ScanLines("seed.cshtml", new[] { line });
        var withAllow = ContentLint.ScanLines("seed.cshtml", new[] { line }, new[] { "SLA-backed uptime" });

        Assert.Contains(withoutAllow, v => v.Rule == "absolute-compliance");
        Assert.Empty(withAllow);
    }
}
