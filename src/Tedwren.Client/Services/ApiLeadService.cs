using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Leads;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ILeadService"/> backed by the Tedwren Web API (<c>/api/leads</c>). The console uses the
/// platform-admin CRUD/status/notes/convert calls (bearer-token authorised); the anonymous capture call exists
/// for completeness but is normally made by the marketing site.
/// </summary>
public sealed class ApiLeadService : ILeadService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiLeadService(HttpClient http) => _http = http;

    /// <summary>Returns every lead, newest-updated first.</summary>
    public async Task<IReadOnlyList<LeadDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _http.GetFromJsonAsync<IReadOnlyList<LeadDto>>("api/leads", cancellationToken)) ?? Array.Empty<LeadDto>();

    /// <summary>Returns a lead with its activity log, or null.</summary>
    public async Task<LeadDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/leads/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LeadDetailDto>(cancellationToken);
    }

    /// <summary>Creates a lead and returns it.</summary>
    public async Task<LeadDto> CreateAsync(CreateLeadRequest request, string? actor, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/leads", request, cancellationToken);
        await ThrowIfBadRequest(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LeadDto>(cancellationToken))!;
    }

    /// <summary>Updates a lead's editable fields. Null when it doesn't exist.</summary>
    public async Task<LeadDto?> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken = default) =>
        await SendForLead(HttpMethod.Put, $"api/leads/{id}", request, cancellationToken);

    /// <summary>Moves a lead to a new stage. Null when it doesn't exist.</summary>
    public async Task<LeadDto?> UpdateStatusAsync(Guid id, UpdateLeadStatusRequest request, string? actor, CancellationToken cancellationToken = default) =>
        await SendForLead(HttpMethod.Post, $"api/leads/{id}/status", request, cancellationToken);

    /// <summary>Adds an activity-log note. Null when the lead doesn't exist.</summary>
    public async Task<LeadNoteDto?> AddNoteAsync(Guid id, AddLeadNoteRequest request, string? actor, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/leads/{id}/notes", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfBadRequest(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LeadNoteDto>(cancellationToken);
    }

    /// <summary>Converts a lead into an account. Null when it doesn't exist.</summary>
    public async Task<LeadDto?> ConvertAsync(Guid id, ConvertLeadRequest request, string? actor, CancellationToken cancellationToken = default) =>
        await SendForLead(HttpMethod.Post, $"api/leads/{id}/convert", request, cancellationToken);

    /// <summary>Creates a lead from an anonymous marketing-site capture.</summary>
    public async Task<LeadDto> CaptureAsync(CaptureLeadRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/leads/capture", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LeadDto>(cancellationToken))!;
    }

    private async Task<LeadDto?> SendForLead<TBody>(HttpMethod method, string url, TBody body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfBadRequest(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LeadDto>(cancellationToken);
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
