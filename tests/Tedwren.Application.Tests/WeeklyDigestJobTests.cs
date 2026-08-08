using Tedwren.Application.Expiry;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>Verifies the weekly digest (SUB-5): one email per company covering items expiring within 60 days.</summary>
public sealed class WeeklyDigestJobTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Company_WithItemWithin60Days_GetsOneDigestEmail()
    {
        var org = new InMemoryOrganisationStore(seed: false);
        var qual = new InMemoryQualificationStore(seed: true);
        var outbox = new NotificationOutbox();

        var company = new Company { Name = "Alpha Ltd", ContactEmail = "admin@alpha.test" };
        org.Companies[company.Id] = company;
        var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900123") };
        org.People[person.Id] = person;
        org.Engagements[Guid.NewGuid()] = new Engagement { CompanyId = company.Id, PersonId = person.Id, Name = "Joe Smith" };
        var cscsId = qual.Types.Values.Single(t => t.Name == "CSCS Card").Id;
        qual.Cards[Guid.NewGuid()] = new QualificationCard { PersonId = person.Id, QualificationTypeId = cscsId, ExpiresOn = Today.AddDays(20) };

        var digest = new WeeklyDigestJob(
            new InMemoryCompanyRepository(org),
            new InMemoryEngagementRepository(org),
            new InMemoryQualificationCardRepository(qual),
            new InMemoryQualificationTypeRepository(qual),
            new OutboxEmailSender(outbox));

        var result = await digest.RunAsync(Today);

        Assert.Equal(1, result.EmailsSent);
        var message = Assert.Single(outbox.Messages);
        Assert.Equal("admin@alpha.test", message.Recipient);
        Assert.Contains("CSCS Card", message.Body);
    }

    [Fact]
    public async Task Company_WithNothingExpiringSoon_GetsNoEmail()
    {
        var org = new InMemoryOrganisationStore(seed: false);
        var qual = new InMemoryQualificationStore(seed: true);
        var outbox = new NotificationOutbox();

        var company = new Company { Name = "Beta Ltd", ContactEmail = "admin@beta.test" };
        org.Companies[company.Id] = company;

        var digest = new WeeklyDigestJob(
            new InMemoryCompanyRepository(org),
            new InMemoryEngagementRepository(org),
            new InMemoryQualificationCardRepository(qual),
            new InMemoryQualificationTypeRepository(qual),
            new OutboxEmailSender(outbox));

        var result = await digest.RunAsync(Today);

        Assert.Equal(0, result.EmailsSent);
        Assert.Empty(outbox.Messages);
    }
}
