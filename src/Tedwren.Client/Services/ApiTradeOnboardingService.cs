using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Trades;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ITradeOnboardingService"/> backed by the Tedwren Web API (<c>/api/trades</c>, UAT-023). The invite
/// and review calls carry the manager's bearer token; the recipient view/upload/submit calls are anonymous,
/// token+passcode gated.
/// </summary>
public sealed class ApiTradeOnboardingService : ITradeOnboardingService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiTradeOnboardingService(HttpClient http) => _http = http;

    /// <summary>Invites a trade (creates the trade company + link) and returns the token + optional passcode.</summary>
    public async Task<TradeInviteLinkDto> InviteTradeAsync(CreateTradeInviteRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/trades/invites", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorBody>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The trade could not be invited.");
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TradeInviteLinkDto>(cancellationToken))!;
    }

    /// <summary>Returns the trade-facing view, or null when the link/passcode is rejected (403).</summary>
    public async Task<TradeInviteViewDto?> GetByTokenAsync(string token, string? passcode, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(
            $"api/trades/by-link/{Uri.EscapeDataString(token)}?passcode={Uri.EscapeDataString(passcode ?? string.Empty)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TradeInviteViewDto>(cancellationToken);
    }

    /// <summary>Uploads a document against the invited trade. Null on a rejected link.</summary>
    public async Task<TradeInviteViewDto?> SubmitDocumentAsync(string token, string? passcode, SubmitTradeDocumentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/trades/by-link/{Uri.EscapeDataString(token)}/documents?passcode={Uri.EscapeDataString(passcode ?? string.Empty)}", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TradeInviteViewDto>(cancellationToken);
    }

    /// <summary>Submits the trade's documents for review. Null on a rejected link; surfaces a not-submittable state as an exception.</summary>
    public async Task<TradeInviteViewDto?> SubmitForReviewAsync(string token, string? passcode, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync(
            $"api/trades/by-link/{Uri.EscapeDataString(token)}/submit?passcode={Uri.EscapeDataString(passcode ?? string.Empty)}", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException("This submission cannot be sent for review right now.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TradeInviteViewDto>(cancellationToken);
    }

    /// <summary>Adds an operative from the link once Gate 1 has cleared. Null on a rejected link; 409 (Gate 1 not cleared / SF-2) surfaces as an exception.</summary>
    public async Task<TradeInviteViewDto?> AddOperativeByLinkAsync(string token, string? passcode, AddTradeOperativeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/trades/by-link/{Uri.EscapeDataString(token)}/operatives?passcode={Uri.EscapeDataString(passcode ?? string.Empty)}", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorBody>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? error?.Reason ?? "The operative could not be added.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TradeInviteViewDto>(cancellationToken);
    }

    /// <summary>Lists the trade submissions the caller's tenant should review.</summary>
    public async Task<IReadOnlyList<TradeReviewItemDto>> GetReviewQueueAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<TradeReviewItemDto>>("api/trades/reviews", cancellationToken)
        ?? Array.Empty<TradeReviewItemDto>();

    /// <summary>Approves a submission. Null when not found/out of scope.</summary>
    public Task<TradeReviewItemDto?> ApproveAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default) =>
        DecideAsync($"api/trades/reviews/{inviteId}/approve", request, cancellationToken);

    /// <summary>Rejects a submission (a note is required). Null when not found/out of scope.</summary>
    public Task<TradeReviewItemDto?> RejectAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default) =>
        DecideAsync($"api/trades/reviews/{inviteId}/reject", request, cancellationToken);

    /// <summary>Returns a submission to the trade for changes (a note is required). Null when not found/out of scope.</summary>
    public Task<TradeReviewItemDto?> ReturnAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default) =>
        DecideAsync($"api/trades/reviews/{inviteId}/return", request, cancellationToken);

    /// <summary>Posts a review decision, mapping 404→null, 400→a validation exception (missing note), 409→a state exception.</summary>
    private async Task<TradeReviewItemDto?> DecideAsync(string url, ReviewTradeRequest request, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(url, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorBody>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? error?.Reason ?? "The decision could not be recorded.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TradeReviewItemDto>(cancellationToken);
    }

    /// <summary>Shape of an error body from the API (either an <c>error</c> or a <c>reason</c>).</summary>
    private sealed record ErrorBody(string? Error, string? Reason);
}
