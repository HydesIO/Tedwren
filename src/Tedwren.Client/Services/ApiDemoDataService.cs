using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.DemoData;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IDemoDataService"/> backed by the Web API's platform-admin demo-data surface
/// (<c>/api/admin/demo-data</c>, gated by the <c>PlatformAdmin</c> policy). Used by the Product Admin portal's
/// demo-data page to seed/recreate/delete the demonstration dataset and to poll progress while it runs.
/// </summary>
public sealed class ApiDemoDataService : IDemoDataService
{
    private static readonly DemoDataProgress Idle = new(false, null, null, 0, 0, 0, null);

    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiDemoDataService(HttpClient http) => _http = http;

    /// <inheritdoc />
    public async Task<DemoDataStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<DemoDataStatus>("api/admin/demo-data/status", cancellationToken)
        ?? new DemoDataStatus(false, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Synchronous progress access is not available over HTTP; use <see cref="GetProgressAsync"/> to poll.
    /// Returns an idle snapshot so callers that ignore live progress still work.
    /// </summary>
    public DemoDataProgress GetProgress() => Idle;

    /// <summary>Polls the server for the live progress of a running seed/delete operation.</summary>
    public async Task<DemoDataProgress> GetProgressAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<DemoDataProgress>("api/admin/demo-data/progress", cancellationToken) ?? Idle;

    /// <inheritdoc />
    public async Task<DemoDataStatus> SeedAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync("api/admin/demo-data/seed", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DemoDataStatus>(cancellationToken)
            ?? new DemoDataStatus(false, 0, 0, 0, 0, 0, 0, 0);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync("api/admin/demo-data/delete", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
