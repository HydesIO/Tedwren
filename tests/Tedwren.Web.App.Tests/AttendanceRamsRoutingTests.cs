using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Web.App.Platform;
using Tedwren.Web.App.Pages.Operative;

namespace Tedwren.Web.App.Tests;

/// <summary>
/// bUnit test for the operative attendance page's Gate-5 routing (Phase 7, "block + retry"): when a sign-in is
/// refused because the operative must sign the RAMS, the page routes to the RAMS sign screen so they can sign and retry.
/// </summary>
public class AttendanceRamsRoutingTests : TestContext
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public void SignIn_routes_to_the_rams_sign_page_when_a_signature_is_required()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;   // WebGeolocation returns null (no geofence hint), the sign-in has no location

        var site = new MobileSiteDto(Guid.NewGuid(), "meridian", "Meridian Tower", null, false, false, null, Array.Empty<SitePropertyDto>());
        var ramsId = Guid.NewGuid();

        Services.AddSingleton<IConnectivityService>(new FakeConnectivity());
        Services.AddSingleton(new OperativeDataService(new OperativeApiClient(StubJson(new[] { site })), new FakeReadCache(), new FakeConnectivity()));
        Services.AddSingleton(new AttendanceApiClient(RoutedAttendance(ramsId), new NoOpTelemetry()));
        Services.AddSingleton(new WebGeolocation(JSInterop.JSRuntime));

        var cut = RenderComponent<Attendance>();
        cut.WaitForAssertion(() => Assert.Contains("Meridian Tower", cut.Markup));   // the site option loaded

        cut.Find("#site").Change(site.Id.ToString());
        cut.Find("button.tw-btn--primary").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => Assert.Contains("/operative/rams-sign", nav.Uri));
    }

    /// <summary>An attendance client whose sign-in is refused pending a RAMS signature, and whose "current" is empty (204).</summary>
    private static HttpClient RoutedAttendance(Guid ramsToSignId) =>
        new(new RoutedHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                var result = new SignInResult(false, "Refused", "You must read and sign the current RAMS before signing in.", Guid.NewGuid(), null, ramsToSignId);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(result, Json), Encoding.UTF8, "application/json"),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);   // GET current — not signed in anywhere
        }))
        { BaseAddress = new Uri("https://api.test/") };

    private static HttpClient StubJson<T>(T payload) =>
        new(new RoutedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json"),
        }))
        { BaseAddress = new Uri("https://api.test/") };

    private sealed class RoutedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public RoutedHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }

    private sealed class FakeConnectivity : IConnectivityService
    {
        public bool IsConnected => true;
        public event EventHandler<bool>? ConnectivityChanged { add { } remove { } }
    }

    private sealed class FakeReadCache : IReadCache
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
