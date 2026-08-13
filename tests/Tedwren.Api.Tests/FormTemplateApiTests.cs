using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Forms;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the Forms Library template API (PRD-Phase 2 checklist/inspection engine): create a
/// template, read it, update it, publish it (so the fill route serves it), and archive it (so it leaves the
/// library). The in-memory test host authenticates every request as Administrator, satisfying the write policy.
/// </summary>
public sealed class FormTemplateApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host (auth bypass on → Administrator, satisfies RequireWrite).</summary>
    public FormTemplateApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static CreateFormTemplateRequest SampleCreate(string name) => new(
        CompanyId, name, "A short site inspection.",
        new List<FormSectionDto>
        {
            new("s1", "General", new List<FormFieldDto>
            {
                new("f1", "RagStatus", "Housekeeping", null, true, null, null, 0),
                new("f2", "YesNo", "Fire exits clear", "Check all", true, null, null, 1),
            }, 0),
        });

    [Fact]
    public async Task Create_Get_Update_Publish_Archive_RoundTrip()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/forms/templates", SampleCreate("API Inspection"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<CreatedTemplate>())!.Id;

        // Read back the full template with its fields.
        var template = await client.GetFromJsonAsync<FormTemplateDto>($"/api/forms/templates/{id}");
        Assert.NotNull(template);
        Assert.Equal("Draft", template!.Status);
        Assert.Equal(2, template.Sections.Sum(s => s.Fields.Count));

        // Not fillable until published.
        var fillBefore = await client.GetAsync($"/api/forms/templates/{id}/fill");
        Assert.Equal(HttpStatusCode.NotFound, fillBefore.StatusCode);

        // Update the draft in place.
        var put = await client.PutAsJsonAsync($"/api/forms/templates/{id}",
            new UpdateFormTemplateRequest("API Inspection v1.1", "Tweaked.", template.Sections));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        // Publish → the fill route now serves it.
        var publish = await client.PostAsync($"/api/forms/templates/{id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        var fill = await client.GetFromJsonAsync<FormTemplateDto>($"/api/forms/templates/{id}/fill");
        Assert.Equal("Published", fill!.Status);

        // It appears in the library list.
        var list = await client.GetFromJsonAsync<List<FormTemplateSummaryDto>>("/api/forms/templates");
        Assert.Contains(list!, t => t.Id == id && t.FieldCount == 2);

        // Archive → it leaves the library.
        var archive = await client.PostAsync($"/api/forms/templates/{id}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        var listAfter = await client.GetFromJsonAsync<List<FormTemplateSummaryDto>>("/api/forms/templates");
        Assert.DoesNotContain(listAfter!, t => t.Id == id);
    }

    [Fact]
    public async Task Create_BlankName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/forms/templates", SampleCreate("   "));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task GetUnknownTemplate_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var get = await client.GetAsync($"/api/forms/templates/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task SeedStarter_AddsPublishedStarterForms()
    {
        var client = _factory.CreateClient();

        var seed = await client.PostAsync("/api/forms/templates/seed-starter", null);
        Assert.Equal(HttpStatusCode.OK, seed.StatusCode);

        var list = await client.GetFromJsonAsync<List<FormTemplateSummaryDto>>("/api/forms/templates");
        Assert.Contains(list!, t => t.Name == "Daily Site Diary" && t.Status == "Published");
        Assert.Contains(list!, t => t.Name == "Welfare Check");
    }

    private sealed record CreatedTemplate(Guid Id);
}
