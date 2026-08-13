using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Forms;
using Tedwren.Application.Inductions;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the induction-embedded form fill (Forms Library integration, requirement 5, R5): a Form step of an
/// induction session exposes its published form, submitting it records the submission against the session's
/// company/operative and marks the step done, and required-by-default validation still applies (R2).
/// </summary>
public sealed class InductionFormFillTests
{
    private static readonly Guid Company = Guid.Parse("88888888-8888-4888-8888-000000000001");

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Worker", "Administrator", Company));
    }

    private sealed record Harness(InductionService Inductions, FormSubmissionService Submissions, Guid TemplateId, Guid FormVersionId, string FormStepId);

    /// <summary>Wires the induction service over a published form attached to a single Form step of a company induction template.</summary>
    private static async Task<Harness> CreateAsync()
    {
        var formStore = new InMemoryFormStore(seed: false);
        var formTemplateRepo = new InMemoryFormTemplateRepository(formStore);
        var formSubmissionRepo = new InMemoryFormSubmissionRepository(formStore);
        var user = new StubCurrentUser();
        var formTemplates = new FormTemplateService(formTemplateRepo, user);
        var formSubmissions = new FormSubmissionService(formSubmissionRepo, formTemplateRepo, user);

        // A published form (RAG check + notes) that the induction will embed.
        var formId = await formTemplates.CreateAsync(new CreateFormTemplateRequest(
            Company, "Site Safety Check", null,
            new List<FormSectionDto> { new("s1", "Checks", new List<FormFieldDto>
            {
                new("f1", "RagStatus", "PPE worn", null, true, null, null, 0),
                new("f2", "LongText", "Notes", null, false, null, null, 1),
            }, 0) }));
        await formTemplates.PublishAsync(formId);
        var form = (await formTemplates.GetTemplatesAsync()).Single();

        var inductionStore = new InMemoryInductionStore();
        var inductions = new InductionService(
            new InMemoryInductionTemplateRepository(inductionStore),
            new InMemoryInductionSessionRepository(inductionStore),
            formTemplateRepo,
            formSubmissions);

        // A company induction template whose only step embeds the form (Kind "Form", Reference = family id).
        var templateId = await inductions.CreateDefaultTemplateAsync(new CreateInductionTemplateRequest(Company, "Site Induction", 365, 0));
        await inductions.UpdateTemplateAsync(templateId, new UpdateInductionTemplateRequest(
            "Site Induction", 365, 0, 3, false, null, null,
            new List<InductionStepDto> { new("form1", "Form", "Complete the site safety check", true, form.FamilyId.ToString()) },
            new List<InductionQuizAuthoringDto>()));

        return new Harness(inductions, formSubmissions, templateId, form.Id, "form1");
    }

    [Fact] // Requirement 5 — a session's Form step exposes its published form to fill.
    public async Task GetSessionForm_ReturnsPublishedForm()
    {
        var h = await CreateAsync();
        var session = await h.Inductions.StartAsync(new StartInductionRequest(Company, h.TemplateId, Guid.NewGuid(), "A. Worker"));

        var form = await h.Inductions.GetSessionFormAsync(session.Id, h.FormStepId);

        Assert.NotNull(form);
        Assert.Equal(h.FormVersionId, form!.Id);
        Assert.Equal("Site Safety Check", form.Name);
        Assert.Contains(form.Sections.SelectMany(s => s.Fields), f => f.Id == "f1");
    }

    [Fact] // Requirement 5 — submitting the form records the submission and marks the step done.
    public async Task SubmitSessionForm_RecordsSubmissionAndCompletesStep()
    {
        var h = await CreateAsync();
        var personId = Guid.NewGuid();
        var session = await h.Inductions.StartAsync(new StartInductionRequest(Company, h.TemplateId, personId, "A. Worker"));

        var updated = await h.Inductions.SubmitSessionFormAsync(session.Id, h.FormStepId, new CreateFormSubmissionRequest(
            h.FormVersionId, "Induction", null, null,
            new List<FormAnswerDto> { new("f1", "Green", new List<string>()) },
            new List<FormSubmissionFileInput>()));

        Assert.NotNull(updated);
        Assert.Contains(h.FormStepId, updated!.CompletedStepIds);

        // The submission is recorded against the session's company and operative (SubmittedBy = the worker's name).
        var submission = Assert.Single(await h.Submissions.GetSubmissionsAsync());
        Assert.Equal("Site Safety Check", submission.FormName);
        Assert.Equal("A. Worker", submission.SubmittedBy);
        Assert.Equal("Induction", submission.Scope);
    }

    [Fact] // R2 — a required field left blank is rejected even on the anonymous induction path.
    public async Task SubmitSessionForm_MissingRequiredField_Throws()
    {
        var h = await CreateAsync();
        var session = await h.Inductions.StartAsync(new StartInductionRequest(Company, h.TemplateId, Guid.NewGuid(), "A. Worker"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Inductions.SubmitSessionFormAsync(session.Id, h.FormStepId, new CreateFormSubmissionRequest(
                h.FormVersionId, "Induction", null, null,
                new List<FormAnswerDto>(), new List<FormSubmissionFileInput>())));
    }

    [Fact] // A non-form step (or unknown step) has no form to serve.
    public async Task GetSessionForm_NonFormStep_ReturnsNull()
    {
        var h = await CreateAsync();
        var session = await h.Inductions.StartAsync(new StartInductionRequest(Company, h.TemplateId, Guid.NewGuid(), "A. Worker"));

        Assert.Null(await h.Inductions.GetSessionFormAsync(session.Id, "does-not-exist"));
    }
}
