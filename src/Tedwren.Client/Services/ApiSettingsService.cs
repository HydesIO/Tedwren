using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Settings;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ISettingsService"/> backed by the Tedwren Web API (<c>/api/settings</c>). Reads and saves a
/// company's general settings for System Configuration.
/// </summary>
public sealed class ApiSettingsService : ISettingsService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiSettingsService(HttpClient http) => _http = http;

    /// <summary>Gets the company's general settings from the API (server returns defaults when unset).</summary>
    public async Task<GeneralSettingsDto> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<GeneralSettingsDto>($"api/settings/{companyId}", cancellationToken)
        ?? new GeneralSettingsDto(string.Empty, "Europe/London", false, false, false, false);

    /// <summary>Saves the company's general settings via the API.</summary>
    public async Task SaveForCompanyAsync(Guid companyId, GeneralSettingsDto settings, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/settings/{companyId}", settings, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
