using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IFormAssignmentService"/> backed by the Tedwren Web API over HTTP (the Forms Library, PRD-Phase 2).
/// Assigning, listing and removing assignments all go through the server, which scopes by tenant (R15).
/// </summary>
public sealed class ApiFormAssignmentService : IFormAssignmentService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiFormAssignmentService(HttpClient http) => _http = http;

    /// <summary>Lists the caller's assignments.</summary>
    public async Task<IReadOnlyList<FormAssignmentDto>> GetAssignmentsAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<FormAssignmentDto>>("api/forms/assignments", cancellationToken)
        ?? Array.Empty<FormAssignmentDto>();

    /// <summary>Creates an assignment and returns its id; surfaces a validation failure as an exception.</summary>
    public async Task<Guid> CreateAsync(CreateFormAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/forms/assignments", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<FormError>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The assignment could not be created.");
        }

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Created>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Removes an assignment. False when not found.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"api/forms/assignments/{id}", cancellationToken);
        return response.StatusCode != HttpStatusCode.NotFound && response.IsSuccessStatusCode;
    }

    private sealed record Created(Guid Id);
    private sealed record FormError(string Error);
}
