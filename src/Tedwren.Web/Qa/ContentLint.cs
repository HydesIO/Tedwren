using System.Text.RegularExpressions;

namespace Tedwren.Web.Qa;

/// <summary>
/// A single content-lint breach: the file and 1-based line it was found on, the rule that fired, and
/// the offending text, so a failing gate points a reviewer straight at the hit.
/// </summary>
/// <param name="File">Path (or logical name) of the scanned file.</param>
/// <param name="Line">1-based line number the match was found on.</param>
/// <param name="Rule">Identifier of the rule that fired (e.g. "hardcoded-price").</param>
/// <param name="Snippet">The offending line, trimmed for display.</param>
public sealed record LintViolation(string File, int Line, string Rule, string Snippet);

/// <summary>
/// Pre-launch content gate (Web Plan §8, §14). The plan requires the three commercial/legal copy
/// constraints to be enforced as mechanisms that fail the build, not notes a reviewer might miss, so
/// this scans the site's content JSON and Razor views for:
/// <list type="bullet">
///   <item><b>hardcoded-price</b> — a currency symbol typed into copy instead of coming from the single
///   <c>PricingPlan</c> source (Plan §6.5, §8; §14 "grep for hardcoded £").</item>
///   <item><b>absolute-compliance</b> — an absolute-compliance claim: <i>guarantee</i>, <i>ensure</i>,
///   <i>100% compliant</i>, <i>fully compliant</i> (Plan §8.1; §14). Reviewed exceptions go through the
///   allowlist rather than weakening the rule.</item>
///   <item><b>cscs-positioning</b> — CSCS rivalry / replacement / on-demand-verification phrasing, or
///   positioning against the CSCS Digital Skills Passport, checked in titles and meta as well as body
///   copy (Plan §8.2; §14). A plain factual CSCS add-on mention is fine — only rivalry patterns fire.</item>
/// </list>
/// It is a pure function over text so the QA test suite runs it as the enforcement gate and also proves
/// it catches seeded breaches. Single responsibility: detect breaches; it neither reads configuration
/// nor decides what to do about a hit.
/// </summary>
public static class ContentLint
{
    /// <summary>File extensions that carry customer-visible copy and are therefore scanned.</summary>
    private static readonly string[] ScannedExtensions = { ".json", ".cshtml" };

    /// <summary>The rule set. Each entry is a rule id plus the pattern that signals a breach.</summary>
    private static readonly (string Rule, Regex Pattern)[] Rules =
    {
        // A price symbol in copy: prices must come from PricingPlan numeric fields via the content
        // provider's formatter (the sole sanctioned "£" lives in that C# formatter, not in copy). Any
        // literal "£" is a breach; "$"/"€" only when next to a digit, so Razor/C# string interpolation
        // ("$\"…\"") in a .cshtml code block is not mistaken for a dollar price.
        ("hardcoded-price", new Regex(@"£|[$€]\s?\d|\d\s?€", RegexOptions.Compiled)),

        // Absolute-compliance claims (Plan §8.1). Word-boundary anchored so "reassure" ≠ "ensure".
        ("absolute-compliance", new Regex(
            @"\bguarantee|\bensure|100\s*%\s*complian|fully\s+complian",
            RegexOptions.Compiled | RegexOptions.IgnoreCase)),

        // CSCS rivalry/replacement/on-demand phrasing (Plan §8.2). Replacement/rivalry words are matched
        // only when they sit near "CSCS" (either order, same line) so the legitimate understated add-on
        // line ("CSCS card check … as an option") does not trip the gate; the on-demand-verification and
        // Digital Skills Passport positionings are breaches on their own.
        ("cscs-positioning", new Regex(
            @"(replac\w*|instead\s+of|alternative\s+to|rival\w*|better\s+than|substitut\w*)[^.\n]{0,40}cscs" +
            @"|cscs[^.\n]{0,40}(replac\w*|instead|alternative|rival\w*|better\s+than|substitut\w*)" +
            @"|on[\s-]?demand\s+verification" +
            @"|digital\s+skills\s+passport",
            RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    };

    /// <summary>
    /// Scans a built site's copy — the <c>Content</c> JSON and the <c>Views</c> Razor files under the
    /// given <c>Tedwren.Web</c> project directory — and returns every breach found.
    /// </summary>
    /// <param name="webProjectDir">Absolute path to the <c>src/Tedwren.Web</c> project directory.</param>
    /// <param name="allowlist">Reviewed exceptions: a line containing any of these substrings is skipped.</param>
    public static IReadOnlyList<LintViolation> ScanWebProject(
        string webProjectDir, IReadOnlyCollection<string>? allowlist = null)
    {
        var violations = new List<LintViolation>();
        foreach (var dir in new[] { "Content", "Views" })
        {
            var root = Path.Combine(webProjectDir, dir);
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var path in EnumerateScannableFiles(root))
            {
                violations.AddRange(ScanFile(path, allowlist));
            }
        }

        return violations;
    }

    /// <summary>Scans a single file on disk, keying line numbers to the file's contents.</summary>
    /// <param name="path">Path to the file to scan.</param>
    /// <param name="allowlist">Reviewed exceptions: a line containing any of these substrings is skipped.</param>
    public static IReadOnlyList<LintViolation> ScanFile(
        string path, IReadOnlyCollection<string>? allowlist = null) =>
        ScanLines(path, File.ReadLines(path), allowlist);

    /// <summary>
    /// Scans an in-memory sequence of lines. This is the primitive the file and project scans build on,
    /// and the seam the QA tests use to prove each rule fires on a seeded breach.
    /// </summary>
    /// <param name="file">Logical file name reported on any violation.</param>
    /// <param name="lines">The lines to scan, in order.</param>
    /// <param name="allowlist">Reviewed exceptions: a line containing any of these substrings is skipped.</param>
    public static IReadOnlyList<LintViolation> ScanLines(
        string file, IEnumerable<string> lines, IReadOnlyCollection<string>? allowlist = null)
    {
        var violations = new List<LintViolation>();
        var lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            if (IsAllowlisted(line, allowlist))
            {
                continue;
            }

            foreach (var (rule, pattern) in Rules)
            {
                if (pattern.IsMatch(line))
                {
                    violations.Add(new LintViolation(file, lineNumber, rule, line.Trim()));
                }
            }
        }

        return violations;
    }

    /// <summary>Enumerates scannable copy files under a root, skipping build output directories.</summary>
    private static IEnumerable<string> EnumerateScannableFiles(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(p => ScannedExtensions.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    /// <summary>True when a reviewed allowlist entry appears in the line, exempting it from every rule.</summary>
    private static bool IsAllowlisted(string line, IReadOnlyCollection<string>? allowlist) =>
        allowlist is not null &&
        allowlist.Any(entry => line.Contains(entry, StringComparison.OrdinalIgnoreCase));
}
