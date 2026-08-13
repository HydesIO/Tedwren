namespace Tedwren.Web.Leads;

/// <summary>
/// A routed demo lead (Web Plan §7). Captures the sales-qualified details plus the pilot flag and UTM
/// attribution, and the inbox it was routed to.
/// </summary>
/// <param name="Name">Requester name.</param>
/// <param name="WorkEmail">Work email.</param>
/// <param name="Company">Company.</param>
/// <param name="Role">Role.</param>
/// <param name="CompanyType">Company type.</param>
/// <param name="HeadcountOrSites">Headcount or number of sites.</param>
/// <param name="Phone">Optional phone.</param>
/// <param name="InterestedInPilot">Pilot-interest flag (conversion signal).</param>
/// <param name="UtmSource">Captured UTM source (e.g. "pack").</param>
/// <param name="UtmMedium">Captured UTM medium.</param>
/// <param name="UtmCampaign">Captured UTM campaign.</param>
/// <param name="Destination">The inbox/queue the lead was routed to.</param>
public sealed record DemoLead(
    string Name,
    string WorkEmail,
    string Company,
    string Role,
    string CompanyType,
    string HeadcountOrSites,
    string? Phone,
    bool InterestedInPilot,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string Destination);

/// <summary>A routed contact message (Web Plan §6.9): the message plus the inbox it was routed to.</summary>
/// <param name="Name">Sender name.</param>
/// <param name="Email">Sender email.</param>
/// <param name="Reason">Contact reason.</param>
/// <param name="Message">Message body.</param>
/// <param name="Destination">The inbox the message was routed to.</param>
public sealed record ContactLead(
    string Name,
    string Email,
    ContactReason Reason,
    string Message,
    string Destination);
