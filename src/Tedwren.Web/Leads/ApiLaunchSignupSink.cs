using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Tedwren.Web.Leads;

/// <summary>
/// The launch <see cref="ILaunchSignupSink"/>: forwards a captured email to the API's anonymous
/// <c>/api/launch-signups</c> endpoint via a typed <see cref="HttpClient"/>. Delivery failures (API down,
/// misconfiguration) are logged and swallowed so a visitor's signup never surfaces an error — the API
/// deduplicates, so a retry is harmless.
/// </summary>
public sealed class ApiLaunchSignupSink : ILaunchSignupSink
{
    private readonly HttpClient _http;
    private readonly LaunchSignupOptions _options;
    private readonly ILogger<ApiLaunchSignupSink> _logger;

    /// <summary>Injects the typed HttpClient, options and logger.</summary>
    public ApiLaunchSignupSink(HttpClient http, IOptions<LaunchSignupOptions> options, ILogger<ApiLaunchSignupSink> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SubmitAsync(string email, string source, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiBaseUrl))
        {
            _logger.LogInformation("Launch signup captured (no API base URL configured, not forwarded): {Source}", source);
            return;
        }

        try
        {
            using var response = await _http.PostAsJsonAsync(
                "api/launch-signups", new { email, source }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Launch signup forward returned {Status}.", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Launch signup forward failed.");
        }
    }
}
