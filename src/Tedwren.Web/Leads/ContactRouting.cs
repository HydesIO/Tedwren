namespace Tedwren.Web.Leads;

/// <summary>Pure routing rules for contact messages (Web Plan §6.9), kept testable without a web host.</summary>
public static class ContactRouting
{
    /// <summary>
    /// Resolves the inbox a contact reason routes to: the reason's configured inbox if present,
    /// otherwise the sales inbox, otherwise a clearly-labelled unrouted fallback so a misconfiguration
    /// is visible rather than silently dropping the message.
    /// </summary>
    /// <param name="reason">The contact reason.</param>
    /// <param name="options">Lead options carrying the inbox map.</param>
    public static string ResolveInbox(ContactReason reason, LeadOptions options)
    {
        if (options.ContactInboxes.TryGetValue(reason, out var inbox) && !string.IsNullOrWhiteSpace(inbox))
        {
            return inbox;
        }

        return !string.IsNullOrWhiteSpace(options.SalesInbox) ? options.SalesInbox! : "unrouted";
    }
}
