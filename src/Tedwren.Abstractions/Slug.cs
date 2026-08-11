using System.Text;

namespace Tedwren.Abstractions;

/// <summary>
/// Turns a display name into a URL-safe slug used to key detail routes to records
/// (e.g. "Meridian Construction Ltd" → "meridian-construction-ltd"). Canonical, dependency-free copy
/// shared by the API and the client so both derive the same slug from the same name.
/// </summary>
public static class Slug
{
    /// <summary>Produces the deterministic slug for <paramref name="value"/>.</summary>
    public static string From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        var lastHyphen = false;
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastHyphen = false;
            }
            else if (!lastHyphen)
            {
                sb.Append('-');
                lastHyphen = true;
            }
        }

        return sb.ToString().Trim('-');
    }
}
