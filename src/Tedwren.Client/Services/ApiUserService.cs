using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IUserService"/> backed by the Tedwren Web API over HTTP — used when the client is configured
/// with <c>DataSource=Api</c>. The UI injects the same interface as in mock mode.
/// </summary>
public sealed class ApiUserService : IUserService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiUserService(HttpClient http) => _http = http;

    /// <summary>Gets the user list from the API.</summary>
    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<UserDto>>("api/users", cancellationToken)
        ?? Array.Empty<UserDto>();

    /// <summary>Gets a user by id, or null on 404.</summary>
    public async Task<UserDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/users/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
    }

    /// <summary>Gets the selectable roles from the API.</summary>
    public async Task<IReadOnlyList<RoleOption>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<RoleOption>>("api/users/roles", cancellationToken)
        ?? Array.Empty<RoleOption>();

    /// <summary>Invites a user via the API and returns the new id + accept token. Surfaces a duplicate/validation error.</summary>
    public async Task<InviteUserResult> InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/users", request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The user could not be invited.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InviteUserResult>(cancellationToken)
            ?? throw new InvalidOperationException("The user could not be invited.");
    }

    /// <summary>Updates a user's name and role via the API, or null when not found.</summary>
    public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/users/{id}", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
    }

    /// <summary>Suspends a user via the API, or null when not found.</summary>
    public Task<UserDto?> SuspendUserAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostStatusAsync($"api/users/{id}/suspend", cancellationToken);

    /// <summary>Reactivates a user via the API, or null when not found.</summary>
    public Task<UserDto?> ReactivateUserAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostStatusAsync($"api/users/{id}/reactivate", cancellationToken);

    /// <summary>Posts a status-change action and returns the updated user, or null on 404.</summary>
    private async Task<UserDto?> PostStatusAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsync(url, content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
    }

    /// <summary>Shape of the create response body.</summary>
    private sealed record CreatedResponse(Guid Id);

    /// <summary>Shape of an error response body.</summary>
    private sealed record ErrorResponse(string Error);
}
