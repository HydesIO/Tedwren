using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the forms client (M6): it maps the assignments list + template, and surfaces a submit failure.</summary>
public class FormsApiClientTests
{
    [Fact]
    public async Task GetAssignments_maps_the_list()
    {
        var assignment = new MobileFormAssignmentDto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Site Inspection", "Organisation", null, null, "Daily");
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(new[] { assignment }));
        var client = new FormsApiClient(http);

        var result = await client.GetAssignmentsAsync();

        Assert.Single(result);
        Assert.Equal("Site Inspection", result[0].FormName);
    }

    [Fact]
    public async Task GetTemplate_maps_the_template()
    {
        var template = new FormTemplateDto(
            Guid.NewGuid(), Guid.NewGuid(), "Inspection", null, 1, "Published",
            Array.Empty<FormSectionDto>(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(template));
        var client = new FormsApiClient(http);

        var result = await client.GetTemplateAsync(template.Id);

        Assert.Equal(template.Id, result!.Id);
    }

    [Fact]
    public async Task Submit_throws_on_failure()
    {
        var http = FakeHttp.Returning(HttpStatusCode.BadRequest, new StringContent(string.Empty));
        var client = new FormsApiClient(http);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.SubmitAsync(
            new CreateFormSubmissionRequest(Guid.NewGuid(), "Organisation", null, null,
                Array.Empty<FormAnswerDto>(), Array.Empty<FormSubmissionFileInput>())));
    }
}
