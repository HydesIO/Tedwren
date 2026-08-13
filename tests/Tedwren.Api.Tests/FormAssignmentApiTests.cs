using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Forms;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the Forms Library assignment API (PRD-Phase 2, requirement 5): publish a form, assign
/// it to the organisation, list the assignment, then remove it.
/// </summary>
public sealed class FormAssignmentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private readonly WebApplicationFactory<Program> _factory;

    public FormAssignmentApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Assign_List_Delete_RoundTrip()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/forms/templates", new CreateFormTemplateRequest(
            CompanyId, "Assignment API form", null,
            new List<FormSectionDto> { new("s1", "General", new List<FormFieldDto>
            {
                new("f1", "ShortText", "Name", null, true, null, null, 0),
            }, 0) }));
        var templateId = (await create.Content.ReadFromJsonAsync<Created>())!.Id;
        await client.PostAsync($"/api/forms/templates/{templateId}/publish", null);

        var full = await client.GetFromJsonAsync<FormTemplateDto>($"/api/forms/templates/{templateId}");
        var familyId = full!.FamilyId;

        var assign = await client.PostAsJsonAsync("/api/forms/assignments", new CreateFormAssignmentRequest(
            familyId, "Organisation", null, null, null, "Weekly", "alerts@example.com"));
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);
        var assignmentId = (await assign.Content.ReadFromJsonAsync<Created>())!.Id;

        var list = await client.GetFromJsonAsync<List<FormAssignmentDto>>("/api/forms/assignments");
        Assert.Contains(list!, a => a.Id == assignmentId && a.FormName == "Assignment API form" && a.Schedule == "Weekly");

        var delete = await client.DeleteAsync($"/api/forms/assignments/{assignmentId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await client.GetFromJsonAsync<List<FormAssignmentDto>>("/api/forms/assignments");
        Assert.DoesNotContain(after!, a => a.Id == assignmentId);
    }

    private sealed record Created(Guid Id);
}
