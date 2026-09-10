using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Account;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IProfileService"/> backed by the Tedwren Web API's authenticated self-service endpoints
/// (<c>/api/me/*</c>). The caller is resolved server-side from the bearer token, so no user id is ever sent —
/// the UI only ever acts on its own account (R15).
/// </summary>
public sealed class ApiProfileService : IProfileService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiProfileService(HttpClient http) => _http = http;

    /// <summary>Gets the caller's own profile from the API, or null when not found.</summary>
    public async Task<MyProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/me/profile", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MyProfileDto>(cancellationToken);
    }

    /// <summary>Updates the caller's own details via the API. Surfaces a validation error as an exception.</summary>
    public async Task<MyProfileDto?> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync("api/me/profile", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfBadRequestAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MyProfileDto>(cancellationToken);
    }

    /// <summary>Changes the caller's own password via the API. False when the current password is rejected.</summary>
    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/me/password", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The password could not be changed.");
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <summary>Uploads a new avatar image via the API. Surfaces a validation error as an exception.</summary>
    public async Task<MyProfileDto?> UpdateAvatarAsync(UpdateAvatarRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/me/avatar", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowIfBadRequestAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MyProfileDto>(cancellationToken);
    }

    /// <summary>Turns a 400 response carrying an <c>{ error }</c> body into an <see cref="InvalidOperationException"/>.</summary>
    private static async Task ThrowIfBadRequestAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            return;
        }

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        throw new InvalidOperationException(error?.Error ?? "The request was invalid.");
    }

    /// <summary>Shape of an error response body.</summary>
    private sealed record ErrorResponse(string Error);
}
