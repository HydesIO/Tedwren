using System.Net;
using System.Net.Http.Headers;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches the operative access token to each request and, on a 401,
/// silently refreshes the token once (no biometric prompt) and retries. Ported from the console
/// <c>AuthTokenHandler</c>; the refresh half is <see cref="ISessionRefresher"/> instead of a redirect. It never
/// tries to refresh the <c>/api/mobile/auth/*</c> endpoints themselves (avoids a refresh loop).
/// </summary>
public sealed class OperativeAuthMessageHandler : DelegatingHandler
{
    private readonly AccessTokenStore _store;
    private readonly ISessionRefresher _refresher;

    /// <summary>Creates the handler over the access-token store and the silent refresher.</summary>
    public OperativeAuthMessageHandler(AccessTokenStore store, ISessionRefresher refresher)
    {
        _store = store;
        _refresher = refresher;
    }

    /// <summary>Attaches the bearer token; on a 401 for a non-auth path, refreshes once and retries.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Attach(request, _store.AccessToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || IsAuthPath(request))
        {
            return response;
        }

        var newToken = await _refresher.RefreshAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(newToken))
        {
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

    private static bool IsAuthPath(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.Contains("/api/mobile/auth/", StringComparison.OrdinalIgnoreCase) == true;

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
