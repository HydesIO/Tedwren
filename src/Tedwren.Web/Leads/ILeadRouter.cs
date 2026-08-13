namespace Tedwren.Web.Leads;

/// <summary>
/// The seam that delivers a captured lead to wherever it needs to go (Web Plan §7) — a CRM queue, an
/// inbox, a confirmation email. The launch implementation logs; a real CRM/email integration replaces
/// it behind this interface with no controller changes.
/// </summary>
public interface ILeadRouter
{
    /// <summary>Routes a demo lead to the sales-qualified queue and triggers the booking confirmation.</summary>
    /// <param name="lead">The routed demo lead.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RouteDemoAsync(DemoLead lead, CancellationToken cancellationToken = default);

    /// <summary>Routes a contact message to the inbox its reason maps to.</summary>
    /// <param name="lead">The routed contact message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RouteContactAsync(ContactLead lead, CancellationToken cancellationToken = default);
}
