using Microsoft.Extensions.Logging;

namespace Tedwren.Web.Leads;

/// <summary>
/// The launch <see cref="ILeadRouter"/>: it records where each lead was routed via the logger. There is
/// deliberately no CRM/email integration in this project yet — this keeps the capture, validation,
/// routing and attribution real and testable, with the delivery integration slotting in behind the
/// interface later (Web Plan §7).
/// </summary>
public sealed class LoggingLeadRouter : ILeadRouter
{
    private readonly ILogger<LoggingLeadRouter> _logger;

    /// <summary>Injects the logger.</summary>
    /// <param name="logger">Logger.</param>
    public LoggingLeadRouter(ILogger<LoggingLeadRouter> logger) => _logger = logger;

    /// <inheritdoc />
    public Task RouteDemoAsync(DemoLead lead, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Demo lead routed to {Destination}: {Company} ({CompanyType}), pilot={Pilot}, utm_source={UtmSource}",
            lead.Destination, lead.Company, lead.CompanyType, lead.InterestedInPilot, lead.UtmSource ?? "(none)");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RouteContactAsync(ContactLead lead, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Contact message routed to {Destination}: reason={Reason} from {Email}",
            lead.Destination, lead.Reason, lead.Email);
        return Task.CompletedTask;
    }
}
