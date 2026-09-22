using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Sync;
using Tedwren.Mobile.Core.Tests.Api;

namespace Tedwren.Mobile.Core.Tests.Sync;

/// <summary>Verifies the capture outbox handlers (M5) checkpoint the upload — the photo uploads once even when the submit is retried.</summary>
public class CaptureHandlerTests
{
    [Fact] // Gate 3 accreditation upload: the card photo uploads once even when the submit is retried.
    public async Task Card_handler_uploads_once_across_a_retried_submit()
    {
        var uploads = 0;
        var submits = 0;
        var http = FakeHttp.Routed(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/uploads", StringComparison.Ordinal))
            {
                uploads++;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new MobileUploadResultDto("img-9")) };
            }

            submits++;
            return submits == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent(string.Empty) }
                : new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(string.Empty) };
        });

        var handler = new CardOutboxHandler(new CaptureApiClient(http));
        var store = new InMemoryOutboxStore();
        var item = await store.EnqueueAsync(new OutboxItem
        {
            Id = Guid.NewGuid(),
            Kind = CardOutboxHandler.ItemKind,
            PhotoBytes = new byte[] { 4, 5, 6 },
            PhotoContentType = "image/jpeg",
            PayloadJson = JsonSerializer.Serialize(new MobileCaptureCardRequest(
                Guid.NewGuid(), Guid.NewGuid(), "G1", "Sam Taylor", null, null, null, DateTimeOffset.UtcNow)),
        });

        // First attempt: uploads the photo (checkpoint persisted), then the card POST fails (503) and throws.
        await Assert.ThrowsAsync<HttpRequestException>(() => handler.ExecuteAsync(item, store));
        Assert.Equal("img-9", item.UploadedImageReference);
        Assert.Null(item.PhotoBytes);

        // Second attempt: the upload is skipped (already checkpointed); the card POST now succeeds.
        await handler.ExecuteAsync(item, store);

        Assert.Equal(1, uploads);
        Assert.Equal(2, submits);
    }

    [Fact]
    public async Task Hazard_handler_uploads_once_across_a_retried_submit()
    {
        var uploads = 0;
        var reports = 0;
        var http = FakeHttp.Routed(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/uploads", StringComparison.Ordinal))
            {
                uploads++;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new MobileUploadResultDto("img-1")) };
            }

            reports++;
            return reports == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent(string.Empty) }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
        });

        var handler = new HazardOutboxHandler(new CaptureApiClient(http));
        var store = new InMemoryOutboxStore();
        var item = await store.EnqueueAsync(new OutboxItem
        {
            Id = Guid.NewGuid(),
            Kind = HazardOutboxHandler.ItemKind,
            PhotoBytes = new byte[] { 1, 2, 3 },
            PhotoContentType = "image/png",
            PayloadJson = JsonSerializer.Serialize(new MobileReportHazardRequest(
                Guid.NewGuid(), "NearMiss", "Scaffold tag missing", null, null, null, null, null, null, DateTimeOffset.UtcNow)),
        });

        // First attempt: uploads the photo (checkpoint persisted), then the report POST fails (503) and throws.
        await Assert.ThrowsAsync<HttpRequestException>(() => handler.ExecuteAsync(item, store));
        Assert.Equal("img-1", item.UploadedImageReference);
        Assert.Null(item.PhotoBytes);

        // Second attempt: the upload is skipped (already checkpointed); the report POST now succeeds.
        await handler.ExecuteAsync(item, store);

        Assert.Equal(1, uploads);
        Assert.Equal(2, reports);
    }
}
