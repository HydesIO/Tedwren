using System.Net;
using System.Net.Http.Headers;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches the manager's console access token to each request. Unlike the
/// operative handler there is <b>no silent refresh</b> — the console token has no refresh endpoint — so on a 401
/// (for a non-login path) it clears the session and signals that re-authentication is required, then surfaces the
/// 401 to the caller. The login path (<c>/api/auth/*</c>) is exempt: a 401 there is a credential failure handled
/// by the sign-in flow, not an expired session.
/// </summary>
public sealed class ManagerAuthMessageHandler : DelegatingHandler
{
    private readonly AccessTokenStore _store;
    private readonly IManagerSessionExpiredHandler _expired;

    /// <summary>Creates the handler over the access-token store and the session-expiry sink.</summary>
    public ManagerAuthMessageHandler(AccessTokenStore store, IManagerSessionExpiredHandler expired)
    {
        _store = store;
        _expired = expired;
    }

    /// <summary>Attaches the bearer token; on a 401 for a non-login path, clears the session and surfaces the 401.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _store.AccessToken;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized && !IsLoginPath(request))
        {
            _expired.HandleUnauthorized();
        }

        return response;
    }

    private static bool IsLoginPath(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.Contains("/api/auth/", StringComparison.OrdinalIgnoreCase) == true;
}
