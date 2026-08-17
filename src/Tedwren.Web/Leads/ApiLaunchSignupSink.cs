using System.Net.Http.Json;

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
    private readonly ILogger<ApiLaunchSignupSink> _logger;

    /// <summary>Injects the typed HttpClient (its base address is resolved at registration) and logger.</summary>
    public ApiLaunchSignupSink(HttpClient http, ILogger<ApiLaunchSignupSink> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SubmitAsync(string email, string source, CancellationToken cancellationToken = default)
    {
        // The base address is resolved at registration from LaunchSignup:ApiBaseUrl, falling back to
        // Api:BaseUrl. When neither is configured there is nowhere to forward to, so we log and no-op.
        if (_http.BaseAddress is null)
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
