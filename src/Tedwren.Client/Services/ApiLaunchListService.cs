using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.LaunchList;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ILaunchListService"/> backed by the Tedwren Web API (<c>/api/launch-signups</c>). The console
/// uses the list and notify calls (platform-admin, bearer-token authorised); the anonymous signup call exists
/// for completeness but is normally made by the marketing site rather than the console.
/// </summary>
public sealed class ApiLaunchListService : ILaunchListService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiLaunchListService(HttpClient http) => _http = http;

    /// <summary>Adds an email to the launch list (anonymous).</summary>
    public async Task<LaunchSignupResultDto> SignUpAsync(CreateLaunchSignupRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/launch-signups", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LaunchSignupResultDto>(cancellationToken))!;
    }

    /// <summary>Returns every launch-list subscriber, newest first.</summary>
    public async Task<IReadOnlyList<LaunchSignupDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _http.GetFromJsonAsync<IReadOnlyList<LaunchSignupDto>>("api/launch-signups", cancellationToken))
            ?? Array.Empty<LaunchSignupDto>();

    /// <summary>Sends the branded launch announcement to each subscriber individually.</summary>
    public async Task<NotifyLaunchResultDto> NotifyAsync(NotifyLaunchRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/launch-signups/notify", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NotifyLaunchResultDto>(cancellationToken))!;
    }
}
