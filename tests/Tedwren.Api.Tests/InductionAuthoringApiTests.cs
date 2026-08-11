using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Inductions;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for induction template authoring (D3, MC-15): create a template, then edit its content,
/// pass mark, attempts and quiz, and read it back with answers (authorised admin only, R5).
/// </summary>
public sealed class InductionAuthoringApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host (auth bypass on → Administrator).</summary>
    public InductionAuthoringApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Create_Edit_Readback_Template()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/inductions/templates",
            new CreateInductionTemplateRequest(CompanyId, "Meridian Induction", 365, 1));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<CreatedTemplate>())!.Id;

        var forEdit = await client.GetFromJsonAsync<InductionTemplateAuthoringDto>($"/api/inductions/templates/{id}/edit");
        Assert.NotNull(forEdit);

        var questions = new List<InductionQuizAuthoringDto>
        {
            new("q1", "Is a hard hat required on site?", new[] { "Yes", "No" }, 0),
            new("q2", "What do you do in a fire?", new[] { "Evacuate", "Hide" }, 0),
        };
        var update = new UpdateInductionTemplateRequest(
            "Meridian Induction v2", 180, 2, 4, Mandatory: true, MediaUrl: "https://videos/site.mp4", SiteId: null,
            forEdit!.Steps, questions);

        var put = await client.PutAsJsonAsync($"/api/inductions/templates/{id}", update);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = (await put.Content.ReadFromJsonAsync<InductionTemplateAuthoringDto>())!;

        Assert.Equal("Meridian Induction v2", updated.Name);
        Assert.Equal(180, updated.ValidityDays);
        Assert.Equal(2, updated.PassMark);
        Assert.Equal(4, updated.AttemptLimit);
        Assert.Equal("https://videos/site.mp4", updated.MediaUrl);
        Assert.Equal(2, updated.Questions.Count);
        Assert.Equal(0, updated.Questions[0].CorrectOptionIndex);

        // The management list reflects the new question count.
        var templates = await client.GetFromJsonAsync<List<InductionTemplateDto>>($"/api/inductions/templates/{CompanyId}");
        Assert.Contains(templates!, t => t.Id == id && t.QuestionCount == 2);
    }

    [Fact]
    public async Task Update_PassMarkAboveQuestionCount_IsRejected()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/inductions/templates",
            new CreateInductionTemplateRequest(CompanyId, "Bad passmark", 365, 1));
        var id = (await create.Content.ReadFromJsonAsync<CreatedTemplate>())!.Id;
        var forEdit = await client.GetFromJsonAsync<InductionTemplateAuthoringDto>($"/api/inductions/templates/{id}/edit");

        var update = new UpdateInductionTemplateRequest("X", 365, 5, 3, true, null, null,
            forEdit!.Steps, new List<InductionQuizAuthoringDto> { new("q1", "Only one?", new[] { "Yes", "No" }, 0) });

        var put = await client.PutAsJsonAsync($"/api/inductions/templates/{id}", update);
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    private sealed record CreatedTemplate(Guid Id);
}
