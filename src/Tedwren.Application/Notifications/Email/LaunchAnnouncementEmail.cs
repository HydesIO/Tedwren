namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// Composes the content for the launch-announcement email sent to everyone on the launch list (Web Content
/// Spec §6.9). Kept with the rest of the component kit so the presentation lives apart from the launch-list
/// service. Returns the inner content HTML that the sender wraps in the branded shell.
/// </summary>
public static class LaunchAnnouncementEmail
{
    /// <summary>The subject line for the launch announcement.</summary>
    public const string Subject = "Tedwren is live — start your trial";

    /// <summary>
    /// Builds the announcement's inner content: a concise "we've launched" message, the two products, and a
    /// button to start a trial. <paramref name="startUrl"/> is the marketing/sign-up URL to land on;
    /// <paramref name="unsubscribeUrl"/> is the recipient's one-click opt-out link (PECR/GDPR).
    /// </summary>
    public static string BuildContent(string startUrl, string unsubscribeUrl)
    {
        return new EmailContentBuilder()
            .Heading("Tedwren is live")
            .Paragraph("Thanks for registering your interest — Tedwren has launched, and you're among the first to hear.")
            .Paragraph("Tedwren keeps site compliance straightforward for both sides of the job: subcontractors managing their operatives' cards and timesheets, and main contractors running inductions and site entry across their sites.")
            .Button("Start your trial", startUrl)
            .Callout("You're receiving this because you asked to be told when Tedwren launched.")
            .Paragraph($"If the button doesn't work, copy and paste this link into your browser:\n{startUrl}")
            .Divider()
            .Paragraph($"Don't want these emails? Unsubscribe here: {unsubscribeUrl}")
            .Build();
    }
}
