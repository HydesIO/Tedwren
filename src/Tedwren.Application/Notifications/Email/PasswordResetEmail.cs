namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// Composes the content for the console password-reset email (D1). Kept separate from
/// <see cref="Tedwren.Application.Auth.AuthService"/> so the service stays focused on auth rules and the
/// email presentation lives with the rest of the component kit. Returns the inner content HTML that the
/// sender wraps in the branded shell — mirroring <see cref="InviteEmail"/>.
/// </summary>
public static class PasswordResetEmail
{
    /// <summary>The subject line for the password-reset email.</summary>
    public const string Subject = "Reset your Tedwren password";

    /// <summary>
    /// Builds the reset email's inner content: a greeting, the call to reset, a button to the reset link,
    /// the expiry note, a reassurance for the unintended recipient, and a plain fallback link.
    /// </summary>
    public static string BuildContent(string recipientName, string resetUrl, DateTimeOffset? expiresUtc)
    {
        var greeting = string.IsNullOrWhiteSpace(recipientName) ? "Hello," : $"Hello {recipientName},";
        var expiryNote = expiresUtc is { } expiry
            ? $"This link expires on {expiry:dd MMM yyyy 'at' HH:mm} (UK time)."
            : "This link will expire shortly, so please use it soon.";

        return new EmailContentBuilder()
            .Heading("Reset your Tedwren password")
            .Paragraph(greeting)
            .Paragraph("We received a request to reset the password for your Tedwren console account. Click the button below to choose a new password.")
            .Button("Reset your password", resetUrl)
            .Callout($"This link is unique to you. {expiryNote}", "Keep it private")
            .Paragraph("If you didn't request this, you can safely ignore this email — your password will not change.")
            .Paragraph($"If the button doesn't work, copy and paste this link into your browser:\n{resetUrl}")
            .Build();
    }
}
