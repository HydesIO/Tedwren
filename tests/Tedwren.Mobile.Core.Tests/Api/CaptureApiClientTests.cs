using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the capture client (M5): the multipart upload returns a reference; submits succeed on 200 and throw on failure.</summary>
public class CaptureApiClientTests
{
    [Fact]
    public async Task UploadPhoto_returns_the_reference()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(new MobileUploadResultDto("img-1")));
        var client = new CaptureApiClient(http);

        var reference = await client.UploadPhotoAsync(new byte[] { 1, 2, 3 }, "image/png");

        Assert.Equal("img-1", reference);
    }

    [Fact]
    public async Task UploadPhoto_throws_on_failure()
    {
        var http = FakeHttp.Returning(HttpStatusCode.InternalServerError, new StringContent(string.Empty));
        var client = new CaptureApiClient(http);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.UploadPhotoAsync(new byte[] { 1 }, "image/png"));
    }

    [Fact]
    public async Task ReportEvidence_succeeds_on_ok()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK,
            JsonContent.Create(new EvidenceItemDto(Guid.NewGuid(), null, null, null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
        var client = new CaptureApiClient(http);

        await client.ReportEvidenceAsync(new MobileReportEvidenceRequest(Guid.NewGuid(), "note", null, null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task ReportHazard_throws_on_forbidden()
    {
        var http = FakeHttp.Returning(HttpStatusCode.Forbidden, new StringContent(string.Empty));
        var client = new CaptureApiClient(http);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.ReportHazardAsync(new MobileReportHazardRequest(
                Guid.NewGuid(), "NearMiss", "desc", null, null, null, null, null, null, DateTimeOffset.UtcNow)));
    }
}
