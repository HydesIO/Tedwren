using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the operative attendance actions (<c>/api/mobile/attendance/*</c>, M4). The bearer token and
/// silent refresh are handled by <see cref="OperativeAuthMessageHandler"/> on the wrapped <see cref="HttpClient"/>.
/// Sign-in/out are <b>online-only</b> (R2/R3) — the caller checks connectivity first and never queues these. A
/// recorded refusal comes back as a 200 with <see cref="SignInResult.SignedIn"/> false (SF-16); a non-success
/// status (e.g. a cross-company site, 403) throws.
/// </summary>
public sealed class AttendanceApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over the auth-handled <see cref="HttpClient"/> (BaseAddress = API root).</summary>
    public AttendanceApiClient(HttpClient http) => _http = http;

    /// <summary>Records a sign-in attempt and returns its outcome (accepted, flagged or a recorded refusal).</summary>
    public async Task<SignInResult> SignInAsync(MobileSignInRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/attendance/sign-in", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignInResult>(cancellationToken))!;
    }

    /// <summary>Records a sign-out attempt and returns its outcome, with the duration on site (SF-17).</summary>
    public async Task<SignOutResult> SignOutAsync(MobileSignOutRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/attendance/sign-out", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignOutResult>(cancellationToken))!;
    }

    /// <summary>The operative's current open sign-in, or null when they are not signed in anywhere (204, SF-18).</summary>
    public async Task<CurrentAttendanceDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/mobile/attendance/current", cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CurrentAttendanceDto>(cancellationToken);
    }
}
