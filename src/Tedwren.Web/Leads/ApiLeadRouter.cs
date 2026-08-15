using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Tedwren.Web.Leads;

/// <summary>
/// The production <see cref="ILeadRouter"/>: forwards captured demo and contact leads to the API's anonymous
/// <c>/api/leads/capture</c> endpoint (Web Plan §7) so they land in the admin sales pipeline, and logs where
/// each was routed. Delivery failures are logged and swallowed — a lead is never lost to the visitor, and the
/// capture endpoint deduplicates open leads so a retry is harmless. Replaces <see cref="LoggingLeadRouter"/>.
/// </summary>
public sealed class ApiLeadRouter : ILeadRouter
{
    private readonly HttpClient _http;
    private readonly WebApiOptions _options;
    private readonly ILogger<ApiLeadRouter> _logger;

    /// <summary>Injects the typed HttpClient, API options and logger.</summary>
    public ApiLeadRouter(HttpClient http, IOptions<WebApiOptions> options, ILogger<ApiLeadRouter> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RouteDemoAsync(DemoLead lead, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Demo lead routed to {Destination}: {Company} ({CompanyType}), pilot={Pilot}",
            lead.Destination, lead.Company, lead.CompanyType, lead.InterestedInPilot);

        var note = $"Demo request. Role: {lead.Role}. Company type: {lead.CompanyType}. " +
                   $"Headcount/sites: {lead.HeadcountOrSites}. Pilot interest: {(lead.InterestedInPilot ? "yes" : "no")}.";
        await ForwardAsync(new
        {
            companyName = lead.Company,
            contactName = lead.Name,
            contactEmail = lead.WorkEmail,
            model = MapModel(lead.CompanyType),
            numberOfSites = TryParseSites(lead.HeadcountOrSites),
            source = "demo",
            note,
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RouteContactAsync(ContactLead lead, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Contact message routed to {Destination}: reason={Reason} from {Email}",
            lead.Destination, lead.Reason, lead.Email);

        await ForwardAsync(new
        {
            companyName = lead.Name,
            contactName = lead.Name,
            contactEmail = lead.Email,
            source = "contact",
            note = $"Contact ({lead.Reason}): {lead.Message}",
        }, cancellationToken);
    }

    /// <summary>POSTs a capture payload to the API, logging and swallowing any failure.</summary>
    private async Task ForwardAsync(object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogInformation("Lead captured (no API base URL configured, not forwarded).");
            return;
        }

        try
        {
            using var response = await _http.PostAsJsonAsync("api/leads/capture", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Lead capture forward returned {Status}.", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lead capture forward failed.");
        }
    }

    /// <summary>Maps a free-text company type to a lead model name the API understands.</summary>
    private static string MapModel(string? companyType) => (companyType ?? string.Empty).ToLowerInvariant() switch
    {
        var t when t.Contains("main") || t.Contains("principal") => "MainContractor",
        var t when t.Contains("sub") => "Subcontractor",
        _ => "Unknown",
    };

    /// <summary>Extracts a leading site/headcount number from free text, or null.</summary>
    private static int? TryParseSites(string? headcountOrSites)
    {
        if (string.IsNullOrWhiteSpace(headcountOrSites))
        {
            return null;
        }

        var digits = new string(headcountOrSites.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : null;
    }
}
