using System.Net;
using System.Text;

namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// A kit of optional, email-client-safe HTML fragments that are composed into the content container of the
/// branded template. Every fragment is table-based with inline styles only (no flexbox/grid, no external CSS,
/// no SVG) so it renders consistently across Outlook, Gmail, Apple Mail and webmail. Callers assemble these
/// via <see cref="EmailContentBuilder"/>; the fragments carry no branding chrome of their own.
/// </summary>
public static class EmailComponents
{
    /// <summary>Renders a section heading. <paramref name="level"/> 1 is the largest; 2 (default) suits most sections.</summary>
    public static string Heading(string text, int level = 2)
    {
        var size = level <= 1 ? "24px" : level == 2 ? "20px" : "16px";
        return $"<h{Clamp(level)} style=\"margin:0 0 12px 0;font-family:{EmailPalette.FontFamily};" +
               $"font-size:{size};line-height:1.3;font-weight:700;color:{EmailPalette.Text};\">" +
               $"{Encode(text)}</h{Clamp(level)}>";
    }

    /// <summary>Renders a body paragraph. Line breaks in <paramref name="text"/> are preserved as &lt;br&gt;.</summary>
    public static string Paragraph(string text)
    {
        return $"<p style=\"margin:0 0 16px 0;font-family:{EmailPalette.FontFamily};font-size:15px;" +
               $"line-height:1.6;color:{EmailPalette.Text};\">{EncodeMultiline(text)}</p>";
    }

    /// <summary>Renders a bulletproof, brand-coloured call-to-action button linking to <paramref name="url"/>.</summary>
    public static string Button(string label, string url)
    {
        var href = Encode(url);
        return
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:8px 0 20px 0;\"><tr>" +
            $"<td align=\"center\" bgcolor=\"{EmailPalette.Brand}\" style=\"border-radius:6px;\">" +
            $"<a href=\"{href}\" target=\"_blank\" style=\"display:inline-block;padding:12px 28px;" +
            $"font-family:{EmailPalette.FontFamily};font-size:15px;font-weight:600;line-height:1;" +
            $"color:{EmailPalette.ButtonText};text-decoration:none;border-radius:6px;\">{Encode(label)}</a>" +
            "</td></tr></table>";
    }

    /// <summary>
    /// Renders a prominent verification / two-factor code block (large, letter-spaced digits) with an optional
    /// caption above it.
    /// </summary>
    public static string VerificationCode(string code, string? caption = null)
    {
        var captionHtml = string.IsNullOrWhiteSpace(caption)
            ? string.Empty
            : $"<p style=\"margin:0 0 8px 0;font-family:{EmailPalette.FontFamily};font-size:13px;" +
              $"color:{EmailPalette.Muted};\">{Encode(caption)}</p>";
        return
            captionHtml +
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" " +
            "style=\"margin:0 0 20px 0;\"><tr>" +
            $"<td align=\"center\" bgcolor=\"{EmailPalette.TableHeaderBackground}\" " +
            $"style=\"padding:18px;border:1px solid {EmailPalette.Border};border-radius:8px;\">" +
            $"<span style=\"font-family:'Courier New',Courier,monospace;font-size:32px;font-weight:700;" +
            $"letter-spacing:8px;color:{EmailPalette.Text};\">{Encode(code)}</span>" +
            "</td></tr></table>";
    }

    /// <summary>Renders a highlighted callout box (optional bold <paramref name="title"/> then body text).</summary>
    public static string Callout(string text, string? title = null)
    {
        var titleHtml = string.IsNullOrWhiteSpace(title)
            ? string.Empty
            : $"<strong style=\"display:block;margin-bottom:4px;font-family:{EmailPalette.FontFamily};" +
              $"font-size:15px;color:{EmailPalette.Text};\">{Encode(title)}</strong>";
        return
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" " +
            "style=\"margin:0 0 20px 0;\"><tr>" +
            $"<td style=\"padding:14px 16px;background-color:{EmailPalette.CalloutBackground};" +
            $"border-left:4px solid {EmailPalette.CalloutAccent};border-radius:4px;\">" +
            titleHtml +
            $"<span style=\"font-family:{EmailPalette.FontFamily};font-size:14px;line-height:1.6;" +
            $"color:{EmailPalette.Text};\">{EncodeMultiline(text)}</span>" +
            "</td></tr></table>";
    }

    /// <summary>Renders inline highlighted text (a coloured background span) for emphasis within a paragraph.</summary>
    public static string Highlight(string text) =>
        $"<span style=\"background-color:{EmailPalette.CalloutBackground};color:{EmailPalette.BrandDark};" +
        $"padding:1px 5px;border-radius:3px;font-weight:600;\">{Encode(text)}</span>";

    /// <summary>Renders a data table with a header row and body rows. Ragged rows are padded with empty cells.</summary>
    public static string Table(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" " +
                  $"style=\"margin:0 0 20px 0;border:1px solid {EmailPalette.Border};border-radius:6px;" +
                  "border-collapse:separate;\">");

        sb.Append("<tr>");
        foreach (var header in headers)
        {
            sb.Append($"<th align=\"left\" bgcolor=\"{EmailPalette.TableHeaderBackground}\" " +
                      $"style=\"padding:10px 12px;font-family:{EmailPalette.FontFamily};font-size:13px;" +
                      $"font-weight:700;color:{EmailPalette.Muted};border-bottom:1px solid {EmailPalette.Border};" +
                      "text-transform:uppercase;letter-spacing:0.3px;\">" + Encode(header) + "</th>");
        }
        sb.Append("</tr>");

        for (var r = 0; r < rows.Count; r++)
        {
            sb.Append("<tr>");
            for (var c = 0; c < headers.Count; c++)
            {
                var value = c < rows[r].Count ? rows[r][c] : string.Empty;
                var border = r == rows.Count - 1 ? string.Empty : $"border-bottom:1px solid {EmailPalette.Border};";
                sb.Append($"<td align=\"left\" style=\"padding:10px 12px;font-family:{EmailPalette.FontFamily};" +
                          $"font-size:14px;color:{EmailPalette.Text};{border}\">" + Encode(value) + "</td>");
            }
            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    /// <summary>Renders a label/value list as a two-column table (e.g. reference, date, site).</summary>
    public static string KeyValueRows(IReadOnlyList<KeyValuePair<string, string>> pairs)
    {
        var sb = new StringBuilder();
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" " +
                  "style=\"margin:0 0 20px 0;\">");
        foreach (var pair in pairs)
        {
            sb.Append("<tr>");
            sb.Append($"<td align=\"left\" valign=\"top\" style=\"padding:6px 12px 6px 0;width:40%;" +
                      $"font-family:{EmailPalette.FontFamily};font-size:14px;color:{EmailPalette.Muted};\">" +
                      Encode(pair.Key) + "</td>");
            sb.Append($"<td align=\"left\" valign=\"top\" style=\"padding:6px 0;" +
                      $"font-family:{EmailPalette.FontFamily};font-size:14px;font-weight:600;" +
                      $"color:{EmailPalette.Text};\">" + Encode(pair.Value) + "</td>");
            sb.Append("</tr>");
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    /// <summary>Renders a bulleted list.</summary>
    public static string BulletList(IReadOnlyList<string> items)
    {
        var sb = new StringBuilder();
        sb.Append($"<ul style=\"margin:0 0 16px 0;padding-left:20px;font-family:{EmailPalette.FontFamily};" +
                  $"font-size:15px;line-height:1.6;color:{EmailPalette.Text};\">");
        foreach (var item in items)
        {
            sb.Append("<li style=\"margin-bottom:6px;\">" + Encode(item) + "</li>");
        }
        sb.Append("</ul>");
        return sb.ToString();
    }

    /// <summary>Renders a horizontal divider rule.</summary>
    public static string Divider() =>
        $"<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\">" +
        $"<tr><td style=\"padding:4px 0 20px 0;border-bottom:1px solid {EmailPalette.Border};font-size:0;" +
        "line-height:0;\">&nbsp;</td></tr></table>";

    /// <summary>Renders vertical whitespace of the given pixel height.</summary>
    public static string Spacer(int heightPx = 16) =>
        $"<div style=\"height:{Math.Max(0, heightPx)}px;line-height:{Math.Max(0, heightPx)}px;font-size:0;\">&nbsp;</div>";

    /// <summary>HTML-encodes caller text so it cannot break the surrounding markup.</summary>
    private static string Encode(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

    /// <summary>HTML-encodes caller text and preserves line breaks as &lt;br&gt;.</summary>
    private static string EncodeMultiline(string? text) =>
        Encode(text).Replace("\r\n", "\n").Replace("\n", "<br>");

    /// <summary>Constrains a heading level to the valid 1–3 range used by the kit.</summary>
    private static int Clamp(int level) => level < 1 ? 1 : level > 3 ? 3 : level;
}
