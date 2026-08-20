using Tedwren.Application.Attendance;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>Verifies the overnight still-signed-in check (SF-19): an open sign-in past the cutoff is flagged
/// (not truncated) and the manager alerted, and re-running does not flag it again.</summary>
public sealed class OvernightSignInJobTests
{
    [Fact]
    public async Task OpenSignInBeforeCutoff_IsFlaggedAndAlerted_ThenIdempotent()
    {
        var org = new InMemoryOrganisationStore(seed: false);
        var siteStore = new InMemorySiteStore(seed: false);
        var attendance = new InMemoryAttendanceStore(seed: false);
        var outbox = new NotificationOutbox();

        var company = new Company { Name = "Alpha Ltd", ContactEmail = "manager@alpha.test" };
        org.Companies[company.Id] = company;
        var site = new Site { CompanyId = company.Id, Name = "Alpha Site", Boundary = new Geofence(51.5, -0.1, 150) };
        siteStore.Sites[site.Id] = site;
        var person = Guid.NewGuid();
        attendance.Records[Guid.NewGuid()] = new AttendanceRecord
        {
            PersonId = person,
            SiteId = site.Id,
            Type = AttendanceEventType.SignIn,
            Outcome = AttendanceOutcome.Accepted,
            OccurredUtc = DateTimeOffset.UtcNow.AddDays(-1),   // signed in yesterday, still open
        };

        var job = new OvernightSignInJob(
            new InMemoryAttendanceRepository(attendance),
            new InMemorySiteRepository(siteStore),
            new InMemoryCompanyRepository(org),
            new OutboxEmailSender(outbox));
        var cutoff = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        var first = await job.RunAsync(cutoff);
        var second = await job.RunAsync(cutoff);

        Assert.Equal((1, 1), first);                 // flagged once, one alert
        Assert.Equal((0, 0), second);                // idempotent
        Assert.Single(outbox.Messages, m => m.Recipient == "manager@alpha.test");
    }
}
