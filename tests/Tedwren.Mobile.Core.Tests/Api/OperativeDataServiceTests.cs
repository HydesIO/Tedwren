using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the cache-then-network reader: online caches + returns; offline / failure returns the cached copy.</summary>
public class OperativeDataServiceTests
{
    private const string DashboardKey = "operative.dashboard";

    private static OperativeDashboardDto Dashboard(string name) =>
        new(name, ComplianceState.Compliant, "Compliant", 12.5m, null, 0, false, null, null);

    [Fact]
    public async Task Online_fetches_fresh_and_caches_it()
    {
        var cache = new InMemoryReadCache();
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Dashboard("Fresh")));
        var service = new OperativeDataService(new OperativeApiClient(http), cache, new FakeConnectivity(connected: true));

        var dto = await service.GetDashboardAsync();

        Assert.Equal("Fresh", dto!.Name);
        Assert.Equal("Fresh", (await cache.GetAsync<OperativeDashboardDto>(DashboardKey))!.Name);
    }

    [Fact]
    public async Task Offline_returns_the_cached_copy()
    {
        var cache = new InMemoryReadCache();
        await cache.SetAsync(DashboardKey, Dashboard("Cached"));
        var http = FakeHttp.Returning(HttpStatusCode.InternalServerError, new StringContent(string.Empty));
        var service = new OperativeDataService(new OperativeApiClient(http), cache, new FakeConnectivity(connected: false));

        var dto = await service.GetDashboardAsync();

        Assert.Equal("Cached", dto!.Name);
    }

    [Fact]
    public async Task Online_failure_falls_back_to_the_cached_copy()
    {
        var cache = new InMemoryReadCache();
        await cache.SetAsync(DashboardKey, Dashboard("Cached"));
        var http = FakeHttp.Returning(HttpStatusCode.InternalServerError, new StringContent(string.Empty));
        var service = new OperativeDataService(new OperativeApiClient(http), cache, new FakeConnectivity(connected: true));

        var dto = await service.GetDashboardAsync();

        Assert.Equal("Cached", dto!.Name);
    }

    private sealed class InMemoryReadCache : IReadCache
    {
        private readonly Dictionary<string, object?> _values = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(key, out var v) ? (T?)v : default);

        public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeConnectivity : IConnectivityService
    {
        public FakeConnectivity(bool connected) => IsConnected = connected;

        public bool IsConnected { get; }

        // Interface-required, but the fake never changes connectivity — no-op accessors avoid an unused-event warning.
        public event EventHandler<bool>? ConnectivityChanged { add { } remove { } }
    }
}
