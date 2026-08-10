using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IAttendanceService"/> backed by the Tedwren Web API over HTTP (SF-13–SF-19) — used when the
/// client is configured with <c>DataSource=Api</c> (the standard mode). The UI injects the same interface.
/// </summary>
public sealed class ApiAttendanceService : IAttendanceService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiAttendanceService(HttpClient http) => _http = http;

    /// <summary>Records a sign-in attempt via the API.</summary>
    public async Task<SignInResult> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/attendance/sign-in", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignInResult>(cancellationToken))!;
    }

    /// <summary>Records a sign-out attempt via the API.</summary>
    public async Task<SignOutResult> SignOutAsync(SignOutRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/attendance/sign-out", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignOutResult>(cancellationToken))!;
    }

    /// <summary>Gets the workers currently present on a site.</summary>
    public async Task<IReadOnlyList<OnSiteWorkerDto>> GetOnSiteAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<OnSiteWorkerDto>>($"api/attendance/on-site/{siteId}", cancellationToken)
        ?? Array.Empty<OnSiteWorkerDto>();

    /// <summary>Gets the recent attendance records for a site (the append-only log).</summary>
    public async Task<IReadOnlyList<AttendanceRecordDto>> GetSiteRecordsAsync(Guid siteId, int take, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<AttendanceRecordDto>>($"api/attendance/sites/{siteId}/records", cancellationToken)
        ?? Array.Empty<AttendanceRecordDto>();
}
