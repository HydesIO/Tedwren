using System.Text;

namespace Tedwren.UiComponents.Models;

/// <summary>
/// Presentation helpers for turning machine-shaped strings into human-readable labels. Status labels reach
/// the client as raw enum names (e.g. <c>PaidOut</c>, <c>NotRun</c>, <c>InProgress</c>); the display layer
/// owns turning those into spaced words ("Paid Out", "Not Run", "In Progress").
/// </summary>
public static class DisplayText
{
    /// <summary>
    /// Splits a PascalCase / camelCase token into spaced words. Idempotent: text that is already spaced
    /// (e.g. "Paid Out", "3 months") is returned unchanged, and acronym boundaries are respected
    /// ("PDFExport" → "PDF Export").
    /// </summary>
    public static string Humanize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var sb = new StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c))
            {
                var prev = value[i - 1];
                var nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);

                // Insert a space at a word boundary: a lower/digit → upper transition, or the end of an
                // acronym before a new word (e.g. "AB Cd"). Never double an existing space.
                var boundary = (!char.IsUpper(prev) && prev != ' ')
                    || (char.IsUpper(prev) && nextIsLower);
                if (boundary && prev != ' ')
                {
                    sb.Append(' ');
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
