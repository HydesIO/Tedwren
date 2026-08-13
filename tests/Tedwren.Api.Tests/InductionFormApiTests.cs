using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Inductions;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the induction-embedded form fill (Forms Library integration, requirement 5, R5): the
/// worker's anonymous take-flow serves and submits a published form attached to a Form step, and those routes are
/// reachable without authentication (the secure-by-default boundary is opened only for the take-flow).
/// </summary>
public sealed class InductionFormApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    // The company the test-bypass identity is scoped to (matches TestBypassAuthenticationHandler).
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private readonly WebApplicationFactory<Program> _factory;

    public InductionFormApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact] // Requirement 5 — a Form step serves its published form and accepts the completed submission.
    public async Task EmbeddedForm_ServedAndSubmitted_ThroughSession()
    {
        var client = _factory.CreateClient();

        // A published form to embed.
        var create = await client.PostAsJsonAsync("/api/forms/templates", new CreateFormTemplateRequest(
            CompanyId, "Induction Safety Check", null,
            new List<FormSectionDto> { new("s1", "Checks", new List<FormFieldDto>
            {
                new("f1", "RagStatus", "PPE worn", null, true, null, null, 0),
            }, 0) }));
        var templateId = (await create.Content.ReadFromJsonAsync<Created>())!.Id;
        await client.PostAsync($"/api/forms/templates/{templateId}/publish", null);
        var full = await client.GetFromJsonAsync<FormTemplateDto>($"/api/forms/templates/{templateId}");
        var familyId = full!.FamilyId;

        // An induction template whose only step embeds the form.
        var indTemplateId = (await (await client.PostAsJsonAsync("/api/inductions/templates",
            new CreateInductionTemplateRequest(CompanyId, "Embedded Form Induction", 365, 0)))
            .Content.ReadFromJsonAsync<Created>())!.Id;
        await client.PutAsJsonAsync($"/api/inductions/templates/{indTemplateId}", new UpdateInductionTemplateRequest(
            "Embedded Form Induction", 365, 0, 3, false, null, null,
            new List<InductionStepDto> { new("form1", "Form", "Complete the safety check", true, familyId.ToString()) },
            new List<InductionQuizAuthoringDto>()));

        // The worker's take-flow: start a session, fetch the embedded form, submit it.
        var session = await (await client.PostAsJsonAsync("/api/inductions/sessions",
            new StartInductionRequest(CompanyId, indTemplateId, Guid.NewGuid(), "A. Worker")))
            .Content.ReadFromJsonAsync<InductionSessionDto>();

        var form = await client.GetFromJsonAsync<FormTemplateDto>($"/api/inductions/sessions/{session!.Id}/forms/form1");
        Assert.Equal("Induction Safety Check", form!.Name);

        var submitResponse = await client.PostAsJsonAsync($"/api/inductions/sessions/{session.Id}/forms/form1",
            new CreateFormSubmissionRequest(form.Id, "Induction", null, null,
                new List<FormAnswerDto> { new("f1", "Green", new List<string>()) },
                new List<FormSubmissionFileInput>()));
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var updated = await submitResponse.Content.ReadFromJsonAsync<InductionSessionDto>();
        Assert.Contains("form1", updated!.CompletedStepIds);

        // The submission is recorded and visible in the company's submissions.
        var submissions = await client.GetFromJsonAsync<List<FormSubmissionSummaryDto>>("/api/forms/submissions");
        Assert.Contains(submissions!, s => s.FormName == "Induction Safety Check" && s.SubmittedBy == "A. Worker");
    }

    [Fact] // R2 — a required field left blank is rejected with a 400.
    public async Task EmbeddedForm_MissingRequiredField_BadRequest()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/forms/templates", new CreateFormTemplateRequest(
            CompanyId, "Required Check", null,
            new List<FormSectionDto> { new("s1", "Checks", new List<FormFieldDto>
            {
                new("f1", "RagStatus", "PPE worn", null, true, null, null, 0),
            }, 0) }));
        var templateId = (await create.Content.ReadFromJsonAsync<Created>())!.Id;
        await client.PostAsync($"/api/forms/templates/{templateId}/publish", null);
        var familyId = (await client.GetFromJsonAsync<FormTemplateDto>($"/api/forms/templates/{templateId}"))!.FamilyId;

        var indTemplateId = (await (await client.PostAsJsonAsync("/api/inductions/templates",
            new CreateInductionTemplateRequest(CompanyId, "Required Field Induction", 365, 0)))
            .Content.ReadFromJsonAsync<Created>())!.Id;
        await client.PutAsJsonAsync($"/api/inductions/templates/{indTemplateId}", new UpdateInductionTemplateRequest(
            "Required Field Induction", 365, 0, 3, false, null, null,
            new List<InductionStepDto> { new("form1", "Form", "Complete the check", true, familyId.ToString()) },
            new List<InductionQuizAuthoringDto>()));

        var session = await (await client.PostAsJsonAsync("/api/inductions/sessions",
            new StartInductionRequest(CompanyId, indTemplateId, Guid.NewGuid(), "A. Worker")))
            .Content.ReadFromJsonAsync<InductionSessionDto>();
        var form = await client.GetFromJsonAsync<FormTemplateDto>($"/api/inductions/sessions/{session!.Id}/forms/form1");

        var submit = await client.PostAsJsonAsync($"/api/inductions/sessions/{session.Id}/forms/form1",
            new CreateFormSubmissionRequest(form!.Id, "Induction", null, null,
                new List<FormAnswerDto>(), new List<FormSubmissionFileInput>()));
        Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode);
    }

    [Fact] // R5 — the take-flow form routes are anonymous (no auth token) and 404 for an unknown session, never 401.
    public async Task SessionFormRoutes_AreAnonymous()
    {
        var anonymous = _factory.WithWebHostBuilder(b => b.UseSetting("Auth:TestBypass", "false")).CreateClient();

        var get = await anonymous.GetAsync($"/api/inductions/sessions/{Guid.NewGuid()}/forms/step1");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        var post = await anonymous.PostAsJsonAsync($"/api/inductions/sessions/{Guid.NewGuid()}/forms/step1",
            new CreateFormSubmissionRequest(Guid.NewGuid(), "Induction", null, null,
                new List<FormAnswerDto>(), new List<FormSubmissionFileInput>()));
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    private sealed record Created(Guid Id);
}
