using System.Net.Mail;

namespace Tedwren.Application.Common;

/// <summary>
/// A single, shared server-side email-format check used by the public-facing slices (launch list, leads,
/// affiliates). The marketing-site forms validate client-side, but the API must not trust that — every write
/// path that accepts an email revalidates here so an invalid address is rejected with a 400, not stored.
/// </summary>
public static class EmailValidation
{
    /// <summary>True when <paramref name="email"/> is a syntactically valid single email address.</summary>
    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        // MailAddress.TryCreate is permissive about display names; require the parsed address to equal the input
        // (trimmed) and to contain a dot in the host, rejecting "a@b" and "Name <a@b.com>" forms.
        if (!MailAddress.TryCreate(email.Trim(), out var parsed))
        {
            return false;
        }

        return string.Equals(parsed.Address, email.Trim(), StringComparison.OrdinalIgnoreCase)
            && parsed.Host.Contains('.');
    }
}
