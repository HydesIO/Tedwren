using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IQualificationService"/> backed by the Tedwren Web API over HTTP — used when the client is
/// configured with <c>DataSource=Api</c>. The UI injects the same interface as in mock mode.
/// </summary>
public sealed class ApiQualificationService : IQualificationService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiQualificationService(HttpClient http) => _http = http;

    /// <summary>Gets the qualification-type library from the API.</summary>
    public async Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<QualificationTypeDto>>("api/qualifications/types", cancellationToken)
        ?? Array.Empty<QualificationTypeDto>();

    /// <summary>Gets a person's cards from the API.</summary>
    public async Task<IReadOnlyList<QualificationCardDto>> GetCardsForPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<QualificationCardDto>>(
            $"api/qualifications/people/{personId}/cards", cancellationToken)
        ?? Array.Empty<QualificationCardDto>();

    /// <summary>Captures a card via the API and returns its new id.</summary>
    public async Task<Guid> CaptureCardAsync(CaptureCardRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/qualifications/cards", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Confirms a card via the API.</summary>
    public async Task<bool> ConfirmCardAsync(ConfirmCardRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/qualifications/cards/{request.CardId}/confirm", new { request.ConfirmedBy }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Renews a card via the API, returning the new card id, or null when the source is not found.</summary>
    public async Task<Guid?> RenewCardAsync(RenewCardRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/qualifications/cards/{request.CardId}/renew",
            new { request.CardNumber, request.IssuedOn, request.ExpiresOn }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id;
    }

    /// <summary>Gets a person's competency shortfall for a trade from the API.</summary>
    public async Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, Guid? companyId = null, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<CompetencyShortfallDto>(
            $"api/qualifications/people/{personId}/shortfall?trade={Uri.EscapeDataString(trade)}", cancellationToken)
        ?? new CompetencyShortfallDto(personId, trade, Array.Empty<string>());

    /// <summary>Evaluates Gate 3 for a person + trade via the API.</summary>
    public async Task<Gate3StatusDto> EvaluateGate3Async(Guid personId, string trade, Guid? companyId = null, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<Gate3StatusDto>(
            $"api/qualifications/people/{personId}/gate3?trade={Uri.EscapeDataString(trade)}", cancellationToken)
        ?? new Gate3StatusDto(true, Array.Empty<Gate3RequirementDto>());

    /// <summary>Gets the manageable qualification-type library (shared + own-org) from the API.</summary>
    public async Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesForManagementAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<QualificationTypeDto>>("api/qualifications/types/manage", cancellationToken)
        ?? Array.Empty<QualificationTypeDto>();

    /// <summary>Adds a qualification type via the API and returns its new id.</summary>
    public async Task<Guid> CreateQualificationTypeAsync(CreateQualificationTypeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/qualifications/types", request, cancellationToken);
        await EnsureOrThrowAsync(response, cancellationToken);
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Updates a qualification type via the API.</summary>
    public async Task UpdateQualificationTypeAsync(Guid id, UpdateQualificationTypeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/qualifications/types/{id}", request, cancellationToken);
        await EnsureOrThrowAsync(response, cancellationToken);
    }

    /// <summary>Deletes a qualification type via the API (surfaces the server's reason when it is still referenced).</summary>
    public async Task DeleteQualificationTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"api/qualifications/types/{id}", cancellationToken);
        await EnsureOrThrowAsync(response, cancellationToken);
    }

    /// <summary>Gets the manageable trade→accreditation map (shared + own-org) from the API.</summary>
    public async Task<IReadOnlyList<TradeQualificationRequirementDto>> GetTradeRequirementsAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<TradeQualificationRequirementDto>>("api/qualifications/requirements", cancellationToken)
        ?? Array.Empty<TradeQualificationRequirementDto>();

    /// <summary>Adds a trade→accreditation map row via the API and returns its new id.</summary>
    public async Task<Guid> CreateTradeRequirementAsync(CreateTradeRequirementRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/qualifications/requirements", request, cancellationToken);
        await EnsureOrThrowAsync(response, cancellationToken);
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Updates a trade→accreditation map row's flags via the API.</summary>
    public async Task UpdateTradeRequirementAsync(Guid id, UpdateTradeRequirementRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/qualifications/requirements/{id}", request, cancellationToken);
        await EnsureOrThrowAsync(response, cancellationToken);
    }

    /// <summary>Deletes a trade→accreditation map row via the API.</summary>
    public async Task DeleteTradeRequirementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"api/qualifications/requirements/{id}", cancellationToken);
        await EnsureOrThrowAsync(response, cancellationToken);
    }

    /// <summary>Throws with the server's <c>{ error }</c> message when the response is not a success, so dialogs can show why (e.g. a guarded delete or a shared-row write refused).</summary>
    private static async Task EnsureOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? message = null;
        try
        {
            message = (await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken))?.Error;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or HttpRequestException or NotSupportedException)
        {
            // No JSON body — fall back to the status reason below.
        }

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? $"The request failed ({(int)response.StatusCode})." : message);
    }

    /// <summary>Shape of the create/renew response body.</summary>
    private sealed record CreatedResponse(Guid Id);

    /// <summary>Shape of the API's <c>{ error }</c> problem body.</summary>
    private sealed record ErrorResponse(string Error);
}
