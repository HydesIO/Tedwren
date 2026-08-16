using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Forms;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the recurring-form reminder engine (PRD-Phase 2 checklist scheduling, R12): a recurring form with no
/// submission this period reminds the administrator, the reminder is idempotent per period, and a submission made
/// this period suppresses it. AdHoc assignments are never reminded.
/// </summary>
public sealed class RecurringFormReminderJobTests
{
    private static readonly Guid Company = Guid.Parse("77777777-7777-4777-8777-000000000001");

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Admin", "Administrator", Company));
    }

    /// <summary>Minimal company repository returning one company with a contact email (the reminder recipient).</summary>
    private sealed class StubCompanyRepository : ICompanyRepository
    {
        private readonly Company _company;
        public StubCompanyRepository(Company company) => _company = company;
        public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Company>>(new[] { _company });
        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == _company.Id ? _company : null);
        public Task AddAsync(Company company, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Company company, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record Harness(
        RecurringFormReminderJob Job,
        FormTemplateService Templates,
        FormAssignmentService Assignments,
        FormSubmissionService Submissions,
        NotificationOutbox Outbox);

    private static Harness CreateHarness()
    {
        var store = new InMemoryFormStore(seed: false);
        var templateRepo = new InMemoryFormTemplateRepository(store);
        var submissionRepo = new InMemoryFormSubmissionRepository(store);
        var assignmentRepo = new InMemoryFormAssignmentRepository(store);
        var user = new StubCurrentUser();
        var outbox = new NotificationOutbox();
        var email = new OutboxEmailSender(outbox);
        var companies = new StubCompanyRepository(new Company { Id = Company, Name = "Acme", ContactEmail = "admin@acme.test" });

        var job = new RecurringFormReminderJob(assignmentRepo, submissionRepo, templateRepo, companies, email);
        return new Harness(
            job,
            new FormTemplateService(templateRepo, user),
            new FormAssignmentService(assignmentRepo, templateRepo, user),
            new FormSubmissionService(submissionRepo, templateRepo, user),
            outbox);
    }

    private static async Task<FormTemplateSummaryDto> PublishedFormAsync(FormTemplateService templates)
    {
        var id = await templates.CreateAsync(new CreateFormTemplateRequest(
            Company, "Daily Site Diary", null,
            new List<FormSectionDto> { new("s1", "Diary", new List<FormFieldDto>
            {
                new("f1", "LongText", "What happened today", null, true, null, null, 0),
            }, 0) }));
        await templates.PublishAsync(id);
        return (await templates.GetTemplatesAsync()).Single();
    }

    [Fact] // R12 — a daily form with no submission today reminds the administrator, once.
    public async Task Run_DailyFormNotCompleted_RemindsOnceAndIsIdempotent()
    {
        var h = CreateHarness();
        var form = await PublishedFormAsync(h.Templates);
        await h.Assignments.CreateAsync(new CreateFormAssignmentRequest(form.FamilyId, "Organisation", null, null, null, "Daily", null));

        var now = DateTimeOffset.UtcNow;
        var first = await h.Job.RunAsync(now);
        Assert.Equal(1, first.AssignmentsEvaluated);
        Assert.Equal(1, first.RemindersSent);
        var message = Assert.Single(h.Outbox.Messages);
        Assert.Equal("admin@acme.test", message.Recipient);
        Assert.Contains("Daily Site Diary", message.Subject);

        // Running again the same period sends nothing more (per-period idempotency).
        var second = await h.Job.RunAsync(now);
        Assert.Equal(0, second.RemindersSent);
        Assert.Single(h.Outbox.Messages);
    }

    [Fact] // R12 — a submission made this period suppresses the reminder.
    public async Task Run_CompletedThisPeriod_NoReminder()
    {
        var h = CreateHarness();
        var form = await PublishedFormAsync(h.Templates);
        await h.Assignments.CreateAsync(new CreateFormAssignmentRequest(form.FamilyId, "Organisation", null, null, null, "Daily", null));

        // The form is completed today (the single published version id is form.Id), so nothing is due.
        await h.Submissions.SubmitAsync(new CreateFormSubmissionRequest(
            form.Id, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "All good", new List<string>()) },
            new List<FormSubmissionFileInput>()));

        var result = await h.Job.RunAsync(DateTimeOffset.UtcNow);
        Assert.Equal(1, result.AssignmentsEvaluated);
        Assert.Equal(0, result.RemindersSent);
        Assert.Empty(h.Outbox.Messages);
    }

    [Fact] // R12 — AdHoc assignments have no cadence, so they are never reminded.
    public async Task Run_AdHocAssignment_NotEvaluated()
    {
        var h = CreateHarness();
        var form = await PublishedFormAsync(h.Templates);
        await h.Assignments.CreateAsync(new CreateFormAssignmentRequest(form.FamilyId, "Organisation", null, null, null, "AdHoc", null));

        var result = await h.Job.RunAsync(DateTimeOffset.UtcNow);
        Assert.Equal(0, result.AssignmentsEvaluated);
        Assert.Equal(0, result.RemindersSent);
        Assert.Empty(h.Outbox.Messages);
    }
}
