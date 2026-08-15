using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tedwren.Application.Billing;

/// <summary>
/// Real <see cref="IGoCardlessClient"/> over the GoCardless REST API, registered as a typed <c>HttpClient</c>
/// whose base address, Bearer token and <c>GoCardless-Version</c> header are configured by the API composition
/// root from <c>GoCardlessOptions</c>. Each GoCardless resource is wrapped under a top-level key
/// (<c>billing_requests</c>, <c>mandates</c>, <c>payments</c>); the private records below model exactly the
/// fields used. Mirrors <c>ResendEmailSender</c> in shape.
/// </summary>
public sealed class GoCardlessClient : IGoCardlessClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;

    /// <summary>Creates the client over the configured typed <see cref="HttpClient"/>.</summary>
    public GoCardlessClient(HttpClient http) => _http = http;

    /// <summary>Creates a billing request (mandate_request) then a billing-request flow, returning the hosted URL.</summary>
    public async Task<GoCardlessMandateSetup> StartMandateSetupAsync(GoCardlessMandateSetupRequest request, CancellationToken cancellationToken = default)
    {
        var brRequest = new BillingRequestEnvelope(new BillingRequestBody(
            new MandateRequest(request.Currency),
            new Dictionary<string, string> { ["company_id"] = request.CompanyId }));
        var br = await PostAsync<BillingRequestEnvelope, BillingRequestEnvelope>("billing_requests", brRequest, idempotencyKey: null, cancellationToken);
        var billingRequestId = br?.BillingRequests?.Id
            ?? throw new InvalidOperationException("GoCardless did not return a billing request id.");

        var flowRequest = new BillingRequestFlowEnvelope(new BillingRequestFlowBody(
            request.RedirectUri, request.ExitUri, new BillingRequestFlowLinks(billingRequestId)));
        var flow = await PostAsync<BillingRequestFlowEnvelope, BillingRequestFlowEnvelope>("billing_request_flows", flowRequest, idempotencyKey: null, cancellationToken);
        var authorisationUrl = flow?.BillingRequestFlows?.AuthorisationUrl
            ?? throw new InvalidOperationException("GoCardless did not return an authorisation URL.");

        return new GoCardlessMandateSetup(billingRequestId, authorisationUrl);
    }

    /// <summary>Fetches a mandate, or null on 404.</summary>
    public async Task<GoCardlessMandate?> GetMandateAsync(string mandateId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"mandates/{mandateId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<MandateEnvelope>(JsonOptions, cancellationToken);
        return Map(envelope?.Mandates);
    }

    /// <summary>Cancels a mandate and returns its updated state.</summary>
    public async Task<GoCardlessMandate> CancelMandateAsync(string mandateId, CancellationToken cancellationToken = default)
    {
        var envelope = await PostAsync<object, MandateEnvelope>($"mandates/{mandateId}/actions/cancel", new { }, idempotencyKey: null, cancellationToken);
        return Map(envelope?.Mandates) ?? throw new InvalidOperationException("GoCardless did not return the cancelled mandate.");
    }

    /// <summary>Creates a one-off payment against a mandate (idempotent).</summary>
    public async Task<GoCardlessPayment> CreatePaymentAsync(GoCardlessPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var body = new PaymentRequestEnvelope(new PaymentBody(
            request.Amount, request.Currency, request.Description,
            new PaymentLinks(request.MandateId),
            new Dictionary<string, string> { ["company_id"] = request.CompanyId }));
        var envelope = await PostAsync<PaymentRequestEnvelope, PaymentEnvelope>("payments", body, request.IdempotencyKey, cancellationToken);
        return Map(envelope?.Payments) ?? throw new InvalidOperationException("GoCardless did not return the created payment.");
    }

    /// <summary>Fetches a payment, or null on 404.</summary>
    public async Task<GoCardlessPayment?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"payments/{paymentId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<PaymentEnvelope>(JsonOptions, cancellationToken);
        return Map(envelope?.Payments);
    }

    /// <summary>Retries a failed payment and returns its updated state.</summary>
    public async Task<GoCardlessPayment> RetryPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        var envelope = await PostAsync<object, PaymentEnvelope>($"payments/{paymentId}/actions/retry", new { }, idempotencyKey: null, cancellationToken);
        return Map(envelope?.Payments) ?? throw new InvalidOperationException("GoCardless did not return the retried payment.");
    }

    /// <summary>POSTs a JSON body (optionally idempotent) and deserialises the response, throwing on non-success.</summary>
    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, string? idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
    }

    /// <summary>Maps a GoCardless mandate resource to the transport-neutral record.</summary>
    private static GoCardlessMandate? Map(MandateResource? m) =>
        m is null ? null : new GoCardlessMandate(m.Id, m.Status, m.Reference, m.Links?.Customer);

    /// <summary>Maps a GoCardless payment resource to the transport-neutral record.</summary>
    private static GoCardlessPayment? Map(PaymentResource? p) =>
        p is null ? null : new GoCardlessPayment(
            p.Id, p.Status, p.Amount, p.Currency,
            DateOnly.TryParse(p.ChargeDate, out var d) ? d : null, p.Description);

    // ----- GoCardless JSON shapes (only the fields used) -----

    private sealed record BillingRequestEnvelope(
        [property: JsonPropertyName("billing_requests")] BillingRequestBody BillingRequests);

    private sealed record BillingRequestBody(
        [property: JsonPropertyName("mandate_request")] MandateRequest MandateRequest,
        [property: JsonPropertyName("metadata")] Dictionary<string, string>? Metadata,
        [property: JsonPropertyName("id")] string? Id = null);

    private sealed record MandateRequest([property: JsonPropertyName("currency")] string Currency);

    private sealed record BillingRequestFlowEnvelope(
        [property: JsonPropertyName("billing_request_flows")] BillingRequestFlowBody BillingRequestFlows);

    private sealed record BillingRequestFlowBody(
        [property: JsonPropertyName("redirect_uri")] string RedirectUri,
        [property: JsonPropertyName("exit_uri")] string ExitUri,
        [property: JsonPropertyName("links")] BillingRequestFlowLinks Links,
        [property: JsonPropertyName("authorisation_url")] string? AuthorisationUrl = null,
        [property: JsonPropertyName("id")] string? Id = null);

    private sealed record BillingRequestFlowLinks(
        [property: JsonPropertyName("billing_request")] string BillingRequest);

    private sealed record MandateEnvelope([property: JsonPropertyName("mandates")] MandateResource? Mandates);

    private sealed record MandateResource(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("reference")] string? Reference,
        [property: JsonPropertyName("links")] MandateResourceLinks? Links);

    private sealed record MandateResourceLinks([property: JsonPropertyName("customer")] string? Customer);

    private sealed record PaymentEnvelope([property: JsonPropertyName("payments")] PaymentResource? Payments);

    private sealed record PaymentRequestEnvelope([property: JsonPropertyName("payments")] PaymentBody Payments);

    private sealed record PaymentBody(
        [property: JsonPropertyName("amount")] int Amount,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("links")] PaymentLinks Links,
        [property: JsonPropertyName("metadata")] Dictionary<string, string>? Metadata);

    private sealed record PaymentLinks([property: JsonPropertyName("mandate")] string Mandate);

    private sealed record PaymentResource(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("amount")] int Amount,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("charge_date")] string? ChargeDate,
        [property: JsonPropertyName("description")] string? Description);
}
