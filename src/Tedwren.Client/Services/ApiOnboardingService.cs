using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Onboarding;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IOnboardingService"/> backed by the Tedwren Web API (<c>/api/onboarding</c>). The create call
/// carries the admin's bearer token; the recipient view/submit/cards calls are anonymous, token+passcode gated.
/// </summary>
public sealed class ApiOnboardingService : IOnboardingService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiOnboardingService(HttpClient http) => _http = http;

    /// <summary>Creates an onboarding link (admin) and returns the token + optional passcode.</summary>
    public async Task<OnboardingLinkDto> CreateAsync(CreateOnboardingLinkRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/onboarding", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OnboardingLinkDto>(cancellationToken))!;
    }

    /// <summary>Gets the operative-facing view, or null when the link/passcode is rejected (403).</summary>
    public async Task<OnboardingViewDto?> GetByTokenAsync(string token, string? passcode, CancellationToken cancellationToken = default) =>
        await GetOrNullAsync($"api/onboarding/view?token={Uri.EscapeDataString(token)}&passcode={Uri.EscapeDataString(passcode ?? string.Empty)}", cancellationToken);

    /// <summary>Submits the operative's details. Null on a rejected link.</summary>
    public async Task<OnboardingViewDto?> SubmitDetailsAsync(string token, string? passcode, SubmitOnboardingDetailsRequest request, CancellationToken cancellationToken = default) =>
        await PostOrNullAsync($"api/onboarding/submit?token={Uri.EscapeDataString(token)}&passcode={Uri.EscapeDataString(passcode ?? string.Empty)}", request, cancellationToken);

    /// <summary>Captures a card against the submitted operative. Null on a rejected link or missing details.</summary>
    public async Task<OnboardingViewDto?> CaptureCardAsync(string token, string? passcode, CaptureOnboardingCardRequest request, CancellationToken cancellationToken = default) =>
        await PostOrNullAsync($"api/onboarding/cards?token={Uri.EscapeDataString(token)}&passcode={Uri.EscapeDataString(passcode ?? string.Empty)}", request, cancellationToken);

    private async Task<OnboardingViewDto?> GetOrNullAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OnboardingViewDto>(cancellationToken);
    }

    private async Task<OnboardingViewDto?> PostOrNullAsync<TBody>(string url, TBody body, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(url, body, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorBody>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "Those details could not be accepted.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OnboardingViewDto>(cancellationToken);
    }

    private sealed record ErrorBody(string Error);
}
