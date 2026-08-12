using System.Net;
using System.Text.Json;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Notifications;
using Tedwren.Application.Notifications.Email;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the Resend delivery provider (PRD-Phase 7): it POSTs the rendered, branded HTML to Resend's
/// <c>/emails</c> endpoint with the expected payload. Uses a hand-written fake <see cref="HttpMessageHandler"/>
/// (the repo's house style — no mocking library) so no real network call is made.
/// </summary>
public sealed class ResendEmailSenderTests
{
    /// <summary>Captures the outgoing request and returns a canned response, standing in for Resend.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(StatusCode) { Content = new StringContent("{\"id\":\"test\"}") };
        }
    }

    private static (ResendEmailSender Sender, CapturingHandler Handler) CreateSut(EmailOptions? options = null)
    {
        options ??= new EmailOptions
        {
            Provider = EmailProvider.Resend,
            ApiKey = "re_test_key",
            FromEmail = "notifications@tedwren.test",
            FromName = "Tedwren",
            PublicBaseUrl = "https://api.tedwren.test",
        };

        var handler = new CapturingHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/"),
        };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);

        var renderer = new EmailTemplateRenderer(options);
        return (new ResendEmailSender(http, renderer, options), handler);
    }

    [Fact]
    public async Task SendAsync_PostsToResendEmailsEndpoint_WithBearerAuth()
    {
        var (sender, handler) = CreateSut();

        await sender.SendAsync("worker@example.test", "Qualification expiry warning", "Your CSCS card expires soon.");

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.resend.com/emails", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("re_test_key", handler.Request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_SendsRenderedHtml_ToRecipient_FromConfiguredSender()
    {
        var (sender, handler) = CreateSut();

        await sender.SendAsync("worker@example.test", "Qualification expiry warning", "Your CSCS card expires soon.");

        using var doc = JsonDocument.Parse(handler.Body!);
        var root = doc.RootElement;

        Assert.Equal("Tedwren <notifications@tedwren.test>", root.GetProperty("from").GetString());
        Assert.Equal("worker@example.test", root.GetProperty("to")[0].GetString());
        Assert.Equal("Qualification expiry warning", root.GetProperty("subject").GetString());

        var html = root.GetProperty("html").GetString()!;
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("Your CSCS card expires soon.", html);
        Assert.Contains("Private &amp; Confidential", html);

        // No reply-to configured => the property is omitted.
        Assert.False(root.TryGetProperty("reply_to", out _));
    }

    [Fact]
    public async Task SendAsync_IncludesReplyTo_WhenConfigured()
    {
        var options = new EmailOptions
        {
            Provider = EmailProvider.Resend,
            ApiKey = "re_test_key",
            FromEmail = "notifications@tedwren.test",
            FromName = "Tedwren",
            ReplyToEmail = "support@tedwren.test",
        };
        var (sender, handler) = CreateSut(options);

        await sender.SendAsync("worker@example.test", "Subject", "Body");

        using var doc = JsonDocument.Parse(handler.Body!);
        Assert.Equal("support@tedwren.test", doc.RootElement.GetProperty("reply_to").GetString());
    }

    [Fact]
    public async Task SendAsync_Throws_OnNonSuccessResponse()
    {
        var (sender, handler) = CreateSut();
        handler.StatusCode = HttpStatusCode.Unauthorized;

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sender.SendAsync("worker@example.test", "Subject", "Body"));
    }
}
