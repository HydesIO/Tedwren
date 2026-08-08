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
    public async Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<CompetencyShortfallDto>(
            $"api/qualifications/people/{personId}/shortfall?trade={Uri.EscapeDataString(trade)}", cancellationToken)
        ?? new CompetencyShortfallDto(personId, trade, Array.Empty<string>());

    /// <summary>Shape of the create/renew response body.</summary>
    private sealed record CreatedResponse(Guid Id);
}
