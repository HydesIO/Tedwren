using Tedwren.Abstractions.Contracts.Entitlements;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Expiry;
using Tedwren.Application.Expiry.Sources;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.Notifications;
using Tedwren.Domain.ValueObjects;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the Phase 6 generalisation of the expiry engine (SF-9), weekly digest (SUB-5) and upcoming-expiries read
/// from one source (cards) to three (cards, company documents SUB-4, inductions MC-7): the company-document source is
/// email-only, the induction source is entitlement-gated, the engine notifies across sources idempotently per
/// (source, subject), the digest merges the sources into one company email ordered by date, and the read unions the
/// sources scoped to the tenant.
/// </summary>
public sealed class ExpiryEngineMultiSourceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ---- source projections ----

    [Fact] // SUB-4: a company document projects to an email-only item for its owning company (no operative to text).
    public async Task CompanyDocumentSource_ProjectsEmailOnlyItem()
    {
        var org = new InMemoryOrganisationStore(seed: false);
        var company = new Company { Name = "Alpha Ltd", ContactEmail = "admin@alpha.test" };
        org.Companies[company.Id] = company;
        var doc = new CompanyDocument { CompanyId = company.Id, Name = "Employer's Liability Insurance", Type = "Insurance", ExpiresOn = Today.AddDays(20) };
        org.CompanyDocuments[doc.Id] = doc;

        var source = new CompanyDocumentExpirySource(new InMemoryCompanyRepository(org), new InMemoryCompanyDocumentRepository(org));
        var item = Assert.Single(await source.GetCurrentAsync());

        Assert.Equal(ExpirySource.CompanyDocument, item.Source);
        Assert.Equal(doc.Id, item.SubjectId);
        Assert.Equal("Employer's Liability Insurance", item.Label);
        Assert.Null(item.WorkerNumber);                 // no operative — email only
        Assert.Equal("admin@alpha.test", item.AdminEmail);
        Assert.Null(item.PersonId);
    }

    [Fact] // MC-7 (flagged): induction-expiry items appear only for a company holding the subcontractor-onboarding module.
    public async Task InductionSource_GatedByEntitlement()
    {
        var org = new InMemoryOrganisationStore(seed: false);
        var ind = new InMemoryInductionStore(seed: false);
        var company = new Company { Name = "Alpha Ltd", ContactEmail = "admin@alpha.test" };
        org.Companies[company.Id] = company;
        var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900123") };
        org.People[person.Id] = person;
        var session = new InductionSession
        {
            TemplateId = Guid.NewGuid(), CompanyId = company.Id, PersonId = person.Id, PersonName = "Joe Smith",
            Status = InductionStatus.Passed, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(20),
        };
        ind.Sessions[session.Id] = session;

        InductionExpirySource Source(bool enabled) => new(
            new InMemoryCompanyRepository(org), new InMemoryInductionSessionRepository(ind),
            new InMemoryPersonRepository(org), new StubEntitlements(enabled));

        Assert.Empty(await Source(enabled: false).GetCurrentAsync());   // module off → no alerts (fail-closed)

        var item = Assert.Single(await Source(enabled: true).GetCurrentAsync());
        Assert.Equal(ExpirySource.Induction, item.Source);
        Assert.Equal(session.Id, item.SubjectId);
        Assert.Equal("+447700900123", item.WorkerNumber);
        Assert.Equal("admin@alpha.test", item.AdminEmail);
        Assert.Equal(DateOnly.FromDateTime(session.ExpiresUtc!.Value.UtcDateTime), item.ExpiresOn);
    }

    // ---- engine ----

    [Fact] // SUB-4: a company document 30 days out emails the admin, sends no SMS, and is idempotent on re-run.
    public async Task WarningJob_CompanyDocument_EmailsAdminOnly_AndIdempotent()
    {
        var org = new InMemoryOrganisationStore(seed: false);
        var exp = new InMemoryExpiryStore();
        var outbox = new NotificationOutbox();
        var company = new Company { Name = "Alpha Ltd", ContactEmail = "admin@alpha.test" };
        org.Companies[company.Id] = company;
        org.CompanyDocuments[Guid.NewGuid()] = new CompanyDocument { CompanyId = company.Id, Name = "Public Liability", Type = "Insurance", ExpiresOn = Today.AddDays(30) };

        var job = new ExpiryWarningJob(
            new IExpirySource[] { new CompanyDocumentExpirySource(new InMemoryCompanyRepository(org), new InMemoryCompanyDocumentRepository(org)) },
            new InMemoryNotificationLogRepository(exp), new OutboxSmsSender(outbox), new OutboxEmailSender(outbox));

        var first = await job.RunAsync(Today);
        var second = await job.RunAsync(Today);

        Assert.Equal(2, first.NotificationsSent);   // 60- and 30-day stages, email only
        Assert.Equal(0, second.NotificationsSent);  // idempotent (SF-9)
        Assert.All(outbox.Messages, m => Assert.Equal("Email", m.Channel));
        Assert.DoesNotContain(outbox.Messages, m => m.Channel == "Sms");
        Assert.All(outbox.Messages, m => Assert.Equal("admin@alpha.test", m.Recipient));
    }

    // ---- weekly digest ----

    [Fact] // SUB-5: a company's card and company document appear together in one digest, ordered by expiry date.
    public async Task WeeklyDigest_MergesSourcesOrderedByDate()
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
        qual.Cards[Guid.NewGuid()] = new QualificationCard { PersonId = person.Id, QualificationTypeId = cscsId, ExpiresOn = Today.AddDays(40) };
        org.CompanyDocuments[Guid.NewGuid()] = new CompanyDocument { CompanyId = company.Id, Name = "Public Liability", Type = "Insurance", ExpiresOn = Today.AddDays(10) };

        var digest = new WeeklyDigestJob(
            new IExpirySource[]
            {
                new CardExpirySource(new InMemoryQualificationCardRepository(qual), new InMemoryQualificationTypeRepository(qual),
                    new InMemoryPersonRepository(org), new InMemoryEngagementRepository(org), new InMemoryCompanyRepository(org)),
                new CompanyDocumentExpirySource(new InMemoryCompanyRepository(org), new InMemoryCompanyDocumentRepository(org)),
            },
            new OutboxEmailSender(outbox));

        var result = await digest.RunAsync(Today);

        Assert.Equal(1, result.EmailsSent);
        var body = Assert.Single(outbox.Messages).Body;
        Assert.Contains("CSCS Card", body);
        Assert.Contains("Public Liability", body);
        // Ordered by date: the document (10 days) precedes the card (40 days) in the body.
        Assert.True(body.IndexOf("Public Liability", StringComparison.Ordinal) < body.IndexOf("CSCS Card", StringComparison.Ordinal));
    }

    // ---- read ----

    [Fact] // The upcoming read unions the sources and scopes to the caller's tenant (R15), with a source label per row.
    public async Task Read_UnionsSources_TenantScoped()
    {
        var org = new InMemoryOrganisationStore(seed: false);
        var qual = new InMemoryQualificationStore(seed: true);
        var alpha = new Company { Name = "Alpha Ltd", ContactEmail = "admin@alpha.test" };
        var beta = new Company { Name = "Beta Ltd", ContactEmail = "admin@beta.test" };
        org.Companies[alpha.Id] = alpha;
        org.Companies[beta.Id] = beta;
        var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900123") };
        org.People[person.Id] = person;
        org.Engagements[Guid.NewGuid()] = new Engagement { CompanyId = alpha.Id, PersonId = person.Id, Name = "Joe Smith" };
        var cscsId = qual.Types.Values.Single(t => t.Name == "CSCS Card").Id;
        qual.Cards[Guid.NewGuid()] = new QualificationCard { PersonId = person.Id, QualificationTypeId = cscsId, ExpiresOn = Today.AddDays(15) };
        org.CompanyDocuments[Guid.NewGuid()] = new CompanyDocument { CompanyId = alpha.Id, Name = "Public Liability", Type = "Insurance", ExpiresOn = Today.AddDays(5) };
        org.CompanyDocuments[Guid.NewGuid()] = new CompanyDocument { CompanyId = beta.Id, Name = "Beta Policy", Type = "Policy", ExpiresOn = Today.AddDays(5) };

        var sources = new IExpirySource[]
        {
            new CardExpirySource(new InMemoryQualificationCardRepository(qual), new InMemoryQualificationTypeRepository(qual),
                new InMemoryPersonRepository(org), new InMemoryEngagementRepository(org), new InMemoryCompanyRepository(org)),
            new CompanyDocumentExpirySource(new InMemoryCompanyRepository(org), new InMemoryCompanyDocumentRepository(org)),
        };
        var read = new ExpiryQueryService(sources, new InMemoryJobRunRepository(new InMemoryExpiryStore()), new StubCurrentUser(alpha.Id));

        var upcoming = await read.GetUpcomingAsync(30);

        Assert.Equal(2, upcoming.Count);                                             // Alpha's card + document only
        Assert.Contains(upcoming, u => u.SourceLabel == "Card" && u.PersonName == "Joe Smith");
        Assert.Contains(upcoming, u => u.SourceLabel == "Company document" && u.Label == "Public Liability");
        Assert.DoesNotContain(upcoming, u => u.Label == "Beta Policy");              // other tenant excluded (R15)
        Assert.Equal("Public Liability", upcoming[0].Label);                         // soonest first (5 days)
    }

    /// <summary>A current-user stub scoped to a fixed company.</summary>
    private sealed class StubCurrentUser : ICurrentUserService
    {
        private readonly Guid _companyId;
        public StubCurrentUser(Guid companyId) => _companyId = companyId;
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Test User", "Administrator", _companyId));
    }

    /// <summary>An entitlement service that reports one fixed enabled state for every module.</summary>
    private sealed class StubEntitlements : IEntitlementService
    {
        private readonly bool _enabled;
        public StubEntitlements(bool enabled) => _enabled = enabled;
        public Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ModuleEntitlementDto>>(Array.Empty<ModuleEntitlementDto>());
        public Task<bool> IsEnabledAsync(Guid companyId, string moduleKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_enabled);
        public Task SetEnabledAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
