using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Notifications;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the expiry-engine jobs (SF-9, SF-21, R12) in the API's default (mock) mode
/// with the background scheduler disabled, so the manual triggers are the only thing that runs.
/// </summary>
public sealed class JobApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public JobApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>An isolated client (own host, hence own outbox/store) with the scheduler disabled.</summary>
    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // SF-9 + SF-21
    public async Task ExpiryScan_NotifiesWorkerAndAdmin_AndIsIdempotent()
    {
        var client = CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // A company with a contact email, an operative, and a card expiring within the warning window.
        await client.PostAsJsonAsync("/api/organisation/companies",
            new CreateCompanyRequest("Jobs Test Ltd", "Subcontractor", "Groundworks", null, null, null, "admin@jobstest.test", null));
        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/organisation/companies");
        var companyId = companies!.Single(c => c.Name == "Jobs Test Ltd").Id;

        var addResponse = await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, "Joe Smith", "07700900321", "Groundworks", null));
        var added = await addResponse.Content.ReadFromJsonAsync<AddOperativeResult>();
        var personId = added!.PersonId!.Value;

        var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types");
        await client.PostAsJsonAsync("/api/qualifications/cards",
            new CaptureCardRequest(personId, types![0].Id, "CS1", "Joe Smith", null, today.AddDays(20), false));

        // First scan sends warnings; the outbox shows a worker SMS and an admin email.
        var firstScan = await (await client.PostAsync("/api/jobs/expiry-scan", content: null)).Content.ReadFromJsonAsync<ExpiryScanResultDto>();
        Assert.True(firstScan!.NotificationsSent > 0);

        var outbox = await client.GetFromJsonAsync<List<OutboxMessage>>("/api/jobs/outbox");
        Assert.Contains(outbox!, m => m.Channel == "Email" && m.Recipient == "admin@jobstest.test");
        Assert.Contains(outbox!, m => m.Channel == "Sms");

        // Second scan the same day sends nothing (SF-9 idempotency).
        var secondScan = await (await client.PostAsync("/api/jobs/expiry-scan", content: null)).Content.ReadFromJsonAsync<ExpiryScanResultDto>();
        Assert.Equal(0, secondScan!.NotificationsSent);

        // The runs are recorded (SF-21).
        var runs = await client.GetFromJsonAsync<List<JobRunDto>>("/api/jobs/runs");
        Assert.Contains(runs!, r => r.JobName == "expiry-scan" && r.Status == "Succeeded");
    }

    [Fact] // R12
    public async Task HeartbeatCheck_WithNoRuns_AlertsOps()
    {
        var client = CreateClient();

        var result = await (await client.PostAsync("/api/jobs/heartbeat-check", content: null)).Content.ReadFromJsonAsync<HeartbeatResultDto>();

        Assert.Equal(2, result!.AlertsRaised);
        var outbox = await client.GetFromJsonAsync<List<OutboxMessage>>("/api/jobs/outbox");
        Assert.Contains(outbox!, m => m.Recipient == "ops@tedwren.local");
    }
}
