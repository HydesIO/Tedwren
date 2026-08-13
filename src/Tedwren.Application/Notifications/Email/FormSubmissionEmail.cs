namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// Composes the content for the "completed form" email (requirement 6): a short covering message with the
/// form's key details, sent with the branded PDF attached. Kept with the rest of the email kit so the
/// submission service stays focused on submission rules.
/// </summary>
public static class FormSubmissionEmail
{
    /// <summary>Builds the subject line for a completed form.</summary>
    public static string SubjectFor(string formName) => $"Completed form: {formName}";

    /// <summary>Builds the subject line for a failed-check alert.</summary>
    public static string FailureSubjectFor(string formName) => $"Failed check: {formName}";

    /// <summary>
    /// Builds the failed-check alert's inner content: a clear callout that a check failed, the key details, and
    /// a note that the completed report is attached (PRD §8 failed-check alert).
    /// </summary>
    public static string BuildFailureContent(string formName, string submittedBy, DateTimeOffset submittedUtc, string scope)
    {
        return new EmailContentBuilder()
            .Heading($"Failed check: {formName}")
            .Callout("A completed form recorded a red (failed) check. The full report is attached as a PDF.", "Action may be required")
            .KeyValueRows(new List<KeyValuePair<string, string>>
            {
                new("Form", formName),
                new("Level", scope),
                new("Submitted by", submittedBy),
                new("Submitted", submittedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")),
            })
            .Build();
    }

    /// <summary>
    /// Builds the email's inner content: a heading, a short note, and a label/value summary of the submission.
    /// The PDF itself is attached by the sender.
    /// </summary>
    public static string BuildContent(string formName, string submittedBy, DateTimeOffset submittedUtc, string status, string scope)
    {
        return new EmailContentBuilder()
            .Heading($"Completed form: {formName}")
            .Paragraph("A completed form is attached as a PDF. The key details are below.")
            .KeyValueRows(new List<KeyValuePair<string, string>>
            {
                new("Form", formName),
                new("Level", scope),
                new("Submitted by", submittedBy),
                new("Submitted", submittedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")),
                new("Status", status),
            })
            .Build();
    }
}
