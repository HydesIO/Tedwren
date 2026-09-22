using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Rams;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the operative RAMS surface (<c>/api/mobile/rams/*</c>, Subcontractor Onboarding spec Gate 5).
/// The bearer token and silent refresh are applied by <see cref="OperativeAuthMessageHandler"/> on the wrapped
/// <see cref="HttpClient"/>; the operative's PersonId is taken from that token on the server (R15), never sent in
/// the body. <see cref="GetLiveAsync"/> returns null when no live approved RAMS applies to the operative (204);
/// <see cref="SignAsync"/> returns null when the submission is no longer the current live version (409), so the
/// caller re-reads the live RAMS instead of signing a stale document.
/// </summary>
public sealed class RamsApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over the auth-handled <see cref="HttpClient"/> (BaseAddress = API root).</summary>
    public RamsApiClient(HttpClient http) => _http = http;

    /// <summary>The current live, approved RAMS the operative must read and sign, or null when none applies.</summary>
    public async Task<LiveRamsDto?> GetLiveAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/mobile/rams/live", cancellationToken);
        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LiveRamsDto>(cancellationToken);
    }

    /// <summary>Signs the live RAMS version (Gate 5). Returns the recorded acknowledgement, or null when the version is stale (409).</summary>
    public async Task<RamsAcknowledgementDto?> SignAsync(SignRamsRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/mobile/rams/sign", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RamsAcknowledgementDto>(cancellationToken);
    }
}
