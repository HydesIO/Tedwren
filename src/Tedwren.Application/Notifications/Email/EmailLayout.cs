using System.Net;
using Tedwren.Abstractions.Configuration;

namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// Wraps composed content in the branded Tedwren email shell: a hidden preheader, a table-based outer layout,
/// the Tedwren logo in the top-left header, a white content container, and the "Private &amp; Confidential /
/// Tedwren Ltd" footer. The markup is deliberately old-school (tables, inline styles, a single responsive
/// &lt;style&gt; block) because that is what renders reliably in Outlook, Gmail and webmail. Branding text and
/// the logo URL come from <see cref="EmailOptions"/>.
/// </summary>
internal static class EmailLayout
{
    /// <summary>The one responsive rule the template relies on; kept in a &lt;style&gt; block (the only CSS braces).</summary>
    private const string HeadStyle =
        "<style type=\"text/css\">\n" +
        "  body,table,td,a{-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;}\n" +
        "  table,td{mso-table-lspace:0pt;mso-table-rspace:0pt;}\n" +
        "  img{-ms-interpolation-mode:bicubic;border:0;outline:none;text-decoration:none;}\n" +
        "  body{margin:0;padding:0;width:100%!important;height:100%!important;}\n" +
        "  @media only screen and (max-width:600px){\n" +
        "    .email-container{width:100%!important;}\n" +
        "    .email-pad{padding-left:20px!important;padding-right:20px!important;}\n" +
        "  }\n" +
        "</style>";

    /// <summary>Builds the full HTML document from the container's inner <paramref name="contentHtml"/>.</summary>
    /// <param name="options">Branding (logo URL, footer legal text) for this deployment.</param>
    /// <param name="subject">Used as the document title.</param>
    /// <param name="contentHtml">The already-composed inner HTML for the content container.</param>
    /// <param name="preheader">Optional inbox-preview snippet; falls back to the subject.</param>
    public static string Render(EmailOptions options, string subject, string contentHtml, string? preheader = null)
    {
        var title = WebUtility.HtmlEncode(subject);
        var preview = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(preheader) ? subject : preheader);
        var logoUrl = WebUtility.HtmlEncode(BuildLogoUrl(options));
        var brandName = WebUtility.HtmlEncode(options.FromName);
        var legalName = WebUtility.HtmlEncode(options.CompanyLegalName);
        var address = WebUtility.HtmlEncode(options.CompanyAddress);
        var notice = WebUtility.HtmlEncode(options.ConfidentialityNotice);

        var addressLine = string.IsNullOrWhiteSpace(address)
            ? legalName
            : $"{legalName} &middot; {address}";

        return
            "<!DOCTYPE html>\n" +
            "<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">\n" +
            "<head>\n" +
            "<meta charset=\"utf-8\">\n" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n" +
            "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\n" +
            $"<title>{title}</title>\n" +
            HeadStyle + "\n" +
            "</head>\n" +
            $"<body style=\"margin:0;padding:0;background-color:{EmailPalette.PageBackground};\">\n" +
            $"<div style=\"display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;height:0;width:0;\">{preview}</div>\n" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" " +
            $"bgcolor=\"{EmailPalette.PageBackground}\" style=\"background-color:{EmailPalette.PageBackground};\"><tr>" +
            "<td align=\"center\" style=\"padding:24px 12px;\">" +

            // Content container
            "<table role=\"presentation\" class=\"email-container\" width=\"" + EmailPalette.ContainerWidth + "\" " +
            "cellpadding=\"0\" cellspacing=\"0\" border=\"0\" " +
            $"style=\"width:{EmailPalette.ContainerWidth}px;max-width:{EmailPalette.ContainerWidth}px;" +
            $"background-color:{EmailPalette.ContainerBackground};border:1px solid {EmailPalette.Border};" +
            "border-radius:10px;overflow:hidden;\">" +

            // Header — logo top-left with wordmark
            "<tr><td class=\"email-pad\" align=\"left\" " +
            $"style=\"padding:22px 32px;border-bottom:1px solid {EmailPalette.Border};\">" +
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>" +
            $"<td valign=\"middle\" style=\"padding-right:12px;\"><img src=\"{logoUrl}\" width=\"40\" height=\"40\" " +
            $"alt=\"{brandName}\" style=\"display:block;border:0;width:40px;height:40px;\"></td>" +
            $"<td valign=\"middle\" style=\"font-family:{EmailPalette.FontFamily};font-size:20px;font-weight:700;" +
            $"letter-spacing:0.2px;color:{EmailPalette.Text};\">{brandName}</td>" +
            "</tr></table></td></tr>" +

            // Content container body
            $"<tr><td class=\"email-pad\" style=\"padding:28px 32px;\">{contentHtml}</td></tr>" +

            // Footer — private & confidential
            "<tr><td class=\"email-pad\" " +
            $"style=\"padding:20px 32px 24px 32px;background-color:{EmailPalette.PageBackground};" +
            $"border-top:1px solid {EmailPalette.Border};\">" +
            $"<p style=\"margin:0 0 6px 0;font-family:{EmailPalette.FontFamily};font-size:12px;font-weight:700;" +
            $"text-transform:uppercase;letter-spacing:0.4px;color:{EmailPalette.Muted};\">Private &amp; Confidential</p>" +
            $"<p style=\"margin:0 0 8px 0;font-family:{EmailPalette.FontFamily};font-size:12px;line-height:1.5;" +
            $"color:{EmailPalette.Muted};\">{notice}</p>" +
            $"<p style=\"margin:0;font-family:{EmailPalette.FontFamily};font-size:12px;line-height:1.5;" +
            $"color:{EmailPalette.Muted};\">{addressLine}</p>" +
            "</td></tr>" +

            "</table>" + // content container
            "</td></tr></table>\n" +
            "</body>\n</html>";
    }

    /// <summary>Builds the absolute logo URL from the public base URL, tolerating a trailing slash.</summary>
    private static string BuildLogoUrl(EmailOptions options)
    {
        var baseUrl = (options.PublicBaseUrl ?? string.Empty).TrimEnd('/');
        return $"{baseUrl}/api/email-assets/logo.png";
    }
}
