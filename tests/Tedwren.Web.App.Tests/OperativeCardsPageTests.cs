using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Sync;
using Tedwren.Web.App.Pages.Operative;

namespace Tedwren.Web.App.Tests;

/// <summary>
/// bUnit render tests for the operative accreditation screens (Gate 3): the add-accreditation page renders the
/// type picker + save action from the type library, and My cards renders the trade's "still needed" shortfall
/// plus the add-accreditation entry.
/// </summary>
public class OperativeCardsPageTests : TestContext
{
    [Fact] // The add page loads the type library and renders the picker + save action.
    public void AddAccreditation_renders_the_type_picker_and_save_action()
    {
        var types = new[]
        {
            new QualificationTypeDto(Guid.NewGuid(), "Gas Safe", "Health & Safety", "Gas Safe Register", 60, false, 0),
        };
        var connectivity = new FakeConnectivity();
        var outbox = new FakeOutboxStore();
        Services.AddSingleton<IConnectivityService>(connectivity);
        Services.AddSingleton<IOutboxStore>(outbox);
        Services.AddSingleton(new SyncEngine(outbox, connectivity, Array.Empty<IOutboxItemHandler>()));
        Services.AddSingleton(new CaptureApiClient(StubJson(types)));

        var cut = RenderComponent<AddAccreditation>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Add accreditation", cut.Markup);
            Assert.Contains("Gas Safe", cut.Markup);            // the type option
            Assert.Contains("Save accreditation", cut.Markup);   // the submit action
        });
    }

    [Fact] // My cards surfaces the trade's outstanding accreditations (SF-11) and the add-accreditation entry.
    public void MyCards_renders_the_still_needed_shortfall_and_add_link()
    {
        var profile = new OperativeDetailDto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "alex-operative", "Alex Operative", "Gas Engineer",
            "Acme", "+447700900700", ComplianceState.AtRisk, "Induction required",
            Array.Empty<OperativeQualificationDto>(), Array.Empty<OperativeHistoryDto>(),
            new[] { "Gas Safe" });

        Services.AddSingleton(new OperativeDataService(new OperativeApiClient(StubJson(profile)), new FakeReadCache(), new FakeConnectivity()));

        var cut = RenderComponent<MyCards>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Still needed", cut.Markup);
            Assert.Contains("Gas Safe", cut.Markup);
            Assert.Contains("/operative/cards/add", cut.Markup);   // the add-accreditation entry
        });
    }

    private static HttpClient StubJson<T>(T payload) =>
        new(new StubHandler(JsonSerializer.Serialize(payload))) { BaseAddress = new Uri("https://api.test/") };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        public StubHandler(string json) => _json = json;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_json, Encoding.UTF8, "application/json") });
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

    private sealed class FakeOutboxStore : IOutboxStore
    {
        public Task<OutboxItem> EnqueueAsync(OutboxItem item, CancellationToken cancellationToken = default) => Task.FromResult(item);
        public Task<IReadOnlyList<OutboxItem>> GetDrainableAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxItem>>(Array.Empty<OutboxItem>());
        public Task UpdateAsync(OutboxItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<OutboxCounts> GetCountsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new OutboxCounts(0, 0));
    }
}
