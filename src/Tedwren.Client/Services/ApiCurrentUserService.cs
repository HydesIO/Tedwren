using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ICurrentUserService"/> backed by the Tedwren Web API (<c>/api/me</c>). Supplies the signed-in
/// operator's name and role to the shell and permission checks.
/// </summary>
public sealed class ApiCurrentUserService : ICurrentUserService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiCurrentUserService(HttpClient http) => _http = http;

    /// <summary>Gets the current operator's identity and role from the API.</summary>
    public async Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<CurrentUserDto>("api/me", cancellationToken)
        ?? new CurrentUserDto("Unknown", "Viewer", CompanyId: null);
}
