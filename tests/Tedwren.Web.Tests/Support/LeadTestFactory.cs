using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tedwren.Web.Leads;

namespace Tedwren.Web.Tests.Support;

/// <summary>Captures routed leads in memory so tests can assert routing without a CRM or email.</summary>
public sealed class FakeLeadRouter : ILeadRouter
{
    /// <summary>Demo leads routed so far.</summary>
    public List<DemoLead> Demos { get; } = new();

    /// <summary>Contact messages routed so far.</summary>
    public List<ContactLead> Contacts { get; } = new();

    /// <inheritdoc />
    public Task RouteDemoAsync(DemoLead lead, CancellationToken cancellationToken = default)
    {
        Demos.Add(lead);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RouteContactAsync(ContactLead lead, CancellationToken cancellationToken = default)
    {
        Contacts.Add(lead);
        return Task.CompletedTask;
    }

    /// <summary>Clears captured leads between tests sharing the fixture.</summary>
    public void Reset()
    {
        Demos.Clear();
        Contacts.Clear();
    }
}

/// <summary>
/// Test host for the lead forms: replaces the real <see cref="ILeadRouter"/> with an in-memory fake so
/// routing decisions are inspectable, and exposes helpers for the antiforgery-protected POST flow.
/// </summary>
public sealed class LeadTestFactory : WebApplicationFactory<Program>
{
    /// <summary>The in-memory router the app uses under test.</summary>
    public FakeLeadRouter Router { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILeadRouter>();
            services.AddSingleton<ILeadRouter>(Router);
        });
    }

    /// <summary>Extracts the antiforgery token value from a rendered form.</summary>
    /// <param name="html">The rendered page HTML.</param>
    public static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "No antiforgery token found in the form.");
        return match.Groups[1].Value;
    }

    /// <summary>A render timestamp that reads as a genuine human fill-time (a few seconds ago).</summary>
    public static long HumanRenderedAt() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 5000;

    /// <summary>A client that does not auto-follow redirects, so post/redirect/get can be asserted.</summary>
    public HttpClient CreateNoRedirectClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
