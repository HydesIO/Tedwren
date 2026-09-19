using System.Net;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Builds an <see cref="HttpClient"/> whose responses are canned, so API clients can be tested without a server.</summary>
internal static class FakeHttp
{
    /// <summary>An HttpClient that answers every request with the given status and content.</summary>
    public static HttpClient Returning(HttpStatusCode status, HttpContent content)
        => new(new StubHandler(status, content)) { BaseAddress = new Uri("https://api.test/") };

    /// <summary>An HttpClient that answers each request via the supplied responder (keyed on the request), for multi-call flows.</summary>
    public static HttpClient Routed(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new RoutedHandler(responder)) { BaseAddress = new Uri("https://api.test/") };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly HttpContent _content;

        public StubHandler(HttpStatusCode status, HttpContent content)
        {
            _status = status;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(_status) { Content = _content });
    }

    private sealed class RoutedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RoutedHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
