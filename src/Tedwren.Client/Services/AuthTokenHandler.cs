using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace Tedwren.Client.Services;

/// <summary>
/// Attaches the bearer token (when signed in) to every API request, and redirects to <c>/login</c> when the
/// API answers 401 (expired/invalid token). Depends only on the leaf <see cref="ITokenStore"/> to avoid a DI
/// cycle with the <see cref="HttpClient"/> it wraps.
/// </summary>
public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly ITokenStore _tokens;
    private readonly NavigationManager _nav;

    /// <summary>Creates the handler over the token store and navigation.</summary>
    public AuthTokenHandler(ITokenStore tokens, NavigationManager nav)
    {
        _tokens = tokens;
        _nav = nav;
    }

    /// <summary>Adds the bearer header and handles 401 by clearing the token and redirecting to login.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_tokens.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            request.RequestUri?.AbsolutePath.Contains("/api/auth", StringComparison.OrdinalIgnoreCase) != true)
        {
            _tokens.Token = null;
            _nav.NavigateTo("/login", forceLoad: false);
        }

        return response;
    }
}
