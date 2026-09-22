using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Qualifications;
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

    [Fact] // Gate 3: the accreditation-type picker reads the type library.
    public async Task GetCardTypes_returns_the_library()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(new[]
        {
            new QualificationTypeDto(Guid.NewGuid(), "Gas Safe", "Health & Safety", "Gas Safe Register", 60, false, 0),
        }));
        var client = new CaptureApiClient(http);

        var types = await client.GetCardTypesAsync();

        Assert.Equal("Gas Safe", Assert.Single(types).Name);
    }

    [Fact] // Gate 3: an accreditation-card submit succeeds on 200/201.
    public async Task UploadCard_succeeds_on_created()
    {
        var http = FakeHttp.Returning(HttpStatusCode.Created, new StringContent(string.Empty));
        var client = new CaptureApiClient(http);

        await client.UploadCardAsync(new MobileCaptureCardRequest(
            Guid.NewGuid(), Guid.NewGuid(), "G1", "Sam Taylor", null, null, "img-1", DateTimeOffset.UtcNow));
    }

    [Fact] // A rejected accreditation-card submit throws (the sync engine classifies transient vs permanent).
    public async Task UploadCard_throws_on_failure()
    {
        var http = FakeHttp.Returning(HttpStatusCode.InternalServerError, new StringContent(string.Empty));
        var client = new CaptureApiClient(http);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.UploadCardAsync(new MobileCaptureCardRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, DateTimeOffset.UtcNow)));
    }
}
