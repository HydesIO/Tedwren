using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Billing;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IBillingService"/> backed by the Tedwren Web API's platform-admin billing surface
/// (<c>/api/admin/billing</c>), which is gated by the <c>PlatformAdmin</c> policy. Collection actions surface
/// the server's error message (e.g. "GoCardless is not configured", "no active mandate") as an
/// <see cref="InvalidOperationException"/> so the admin pages can show it.
/// </summary>
public sealed class ApiBillingService : IBillingService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiBillingService(HttpClient http) => _http = http;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MandateDto>> GetMandatesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<MandateDto>>("api/admin/billing/mandates", cancellationToken)
        ?? Array.Empty<MandateDto>();

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<PaymentDto>>("api/admin/billing/payments", cancellationToken)
        ?? Array.Empty<PaymentDto>();

    /// <inheritdoc />
    public async Task<CompanyBillingOverviewDto> GetCompanyBillingAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<CompanyBillingOverviewDto>($"api/admin/billing/companies/{companyId}/overview", cancellationToken)
        ?? new CompanyBillingOverviewDto(companyId, null, null, Array.Empty<PaymentDto>());

    /// <inheritdoc />
    public async Task<MandateSetupResultDto> StartMandateSetupAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/admin/billing/companies/{companyId}/mandate/setup", content: null, cancellationToken);
        await ThrowIfActionFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MandateSetupResultDto>(cancellationToken)
            ?? throw new InvalidOperationException("Mandate set-up did not return an authorisation URL.");
    }

    /// <inheritdoc />
    public async Task<bool> CancelMandateAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/admin/billing/companies/{companyId}/mandate/cancel", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await ThrowIfActionFailedAsync(response, cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<PaymentDto> TakePaymentAsync(Guid companyId, TakePaymentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/admin/billing/companies/{companyId}/payments", request, cancellationToken);
        await ThrowIfActionFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PaymentDto>(cancellationToken)
            ?? throw new InvalidOperationException("The payment could not be created.");
    }

    /// <inheritdoc />
    public async Task<PaymentDto?> RetryPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/admin/billing/payments/{paymentId}/retry", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfActionFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PaymentDto>(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionDto> SetSubscriptionAsync(Guid companyId, SetSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/admin/billing/companies/{companyId}/subscription", request, cancellationToken);
        await ThrowIfActionFailedAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<SubscriptionDto>(cancellationToken)
            ?? throw new InvalidOperationException("The subscription could not be saved.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebhookEventDto>> GetWebhookEventsAsync(int limit = 100, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<WebhookEventDto>>("api/admin/billing/events", cancellationToken)
        ?? Array.Empty<WebhookEventDto>();

    /// <summary>Turns a non-success collection response into an <see cref="InvalidOperationException"/> carrying the server message.</summary>
    private static async Task ThrowIfActionFailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? message = null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
            message = error?.Error ?? error?.Detail;
        }
        catch
        {
            // Body was not the expected JSON shape; fall back to a status-based message below.
        }

        throw new InvalidOperationException(message ?? $"The billing action failed ({(int)response.StatusCode}).");
    }

    /// <summary>Shape of an error body: either an <c>error</c> field or a ProblemDetails <c>detail</c>.</summary>
    private sealed record ErrorResponse(string? Error, string? Detail);
}
