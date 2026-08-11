using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IInductionService"/> backed by the Tedwren Web API over HTTP — used when the client is configured
/// with <c>DataSource=Api</c>. The UI injects the same interface as in mock mode; quiz answers are scored on
/// the server (R5).
/// </summary>
public sealed class ApiInductionService : IInductionService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiInductionService(HttpClient http) => _http = http;

    /// <summary>Lists a company's templates (MC-3).</summary>
    public async Task<IReadOnlyList<InductionTemplateDto>> GetTemplatesAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<InductionTemplateDto>>($"api/inductions/templates/{companyId}", cancellationToken)
        ?? Array.Empty<InductionTemplateDto>();

    /// <summary>Creates a company induction template seeded from the default (MC-3/SF-12).</summary>
    public async Task<Guid> CreateDefaultTemplateAsync(CreateInductionTemplateRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/inductions/templates", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedTemplateResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Gets a template for authoring (MC-15), or null when not found.</summary>
    public async Task<InductionTemplateAuthoringDto?> GetTemplateForEditAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/inductions/templates/{templateId}/edit", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InductionTemplateAuthoringDto>(cancellationToken);
    }

    /// <summary>Updates a template's content and configuration (MC-15), or null when not found.</summary>
    public async Task<InductionTemplateAuthoringDto?> UpdateTemplateAsync(Guid templateId, UpdateInductionTemplateRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/inductions/templates/{templateId}", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<TemplateError>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The template could not be saved.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InductionTemplateAuthoringDto>(cancellationToken);
    }

    private sealed record TemplateError(string Error);

    /// <summary>Starts an induction (MC-1/MC-7).</summary>
    public async Task<InductionSessionDto> StartAsync(StartInductionRequest request, CancellationToken cancellationToken = default) =>
        (await (await _http.PostAsJsonAsync("api/inductions/sessions", request, cancellationToken))
            .Content.ReadFromJsonAsync<InductionSessionDto>(cancellationToken))!;

    /// <summary>Returns the device-facing session, or null (R5).</summary>
    public async Task<InductionSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<InductionSessionDto>($"api/inductions/sessions/{sessionId}", cancellationToken);

    /// <summary>Marks a step completed (MC-4).</summary>
    public Task<InductionSessionDto?> CompleteStepAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default) =>
        PostSessionAsync($"api/inductions/sessions/{sessionId}/steps/{Uri.EscapeDataString(stepId)}/complete", (object?)null, cancellationToken);

    /// <summary>Submits quiz answers for server-side scoring (R5).</summary>
    public async Task<QuizResultDto?> SubmitQuizAsync(Guid sessionId, SubmitQuizRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/inductions/sessions/{sessionId}/quiz", request, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<QuizResultDto>(cancellationToken) : null;
    }

    /// <summary>Finalises the induction (MC-5/MC-20); null when not found (a 409 not-ready surfaces as null too).</summary>
    public Task<InductionSessionDto?> FinalizeAsync(Guid sessionId, FinalizeInductionRequest request, CancellationToken cancellationToken = default) =>
        PostSessionAsync($"api/inductions/sessions/{sessionId}/finalize", request, cancellationToken);

    /// <summary>Resets a failed induction with a reason (MC-6).</summary>
    public Task<InductionSessionDto?> ResetAsync(Guid sessionId, ResetInductionRequest request, CancellationToken cancellationToken = default) =>
        PostSessionAsync($"api/inductions/sessions/{sessionId}/reset", request, cancellationToken);

    /// <summary>Lists a company's sessions for the console (MC-15).</summary>
    public async Task<IReadOnlyList<InductionSummaryDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<InductionSummaryDto>>($"api/inductions/company/{companyId}", cancellationToken)
        ?? Array.Empty<InductionSummaryDto>();

    /// <summary>Posts to a session route and reads back the updated session, or null on a non-success response.</summary>
    private async Task<InductionSessionDto?> PostSessionAsync(string url, object? body, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(url, body, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<InductionSessionDto>(cancellationToken) : null;
    }

    /// <summary>Shape of the create-template response body.</summary>
    private sealed record CreatedTemplateResponse(Guid Id);
}
