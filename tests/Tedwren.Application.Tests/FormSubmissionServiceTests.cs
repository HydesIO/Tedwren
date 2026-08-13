using System.Text;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Forms;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the Forms Library submission service (PRD-Phase 2): required-by-default validation, publish-only
/// completion, append-only capture with a snapshot of the template version (R16), file capture, the review
/// flow (a rejection needs a reason), and tenant isolation (R15).
/// </summary>
public sealed class FormSubmissionServiceTests
{
    private static readonly Guid Company = Guid.Parse("55555555-5555-4555-8555-000000000001");

    private sealed class StubCurrentUser : ICurrentUserService
    {
        private readonly Guid? _companyId;
        public StubCurrentUser(Guid? companyId) => _companyId = companyId;
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Reviewer", "Administrator", _companyId));
    }

    private static (FormTemplateService Templates, FormSubmissionService Submissions) CreateServices(Guid? companyId, out InMemoryFormStore store)
    {
        store = new InMemoryFormStore(seed: false);
        var templateRepo = new InMemoryFormTemplateRepository(store);
        var submissionRepo = new InMemoryFormSubmissionRepository(store);
        var user = new StubCurrentUser(companyId);
        return (new FormTemplateService(templateRepo, user), new FormSubmissionService(submissionRepo, templateRepo, user));
    }

    private static CreateFormTemplateRequest SampleTemplate() => new(
        Company, "Site Inspection", null,
        new List<FormSectionDto>
        {
            new("s1", "General", new List<FormFieldDto>
            {
                new("f1", "RagStatus", "Housekeeping", null, true, null, null, 0),
                new("f2", "LongText", "Notes", null, false, null, null, 1),
                new("photo", "Photo", "Site photo", null, false, null, null, 2),
            }, 0),
        });

    private static async Task<Guid> PublishedTemplateAsync(FormTemplateService templates)
    {
        var id = await templates.CreateAsync(SampleTemplate());
        await templates.PublishAsync(id);
        return id;
    }

    [Fact] // Requirement 7 / R16 — a completed form is captured with the template version snapshotted.
    public async Task Submit_ValidForm_IsCaptured()
    {
        var (templates, submissions) = CreateServices(Company, out _);
        var templateId = await PublishedTemplateAsync(templates);

        var id = await submissions.SubmitAsync(new CreateFormSubmissionRequest(
            templateId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "Green", new List<string>()) },
            new List<FormSubmissionFileInput>()));

        var detail = await submissions.GetSubmissionAsync(id);
        Assert.NotNull(detail);
        Assert.Equal("Submitted", detail!.Status);
        Assert.Equal(1, detail.FormTemplateVersion);
        Assert.Equal("Site Inspection", detail.FormName);
        Assert.Contains(detail.Answers, a => a.FieldId == "f1" && a.Value == "Green");

        var list = await submissions.GetSubmissionsAsync();
        Assert.Single(list);
    }

    [Fact] // Requirement 2 — a required field left blank is rejected.
    public async Task Submit_MissingRequiredField_Throws()
    {
        var (templates, submissions) = CreateServices(Company, out _);
        var templateId = await PublishedTemplateAsync(templates);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => submissions.SubmitAsync(new CreateFormSubmissionRequest(
            templateId, "Organisation", null, null, new List<FormAnswerDto>(), new List<FormSubmissionFileInput>())));
        Assert.Contains("Housekeeping", ex.Message);
    }

    [Fact] // A required file field is satisfied by an uploaded file, which is persisted.
    public async Task Submit_RequiredFile_IsSatisfiedAndStored()
    {
        var (templates, submissions) = CreateServices(Company, out _);

        // A template whose only required field is a photo.
        var createId = await templates.CreateAsync(new CreateFormTemplateRequest(
            Company, "Photo form", null,
            new List<FormSectionDto> { new("s1", "Evidence", new List<FormFieldDto>
            {
                new("photo", "Photo", "Site photo", null, true, null, null, 0),
            }, 0) }));
        await templates.PublishAsync(createId);

        var id = await submissions.SubmitAsync(new CreateFormSubmissionRequest(
            createId, "Site", Guid.NewGuid(), null, new List<FormAnswerDto>(),
            new List<FormSubmissionFileInput> { new("photo", "site.png", "image/png", Convert.ToBase64String(new byte[] { 1, 2, 3 })) }));

        var detail = await submissions.GetSubmissionAsync(id);
        var file = Assert.Single(detail!.Files);
        Assert.Equal("site.png", file.FileName);

        var content = await submissions.GetFileAsync(file.Id);
        Assert.Equal(new byte[] { 1, 2, 3 }, content!.Content);
    }

    [Fact] // Only a published form can be completed.
    public async Task Submit_AgainstDraft_Throws()
    {
        var (templates, submissions) = CreateServices(Company, out _);
        var draftId = await templates.CreateAsync(SampleTemplate());

        await Assert.ThrowsAsync<ArgumentException>(() => submissions.SubmitAsync(new CreateFormSubmissionRequest(
            draftId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "Green", new List<string>()) },
            new List<FormSubmissionFileInput>())));
    }

    [Fact] // The review flow: approve records the status; reject requires a written reason.
    public async Task Review_ApproveAndReject()
    {
        var (templates, submissions) = CreateServices(Company, out _);
        var templateId = await PublishedTemplateAsync(templates);
        var id = await submissions.SubmitAsync(new CreateFormSubmissionRequest(
            templateId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "Amber", new List<string>()) },
            new List<FormSubmissionFileInput>()));

        var approved = await submissions.ApproveAsync(id, new ReviewFormSubmissionRequest("Looks good"));
        Assert.Equal("Approved", approved!.Status);

        await Assert.ThrowsAsync<ArgumentException>(() => submissions.RejectAsync(id, new ReviewFormSubmissionRequest("  ")));

        var rejected = await submissions.RejectAsync(id, new ReviewFormSubmissionRequest("Redo housekeeping"));
        Assert.Equal("Rejected", rejected!.Status);
        Assert.Equal("Redo housekeeping", rejected.ReviewNote);
    }

    [Fact] // Requirement 4 — a submission renders to a valid branded PDF.
    public async Task GeneratePdf_ProducesValidPdf()
    {
        var (templates, submissions) = CreateServices(Company, out _);
        var templateId = await PublishedTemplateAsync(templates);
        var id = await submissions.SubmitAsync(new CreateFormSubmissionRequest(
            templateId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "Green", new List<string>()) },
            new List<FormSubmissionFileInput>()));

        var pdf = await submissions.GeneratePdfAsync(id);

        Assert.NotNull(pdf);
        Assert.Equal("application/pdf", pdf!.ContentType);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf.Content, 0, 4));
    }

    [Fact] // Requirement 6 — a submission is emailed with its PDF attached.
    public async Task Email_SendsWithPdfAttachment()
    {
        var store = new InMemoryFormStore(seed: false);
        var templateRepo = new InMemoryFormTemplateRepository(store);
        var submissionRepo = new InMemoryFormSubmissionRepository(store);
        var user = new StubCurrentUser(Company);
        var templates = new FormTemplateService(templateRepo, user);
        var outbox = new NotificationOutbox();
        var submissions = new FormSubmissionService(submissionRepo, templateRepo, user, new OutboxEmailSender(outbox));

        var templateId = await PublishedTemplateAsync(templates);
        var id = await submissions.SubmitAsync(new CreateFormSubmissionRequest(
            templateId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "Green", new List<string>()) },
            new List<FormSubmissionFileInput>()));

        var sent = await submissions.EmailAsync(id, "manager@example.com");

        Assert.True(sent);
        var message = Assert.Single(outbox.Messages);
        Assert.Equal("manager@example.com", message.Recipient);
        Assert.Contains(".pdf", message.Body);   // the outbox stub notes the attachment name
    }

    [Fact] // PRD §8 — a red (failed) RAG check emails the failed report to the assignment's alert address.
    public async Task Submit_RedRagCheck_EmailsFailureAlert()
    {
        var store = new InMemoryFormStore(seed: false);
        var templateRepo = new InMemoryFormTemplateRepository(store);
        var submissionRepo = new InMemoryFormSubmissionRepository(store);
        var assignmentRepo = new InMemoryFormAssignmentRepository(store);
        var user = new StubCurrentUser(Company);
        var templates = new FormTemplateService(templateRepo, user);
        var assignments = new FormAssignmentService(assignmentRepo, templateRepo, user);
        var outbox = new NotificationOutbox();
        var submissions = new FormSubmissionService(submissionRepo, templateRepo, user, new OutboxEmailSender(outbox), sites: null, assignmentRepo);

        // A published form with a RAG field, assigned with a failed-check alert address.
        var createId = await templates.CreateAsync(new CreateFormTemplateRequest(
            Company, "Plant Check", null,
            new List<FormSectionDto> { new("s1", "Checks", new List<FormFieldDto>
            {
                new("f1", "RagStatus", "Guards fitted", null, true, null, null, 0),
            }, 0) }));
        await templates.PublishAsync(createId);
        var form = (await templates.GetTemplatesAsync()).Single();
        await assignments.CreateAsync(new CreateFormAssignmentRequest(form.FamilyId, "Organisation", null, null, null, "Daily", "safety@example.com"));

        // A green check raises no alert…
        await submissions.SubmitAsync(new CreateFormSubmissionRequest(createId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "Green", new List<string>()) }, new List<FormSubmissionFileInput>()));
        Assert.Empty(outbox.Messages);

        // …a red check does, with the report attached.
        await submissions.SubmitAsync(new CreateFormSubmissionRequest(createId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "Red", new List<string>()) }, new List<FormSubmissionFileInput>()));

        var alert = Assert.Single(outbox.Messages);
        Assert.Equal("safety@example.com", alert.Recipient);
        Assert.Contains("Failed check", alert.Subject);
        Assert.Contains(".pdf", alert.Body);
    }

    [Fact] // R15 — another company cannot see a submission.
    public async Task GetSubmission_CrossTenant_ReturnsNull()
    {
        var (templates, submissions) = CreateServices(Company, out var store);
        var templateId = await PublishedTemplateAsync(templates);
        var id = await submissions.SubmitAsync(new CreateFormSubmissionRequest(
            templateId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "Red", new List<string>()) },
            new List<FormSubmissionFileInput>()));

        var otherCompany = Guid.Parse("66666666-6666-4666-8666-000000000002");
        var intruder = new FormSubmissionService(new InMemoryFormSubmissionRepository(store),
            new InMemoryFormTemplateRepository(store), new StubCurrentUser(otherCompany));

        Assert.Null(await intruder.GetSubmissionAsync(id));
        Assert.Empty(await intruder.GetSubmissionsAsync());
    }
}
