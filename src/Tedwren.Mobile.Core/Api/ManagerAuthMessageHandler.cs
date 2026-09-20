using System.Net;
using System.Net.Http.Headers;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches the manager's console access token to each request and, on a 401
/// (for a non-login path), <b>silently refreshes</b> the token once via <see cref="IManagerSessionRefresher"/> and
/// retries (M8). Only when the refresh fails does it clear the session and signal expiry
/// (<see cref="IManagerSessionExpiredHandler"/>), so the shell re-prompts. Mirrors the operative auth handler; the
/// login path (<c>/api/auth/*</c>) is exempt (a 401 there is a credential failure, not an expired session).
/// </summary>
public sealed class ManagerAuthMessageHandler : DelegatingHandler
{
    private readonly AccessTokenStore _store;
    private readonly IManagerSessionRefresher _refresher;
    private readonly IManagerSessionExpiredHandler _expired;

    /// <summary>Creates the handler over the access-token store, the silent refresher and the expiry sink.</summary>
    public ManagerAuthMessageHandler(AccessTokenStore store, IManagerSessionRefresher refresher, IManagerSessionExpiredHandler expired)
    {
        _store = store;
        _refresher = refresher;
        _expired = expired;
    }

    /// <summary>Attaches the bearer token; on a 401 for a non-login path, refreshes once and retries, else signals expiry.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Attach(request, _store.AccessToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || IsLoginPath(request))
        {
            return response;
        }

        var newToken = await _refresher.RefreshAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(newToken))
        {
            _expired.HandleUnauthorized();
            return response;
        }

        _store.AccessToken = newToken;
        response.Dispose();

        var retry = await CloneAsync(request, cancellationToken);
        Attach(retry, newToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    private static void Attach(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static bool IsLoginPath(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.Contains("/api/auth/", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Clones a request so it can be re-sent after a token refresh (a request can only be sent once).</summary>
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
