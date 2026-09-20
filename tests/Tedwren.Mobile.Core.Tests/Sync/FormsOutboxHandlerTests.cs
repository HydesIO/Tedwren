using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Sync;
using Tedwren.Mobile.Core.Tests.Api;

namespace Tedwren.Mobile.Core.Tests.Sync;

/// <summary>Verifies the forms outbox handler (M6): it POSTs the queued submission carried in the item payload.</summary>
public class FormsOutboxHandlerTests
{
    [Fact]
    public async Task Posts_the_form_submission_from_the_payload()
    {
        var posted = false;
        var http = FakeHttp.Routed(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/forms/submissions", StringComparison.Ordinal))
            {
                posted = true;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { Id = Guid.NewGuid() }) };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var handler = new FormsOutboxHandler(new FormsApiClient(http));
        var store = new InMemoryOutboxStore();
        var request = new CreateFormSubmissionRequest(
            Guid.NewGuid(), "Organisation", null, null,
            Array.Empty<FormAnswerDto>(), Array.Empty<FormSubmissionFileInput>(), ClientId: Guid.NewGuid());
        var item = await store.EnqueueAsync(new OutboxItem
        {
            Id = request.ClientId!.Value,
            Kind = FormsOutboxHandler.ItemKind,
            PayloadJson = JsonSerializer.Serialize(request),
        });

        await handler.ExecuteAsync(item, store);

        Assert.True(posted);
    }
}
