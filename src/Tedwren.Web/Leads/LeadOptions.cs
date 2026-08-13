namespace Tedwren.Web.Leads;

/// <summary>
/// Options for lead capture (Web Plan §6.9, §7), bound from the "Leads" configuration section: the
/// booking-calendar URL shown after a demo request, the inbox each contact reason routes to, and the
/// anti-bot minimum form-fill time. These are config so a redeploy isn't needed to change a routing
/// address or the booking link.
/// </summary>
public sealed class LeadOptions
{
    /// <summary>Configuration section name this options type binds from.</summary>
    public const string SectionName = "Leads";

    /// <summary>Calendar booking URL a demo requester is sent to after submitting (real booking, not "we'll be in touch").</summary>
    public string? BookingCalendarUrl { get; set; }

    /// <summary>The inbox that receives sales-qualified demo leads.</summary>
    public string? SalesInbox { get; set; }

    /// <summary>Contact-reason → inbox map. Missing reasons fall back to <see cref="SalesInbox"/>.</summary>
    public Dictionary<ContactReason, string> ContactInboxes { get; set; } = new();

    /// <summary>Minimum seconds a genuine human takes to fill the form; faster submits are treated as bots.</summary>
    public int MinFormSeconds { get; set; } = 2;
}
