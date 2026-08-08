using Tedwren.Application.Expiry;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the expiry-warning engine (SF-9): due stages notify the worker by SMS and the engaging
/// company's administrator by email, and running the scan twice in a day does not send twice.
/// </summary>
public sealed class ExpiryWarningJobTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Builds the job over seeded in-memory stores and returns the outbox for assertions.</summary>
    private static (ExpiryWarningJob Job, NotificationOutbox Outbox) CreateSut(DateOnly cardExpiry)
    {
        var org = new InMemoryOrganisationStore(seed: false);
        var qual = new InMemoryQualificationStore(seed: true);
        var exp = new InMemoryExpiryStore();
        var outbox = new NotificationOutbox();

        var company = new Company { Name = "Alpha Ltd", ContactEmail = "admin@alpha.test" };
        org.Companies[company.Id] = company;
        var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900123") };
        org.People[person.Id] = person;
        var engagement = new Engagement { CompanyId = company.Id, PersonId = person.Id, Name = "Joe Smith", Trade = "Groundworks" };
        org.Engagements[engagement.Id] = engagement;
        var cscsId = qual.Types.Values.Single(t => t.Name == "CSCS Card").Id;
        var card = new QualificationCard { PersonId = person.Id, QualificationTypeId = cscsId, ExpiresOn = cardExpiry };
        qual.Cards[card.Id] = card;

        var job = new ExpiryWarningJob(
            new InMemoryQualificationCardRepository(qual),
            new InMemoryQualificationTypeRepository(qual),
            new InMemoryPersonRepository(org),
            new InMemoryEngagementRepository(org),
            new InMemoryCompanyRepository(org),
            new InMemoryNotificationLogRepository(exp),
            new OutboxSmsSender(outbox),
            new OutboxEmailSender(outbox));
        return (job, outbox);
    }

    [Fact]
    public async Task ThirtyDaysOut_NotifiesWorkerAndAdmin()
    {
        var (job, outbox) = CreateSut(Today.AddDays(30));

        var result = await job.RunAsync(Today);

        // At 30 days the 60- and 30-day stages are both due (catch-up) → 2 stages × (SMS + email).
        Assert.Equal(1, result.CardsEvaluated);
        Assert.Equal(4, result.NotificationsSent);
        Assert.Contains(outbox.Messages, m => m.Channel == "Sms" && m.Recipient == "+447700900123");
        Assert.Contains(outbox.Messages, m => m.Channel == "Email" && m.Recipient == "admin@alpha.test");
    }

    [Fact]
    public async Task RunningTwiceSameDay_DoesNotSendTwice()
    {
        var (job, outbox) = CreateSut(Today.AddDays(30));

        var first = await job.RunAsync(Today);
        var second = await job.RunAsync(Today);

        Assert.Equal(4, first.NotificationsSent);
        Assert.Equal(0, second.NotificationsSent);   // idempotent (SF-9)
        Assert.Equal(4, outbox.Messages.Count);
    }

    [Fact]
    public async Task FarFromExpiry_SendsNothing()
    {
        var (job, outbox) = CreateSut(Today.AddDays(120));

        var result = await job.RunAsync(Today);

        Assert.Equal(0, result.NotificationsSent);
        Assert.Empty(outbox.Messages);
    }
}
