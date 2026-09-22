using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Inductions;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the operative induction surface (<c>/api/mobile/inductions/*</c>, Subcontractor Onboarding
/// spec Stage 4 / Gate 4). The bearer token and silent refresh are applied by
/// <see cref="OperativeAuthMessageHandler"/> on the wrapped <see cref="HttpClient"/>; the quiz is scored on the
/// server (R5), so answers never leave it. <see cref="GetCurrentAsync"/> returns null when no induction is
/// assigned to the operative yet (404).
/// </summary>
public sealed class InductionApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over the auth-handled <see cref="HttpClient"/> (BaseAddress = API root).</summary>
    public InductionApiClient(HttpClient http) => _http = http;

    /// <summary>The operative's current induction session (resumed or started), or null when none is assigned.</summary>
    public async Task<InductionSessionDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/mobile/inductions/current", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InductionSessionDto>(cancellationToken);
    }

    /// <summary>Marks an induction step complete and returns the updated session, or null when the session is not found.</summary>
    public async Task<InductionSessionDto?> CompleteStepAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync(
            $"api/mobile/inductions/{sessionId}/steps/{Uri.EscapeDataString(stepId)}/complete", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InductionSessionDto>(cancellationToken);
    }

    /// <summary>Submits quiz answers for server-side scoring (R5) and returns the result, or null when the session is not found.</summary>
    public async Task<QuizResultDto?> SubmitQuizAsync(Guid sessionId, SubmitQuizRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/mobile/inductions/{sessionId}/quiz", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<QuizResultDto>(cancellationToken);
    }

    /// <summary>Finalises the induction (signature + optional consent). Returns the completed session, or null when not found/not ready.</summary>
    public async Task<InductionSessionDto?> FinalizeAsync(Guid sessionId, FinalizeInductionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/mobile/inductions/{sessionId}/finalize", request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InductionSessionDto>(cancellationToken);
    }
}
