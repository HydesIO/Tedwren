namespace Tedwren.Web.Leads;

/// <summary>
/// The reason a contact message was sent (Web Plan §6.9, §7). The reason routes the message to the
/// right inbox, so General, Press, Partner and Support don't all land in one place.
/// </summary>
public enum ContactReason
{
    /// <summary>A general enquiry.</summary>
    General,

    /// <summary>A press or media enquiry.</summary>
    Press,

    /// <summary>A partner-programme enquiry.</summary>
    Partner,

    /// <summary>An existing-customer support request.</summary>
    Support,
}
