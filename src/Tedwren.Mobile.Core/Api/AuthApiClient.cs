using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Auth;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Calls the Tedwren Web API console authentication endpoint (email + password → JWT). Used by the app's
/// secondary "sign in as manager/admin" flow, which reuses the existing <c>/api/auth/login</c> surface. The
/// operative (mobile-number + OTP) flow is a separate client added in M2.
/// </summary>
public sealed class AuthApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> whose BaseAddress is the API root.</summary>
    public AuthApiClient(HttpClient http) => _http = http;

    /// <summary>
    /// Signs a console user (site manager / admin) in with email + password. Returns the auth result on
    /// success, or null when the credentials are rejected (HTTP 401). Any other non-success status throws
    /// <see cref="ApiException"/>.
    /// </summary>
    public async Task<AuthResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Sign-in failed ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<AuthResultDto>(cancellationToken);
    }
}
