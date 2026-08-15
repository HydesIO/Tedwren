using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Affiliates;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IAffiliateService"/> backed by the Tedwren Web API. The console uses the platform-admin
/// affiliate/payout calls (bearer-token authorised); the anonymous agreement view/sign/pdf calls back the
/// public signing page.
/// </summary>
public sealed class ApiAffiliateService : IAffiliateService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiAffiliateService(HttpClient http) => _http = http;

    /// <summary>Returns every affiliate, newest-updated first.</summary>
    public async Task<IReadOnlyList<AffiliateDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _http.GetFromJsonAsync<IReadOnlyList<AffiliateDto>>("api/affiliates", cancellationToken)) ?? Array.Empty<AffiliateDto>();

    /// <summary>Returns an affiliate with its associated accounts, payouts and agreement, or null.</summary>
    public async Task<AffiliateDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await GetOrNull<AffiliateDetailDto>($"api/affiliates/{id}", cancellationToken);

    /// <summary>Creates an affiliate (drafts the agreement and emails the setup link server-side).</summary>
    public async Task<AffiliateDto> CreateAsync(CreateAffiliateRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/affiliates", request, cancellationToken);
        await ThrowIfBadRequest(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AffiliateDto>(cancellationToken))!;
    }

    /// <summary>Updates an affiliate's details and commission plan. Null when it doesn't exist.</summary>
    public async Task<AffiliateDto?> UpdateAsync(Guid id, UpdateAffiliateRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"api/affiliates/{id}") { Content = JsonContent.Create(request) };
        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfBadRequest(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AffiliateDto>(cancellationToken);
    }

    /// <summary>Raises a payout for an affiliate. Null when the affiliate doesn't exist.</summary>
    public async Task<AffiliatePayoutDto?> CreatePayoutAsync(Guid affiliateId, CreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/affiliates/{affiliateId}/payouts", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AffiliatePayoutDto>(cancellationToken);
    }

    /// <summary>Marks a payout paid. Null when it doesn't exist.</summary>
    public async Task<AffiliatePayoutDto?> MarkPayoutPaidAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/affiliates/payouts/{payoutId}/paid", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AffiliatePayoutDto>(cancellationToken);
    }

    /// <summary>Returns the public, token-gated view of an agreement, or null.</summary>
    public async Task<AffiliateAgreementViewDto?> GetAgreementAsync(string token, CancellationToken cancellationToken = default) =>
        await GetOrNull<AffiliateAgreementViewDto>($"api/affiliate-agreements/{Uri.EscapeDataString(token)}", cancellationToken);

    /// <summary>Signs an agreement. Null when the token is unknown or already signed.</summary>
    public async Task<AffiliateAgreementViewDto?> SignAgreementAsync(string token, SignAffiliateAgreementRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/affiliate-agreements/{Uri.EscapeDataString(token)}/sign", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfBadRequest(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AffiliateAgreementViewDto>(cancellationToken);
    }

    /// <summary>Returns the signed agreement PDF bytes for a token, or null.</summary>
    public async Task<byte[]?> GetAgreementPdfAsync(string token, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/affiliate-agreements/{Uri.EscapeDataString(token)}/pdf", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<T?> GetOrNull<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private static async Task ThrowIfBadRequest(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorBody>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "That request could not be accepted.");
        }
    }

    private sealed record ErrorBody(string Error);
}
