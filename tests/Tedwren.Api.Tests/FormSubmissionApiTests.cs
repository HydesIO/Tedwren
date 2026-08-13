using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Forms;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the Forms Library submission API (PRD-Phase 2): publish a template, complete it at
/// organisation level, list and read the submission, reject a missing-required submission, and approve.
/// </summary>
public sealed class FormSubmissionApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private readonly WebApplicationFactory<Program> _factory;

    public FormSubmissionApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static CreateFormTemplateRequest SampleTemplate() => new(
        CompanyId, "Submission API form", null,
        new List<FormSectionDto>
        {
            new("s1", "General", new List<FormFieldDto>
            {
                new("f1", "YesNo", "Fire exits clear", null, true, null, null, 0),
                new("f2", "ShortText", "Inspector", null, true, null, null, 1),
            }, 0),
        });

    private async Task<Guid> PublishTemplateAsync(HttpClient client)
    {
        var create = await client.PostAsJsonAsync("/api/forms/templates", SampleTemplate());
        var id = (await create.Content.ReadFromJsonAsync<Created>())!.Id;
        await client.PostAsync($"/api/forms/templates/{id}/publish", null);
        return id;
    }

    [Fact]
    public async Task Submit_List_Get_Approve_RoundTrip()
    {
        var client = _factory.CreateClient();
        var templateId = await PublishTemplateAsync(client);

        var submit = await client.PostAsJsonAsync("/api/forms/submissions", new CreateFormSubmissionRequest(
            templateId, "Organisation", null, null,
            new List<FormAnswerDto>
            {
                new("f1", "true", new List<string>()),
                new("f2", "A. Inspector", new List<string>()),
            },
            new List<FormSubmissionFileInput>()));
        Assert.Equal(HttpStatusCode.Created, submit.StatusCode);
        var id = (await submit.Content.ReadFromJsonAsync<Created>())!.Id;

        var detail = await client.GetFromJsonAsync<FormSubmissionDetailDto>($"/api/forms/submissions/{id}");
        Assert.Equal("Submitted", detail!.Status);
        Assert.Equal(2, detail.Answers.Count);

        var list = await client.GetFromJsonAsync<List<FormSubmissionSummaryDto>>("/api/forms/submissions");
        Assert.Contains(list!, s => s.Id == id);

        var approve = await client.PostAsJsonAsync($"/api/forms/submissions/{id}/approve", new ReviewFormSubmissionRequest("OK"));
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        var approved = await approve.Content.ReadFromJsonAsync<FormSubmissionDetailDto>();
        Assert.Equal("Approved", approved!.Status);
    }

    [Fact]
    public async Task Submit_MissingRequired_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var templateId = await PublishTemplateAsync(client);

        var submit = await client.PostAsJsonAsync("/api/forms/submissions", new CreateFormSubmissionRequest(
            templateId, "Organisation", null, null,
            new List<FormAnswerDto> { new("f1", "true", new List<string>()) }, // f2 (required) missing
            new List<FormSubmissionFileInput>()));
        Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode);
    }

    private sealed record Created(Guid Id);
}
