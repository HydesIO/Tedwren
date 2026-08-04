using System.Text;

namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// Turns a display name into a URL-safe slug used to key detail-page routes to sample
/// records (e.g. "Meridian Construction Ltd" → "meridian-construction-ltd"). Deterministic
/// so a list page and a detail page derive the same slug from the same name.
/// </summary>
public static class Slugs
{
    public static string From(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

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
