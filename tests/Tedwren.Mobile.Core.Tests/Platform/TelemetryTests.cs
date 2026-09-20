using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Tests.Api;

namespace Tedwren.Mobile.Core.Tests.Platform;

/// <summary>Verifies the R14 client-side round-trip timing is reported through the telemetry seam (M8).</summary>
public class TelemetryTests
{
    [Fact]
    public async Task Decide_reports_the_round_trip_timing()
    {
        var telemetry = new RecordingTelemetry();
        var result = new EntryDecisionResultDto(true, null, false, Guid.NewGuid(), 12, Array.Empty<DecisionCheckResultDto>());
        var client = new ManagerSiteEntryApiClient(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(result)), telemetry);

        await client.DecideAsync(new ManagerDecideRequest(Guid.NewGuid(), Guid.NewGuid(), null, null));

        Assert.Contains("site-entry.decide.roundtrip", telemetry.Timings.Select(t => t.Name));
    }

    [Fact]
    public async Task Sign_in_reports_the_round_trip_timing()
    {
        var telemetry = new RecordingTelemetry();
        var outcome = new SignInResult(true, "Accepted", null, Guid.NewGuid(), null);
        var client = new AttendanceApiClient(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(outcome)), telemetry);

        await client.SignInAsync(new MobileSignInRequest(Guid.NewGuid(), null, 51.5, -0.1));

        Assert.Contains("attendance.signin.roundtrip", telemetry.Timings.Select(t => t.Name));
    }

    private sealed class RecordingTelemetry : ITelemetry
    {
        public List<(string Name, TimeSpan Duration)> Timings { get; } = new();

        public void TrackEvent(string name, IReadOnlyDictionary<string, string>? properties = null)
        {
        }

        public void TrackError(Exception exception, string? context = null)
        {
        }

        public void TrackTiming(string name, TimeSpan duration) => Timings.Add((name, duration));
    }
}
