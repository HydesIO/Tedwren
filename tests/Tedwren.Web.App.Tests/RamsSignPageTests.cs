using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Mobile.Core.Api;
using Tedwren.Web.App.Pages.Operative;

namespace Tedwren.Web.App.Tests;

/// <summary>
/// bUnit render tests for the operative RAMS sign page (Gate 5): it loads the current live RAMS and renders the
/// read + sign action, and a completed signature shows the confirmation.
/// </summary>
public class RamsSignPageTests : TestContext
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static LiveRamsDto Live() => new(
        Guid.NewGuid(), Guid.NewGuid(), 2, "Groundworks RAMS", "RAMS-1", "Groundworks Co", HasFile: false, "Approved",
        DateTimeOffset.UtcNow);

    [Fact] // The page loads the live RAMS and renders the read + sign action.
    public void Renders_the_live_rams_and_sign_action()
    {
        Services.AddSingleton(new RamsApiClient(Routed(_ => Ok(Live()))));

        var cut = RenderComponent<RamsSign>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Groundworks RAMS", cut.Markup);   // the method statement title
            Assert.Contains("Sign RAMS", cut.Markup);          // the sign action
        });
    }

    [Fact] // Signing records the acknowledgement and shows the confirmation (retry-ready).
    public void Signing_shows_the_confirmation()
    {
        var live = Live();
        Services.AddSingleton(new RamsApiClient(Routed(request =>
            request.Method == HttpMethod.Post
                ? Ok(new RamsAcknowledgementDto(Guid.NewGuid(), live.FamilyId, live.Version, DateTimeOffset.UtcNow, null))
                : Ok(live))));

        var cut = RenderComponent<RamsSign>();
        cut.WaitForAssertion(() => Assert.Contains("Sign RAMS", cut.Markup));

        cut.Find("input.tw-input").Input("Alex Operative");   // the name field binds on oninput
        cut.Find("input[type=checkbox]").Change(true);
        cut.Find("button.tw-btn--primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("your signature is recorded", cut.Markup));
    }

    private static HttpResponseMessage Ok<T>(T payload) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json") };

    private static HttpClient Routed(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new RoutedHandler(responder)) { BaseAddress = new Uri("https://api.test/") };

    private sealed class RoutedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public RoutedHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }
}
