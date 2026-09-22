using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Subcontractors;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ISubcontractorOnboardingService"/> backed by the Tedwren Web API
/// (<c>/api/subcontractor-onboarding</c>): the main-contractor set-up & configuration flow (spec Stage 1 / §4).
/// </summary>
public sealed class ApiSubcontractorOnboardingService : ISubcontractorOnboardingService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiSubcontractorOnboardingService(HttpClient http) => _http = http;

    /// <summary>Sets up & configures a subcontractor, returning the shareable onboarding link and the created ids.</summary>
    public async Task<SubcontractorOnboardingResultDto> SetupAsync(SetupSubcontractorRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/subcontractor-onboarding", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubcontractorOnboardingResultDto>(cancellationToken))!;
    }

    /// <summary>Returns the stored configuration for a subcontractor company, or null when none exists.</summary>
    public async Task<SubcontractorOnboardingConfigDto?> GetBySubcontractorAsync(Guid subcontractorCompanyId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/subcontractor-onboarding/{subcontractorCompanyId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SubcontractorOnboardingConfigDto>(cancellationToken);
    }

    /// <summary>Returns the subcontractor's Gate 1 status (whether every required-before-work document is present and valid).</summary>
    public async Task<Gate1StatusDto> EvaluateGate1Async(Guid subcontractorCompanyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<Gate1StatusDto>($"api/subcontractor-onboarding/{subcontractorCompanyId}/gate1", cancellationToken)
        ?? new Gate1StatusDto(false, Array.Empty<Gate1RequirementDto>());

    /// <summary>Returns the caller's subcontractors whose live RAMS is due for re-review as of <paramref name="asOf"/>.</summary>
    public async Task<IReadOnlyList<RamsReviewDueDto>> GetSubcontractorsDueForRamsReviewAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<RamsReviewDueDto>>(
            $"api/subcontractor-onboarding/rams-review-due?asOf={Uri.EscapeDataString(asOf.ToString("o"))}", cancellationToken)
        ?? Array.Empty<RamsReviewDueDto>();
}
