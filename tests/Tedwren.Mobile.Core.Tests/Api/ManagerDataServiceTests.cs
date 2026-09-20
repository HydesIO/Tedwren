using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the manager cache-then-network reader: online caches + returns; offline / failure returns the cached copy.</summary>
public class ManagerDataServiceTests
{
    private const string DashboardKey = "manager.dashboard";

    private static DashboardSummaryDto Dashboard(int operatives) =>
        new(new DashboardKpisDto(1, operatives, 1, 100, 0), new ComplianceBreakdownDto(100, operatives, 0, 0, 0, operatives), Array.Empty<SiteRiskRowDto>());

    private static ManagerDataService Service(HttpClient http, IReadCache cache, bool connected) =>
        new(new ManagerApiClient(http), new ManagerSiteEntryApiClient(http, new NoOpTelemetry()), new ManagerWorkforceApiClient(http),
            new ManagerFormsApiClient(http), new ManagerEvidenceApiClient(http), cache, new FakeConnectivity(connected));

    [Fact]
    public async Task Online_fetches_fresh_and_caches_it()
    {
        var cache = new InMemoryReadCache();
        var service = Service(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Dashboard(7))), cache, connected: true);

        var dto = await service.GetDashboardAsync();

        Assert.Equal(7, dto!.Kpis.Operatives);
        Assert.Equal(7, (await cache.GetAsync<DashboardSummaryDto>(DashboardKey))!.Kpis.Operatives);
    }

    [Fact]
    public async Task Offline_returns_the_cached_copy()
    {
        var cache = new InMemoryReadCache();
        await cache.SetAsync(DashboardKey, Dashboard(3));
        var service = Service(FakeHttp.Returning(HttpStatusCode.InternalServerError, new StringContent(string.Empty)), cache, connected: false);

        var dto = await service.GetDashboardAsync();

        Assert.Equal(3, dto!.Kpis.Operatives);
    }

    [Fact]
    public async Task Online_failure_falls_back_to_the_cached_copy()
    {
        var cache = new InMemoryReadCache();
        await cache.SetAsync(DashboardKey, Dashboard(3));
        var service = Service(FakeHttp.Returning(HttpStatusCode.InternalServerError, new StringContent(string.Empty)), cache, connected: true);

        var dto = await service.GetDashboardAsync();

        Assert.Equal(3, dto!.Kpis.Operatives);
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

        public event EventHandler<bool>? ConnectivityChanged { add { } remove { } }
    }
}
