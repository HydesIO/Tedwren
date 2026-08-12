namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// Composes the content for the console-user invitation email (SF-20). Kept separate from
/// <see cref="Tedwren.Application.Users.UserService"/> so the service stays focused on user rules and the
/// email presentation lives with the rest of the component kit. Returns the inner content HTML that the
/// sender wraps in the branded shell.
/// </summary>
public static class InviteEmail
{
    /// <summary>The subject line for the invitation email.</summary>
    public const string Subject = "You've been invited to Tedwren";

    /// <summary>
    /// Builds the invitation's inner content: a greeting, the call to accept, a button to the accept-invite
    /// link, the expiry note, and a plain fallback link in case the button can't be used.
    /// </summary>
    public static string BuildContent(string recipientName, string acceptUrl, DateTimeOffset? expiresUtc)
    {
        var greeting = string.IsNullOrWhiteSpace(recipientName) ? "Hello," : $"Hello {recipientName},";
        var expiryNote = expiresUtc is { } expiry
            ? $"This invitation expires on {expiry:dd MMM yyyy}."
            : "This invitation will expire, so please accept it soon.";

        return new EmailContentBuilder()
            .Heading("You've been invited to Tedwren")
            .Paragraph(greeting)
            .Paragraph("You've been invited to join your company's Tedwren workspace. Click the button below to set your password and sign in.")
            .Button("Accept your invitation", acceptUrl)
            .Callout($"This link is unique to you. {expiryNote}", "Keep it private")
            .Paragraph($"If the button doesn't work, copy and paste this link into your browser:\n{acceptUrl}")
            .Build();
    }
}
