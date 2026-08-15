namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// Composes the content for the two affiliate emails (Web Plan §7): the setup notification (with the terms
/// summary and the link to sign the agreement) and the signed confirmation (which carries the countersigned
/// PDF as an attachment). Kept with the rest of the component kit so the presentation lives apart from the
/// affiliate service. Each returns the inner content HTML the sender wraps in the branded shell.
/// </summary>
public static class AffiliateEmails
{
    /// <summary>Subject for the affiliate setup notification.</summary>
    public const string SetupSubject = "You've been set up as a Tedwren affiliate";

    /// <summary>Subject for the signed-agreement confirmation.</summary>
    public const string SignedSubject = "Your Tedwren affiliate agreement is signed";

    /// <summary>
    /// Builds the setup email: a greeting, the commission terms (profit-share), and a button to read and sign
    /// the agreement.
    /// </summary>
    public static string BuildSetupContent(string name, decimal ratePct, decimal marginPct, string agreementUrl)
    {
        var greeting = string.IsNullOrWhiteSpace(name) ? "Hello," : $"Hello {name},";
        var rate = Percent(ratePct);
        var margin = Percent(marginPct);

        return new EmailContentBuilder()
            .Heading("Welcome to the Tedwren affiliate programme")
            .Paragraph(greeting)
            .Paragraph("You've been set up as a Tedwren affiliate. Here are your commission terms:")
            .KeyValueRows(new[]
            {
                new KeyValuePair<string, string>("Commission basis", "Share of profit (not revenue)"),
                new KeyValuePair<string, string>("Profit margin applied", margin),
                new KeyValuePair<string, string>("Your rate of that profit", rate),
            })
            .Paragraph("Please read and sign your affiliate agreement to activate your account:")
            .Button("Read & sign your agreement", agreementUrl)
            .Paragraph($"If the button doesn't work, copy and paste this link into your browser:\n{agreementUrl}")
            .Build();
    }

    /// <summary>Builds the signed-confirmation email; the countersigned PDF is attached by the sender.</summary>
    public static string BuildSignedContent(string name)
    {
        var greeting = string.IsNullOrWhiteSpace(name) ? "Hello," : $"Hello {name},";
        return new EmailContentBuilder()
            .Heading("Your affiliate agreement is signed")
            .Paragraph(greeting)
            .Paragraph("Thank you — your Tedwren affiliate agreement has been signed. A countersigned PDF copy is attached for your records.")
            .Callout("Keep this email — it's your confirmation that the agreement is in place.")
            .Build();
    }

    /// <summary>Formats a fraction as a whole-number percentage (0.20 → "20%").</summary>
    private static string Percent(decimal fraction) => $"{decimal.Round(fraction * 100, 2, MidpointRounding.AwayFromZero):0.##}%";
}
