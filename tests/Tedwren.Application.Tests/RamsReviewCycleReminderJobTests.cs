using Tedwren.Abstractions.Contracts.Entitlements;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Subcontractors;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the RAMS re-review reminder engine (Subcontractor Onboarding spec §4; beyond PRD v6.4, so it is gated
/// behind the paid <c>subcontractor-onboarding</c> module). Once a live RAMS's review cycle has elapsed it reminds
/// the main-contractor administrator, the reminder is idempotent per due window, it fails closed when the module
/// is off, and it does not fire before the cycle is due.
/// </summary>
public sealed class RamsReviewCycleReminderJobTests
{
    private static readonly Guid MainContractor = Guid.Parse("77777777-7777-4777-8777-000000000001");
    private static readonly Guid Subcontractor = Guid.Parse("88888888-8888-4888-8888-000000000002");

    /// <summary>A minimal company repository returning the main contractor with a contact email (the reminder recipient).</summary>
    private sealed class StubCompanyRepository : ICompanyRepository
    {
        private readonly Company _company;
        public StubCompanyRepository(Company company) => _company = company;
        public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Company>>(new[] { _company });
        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == _company.Id ? _company : null);
        public Task<Company?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<Company?>(null);
        public Task AddAsync(Company company, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Company company, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>A fixed-answer entitlement service so the module gate can be turned on or off for the test.</summary>
    private sealed class StubEntitlements : IEntitlementService
    {
        private readonly bool _enabled;
        public StubEntitlements(bool enabled) => _enabled = enabled;
        public Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ModuleEntitlementDto>>(Array.Empty<ModuleEntitlementDto>());
        public Task<bool> IsEnabledAsync(Guid companyId, string moduleKey, CancellationToken cancellationToken = default) => Task.FromResult(_enabled);
        public Task SetEnabledAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record Harness(
        RamsReviewCycleReminderJob Job,
        InMemorySubcontractorOnboardingConfigRepository Configs,
        InMemoryRamsRepository Rams,
        NotificationOutbox Outbox);

    /// <summary>Builds the job with a subcontractor whose live RAMS was approved <paramref name="approvedMonthsAgo"/> months ago, on a 6-month cycle.</summary>
    private static Harness CreateHarness(bool moduleEnabled, int approvedMonthsAgo)
    {
        var configs = new InMemorySubcontractorOnboardingConfigRepository();
        var rams = new InMemoryRamsRepository();
        var outbox = new NotificationOutbox();
        var email = new OutboxEmailSender(outbox);
        var companies = new StubCompanyRepository(new Company { Id = MainContractor, Name = "Meridian Construction", ContactEmail = "admin@meridian.test" });

        var familyId = Guid.NewGuid();
        configs.AddAsync(new SubcontractorOnboardingConfig
        {
            Id = Guid.NewGuid(),
            InviterCompanyId = MainContractor,
            SubcontractorCompanyId = Subcontractor,
            TradeInviteId = Guid.NewGuid(),
            RamsReviewCycleMonths = 6,
            RamsFamilyId = familyId,
            CreatedUtc = DateTimeOffset.UtcNow,
        }).GetAwaiter().GetResult();

        rams.AddAsync(new RamsSubmission
        {
            CompanyId = MainContractor,
            FamilyId = familyId,
            Version = 1,
            Reference = "RAMS-1",
            ContractorName = "Apex Electrical Ltd",
            Title = "Excavation RAMS",
            Status = RamsStatus.Approved,
            IsLive = true,
            ReviewedUtc = DateTimeOffset.UtcNow.AddMonths(-approvedMonthsAgo),
        }).GetAwaiter().GetResult();

        var job = new RamsReviewCycleReminderJob(configs, rams, companies, new StubEntitlements(moduleEnabled), email);
        return new Harness(job, configs, rams, outbox);
    }

    [Fact] // A live RAMS past its 6-month cycle reminds the MC admin — once (idempotent per due window).
    public async Task Run_DueAndEntitled_RemindsOnceAndIsIdempotent()
    {
        var h = CreateHarness(moduleEnabled: true, approvedMonthsAgo: 7);
        var now = DateTimeOffset.UtcNow;

        var first = await h.Job.RunAsync(now);
        Assert.Equal(1, first.ConfigsEvaluated);
        Assert.Equal(1, first.RemindersSent);
        var message = Assert.Single(h.Outbox.Messages);
        Assert.Equal("admin@meridian.test", message.Recipient);
        Assert.Contains("Apex Electrical Ltd", message.Subject);

        // Running again within the same due window sends nothing more.
        var second = await h.Job.RunAsync(now);
        Assert.Equal(0, second.RemindersSent);
        Assert.Single(h.Outbox.Messages);
    }

    [Fact] // Beyond-PRD: fail closed — no reminder unless the tenant holds the subcontractor-onboarding module.
    public async Task Run_ModuleNotEnabled_NoReminder()
    {
        var h = CreateHarness(moduleEnabled: false, approvedMonthsAgo: 7);

        var result = await h.Job.RunAsync(DateTimeOffset.UtcNow);

        Assert.Equal(0, result.RemindersSent);
        Assert.Empty(h.Outbox.Messages);
    }

    [Fact] // Nothing is reminded before the review cycle is due.
    public async Task Run_NotYetDue_NoReminder()
    {
        var h = CreateHarness(moduleEnabled: true, approvedMonthsAgo: 2);   // 2 months into a 6-month cycle

        var result = await h.Job.RunAsync(DateTimeOffset.UtcNow);

        Assert.Equal(1, result.ConfigsEvaluated);
        Assert.Equal(0, result.RemindersSent);
        Assert.Empty(h.Outbox.Messages);
    }
}
