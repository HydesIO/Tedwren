using System.Net;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Notifications;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for <see cref="TwilioSmsSender"/> — that it POSTs to Twilio's Messages endpoint with the correct
/// form body and surfaces transport failures. No network: an in-test <see cref="HttpMessageHandler"/> captures the
/// request. Basic-auth is set on the client by the composition root, so it is out of the sender's scope here.
/// </summary>
public sealed class TwilioSmsSenderTests
{
    /// <summary>Captures the outgoing request and returns a configurable status code.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public CapturingHandler(HttpStatusCode status) => _status = status;
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status);
        }
    }

    private static (TwilioSmsSender Sender, CapturingHandler Handler) CreateSut(HttpStatusCode status = HttpStatusCode.Created)
    {
        var handler = new CapturingHandler(status);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.twilio.com/") };
        var options = new SmsOptions
        {
            Provider = SmsProvider.Twilio,
            AccountSid = "AC123",
            AuthToken = "secret-token",
            FromNumber = "+15550001111",
        };
        return (new TwilioSmsSender(http, options), handler);
    }

    [Fact]
    public async Task SendAsync_PostsToMessagesEndpoint_WithFormBody()
    {
        var (sender, handler) = CreateSut();

        await sender.SendAsync("+447700900321", "Your CSCS card expires soon");

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Contains("Accounts/AC123/Messages.json", handler.Request.RequestUri!.ToString());
        Assert.Contains("To=%2B447700900321", handler.Body);          // '+' is percent-encoded
        Assert.Contains("From=%2B15550001111", handler.Body);
        Assert.Contains("Body=Your+CSCS+card+expires+soon", handler.Body);
    }

    [Fact]
    public async Task SendAsync_ThrowsOnNonSuccessResponse()
    {
        var (sender, _) = CreateSut(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<HttpRequestException>(() => sender.SendAsync("+447700900321", "hello"));
    }
}
