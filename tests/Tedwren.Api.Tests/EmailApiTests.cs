using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Notifications;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the email-template endpoints hosted in the API: the admin test-send delivers a
/// branded sample (captured by the outbox stub in the test host) and validates its input.
/// </summary>
public sealed class EmailApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so outbox state does not leak between tests.</summary>
    public EmailApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // the test-send endpoint actually pushes a branded sample through IEmailSender
    public async Task TestSend_DeliversSampleToOutbox()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/email-templates/test-send", new { toEmail = "ops@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var outbox = await client.GetFromJsonAsync<List<OutboxMessage>>("/api/jobs/outbox");
        Assert.Contains(outbox!, m => m.Channel == "Email" && m.Recipient == "ops@example.com");
    }

    [Fact]
    public async Task TestSend_InvalidAddress_ReturnsBadRequest()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/email-templates/test-send", new { toEmail = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
