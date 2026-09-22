using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the operative induction client (Gate 4): it shapes the current/quiz/finalize JSON and maps "nothing assigned" (404) and "not ready" (409) to null.</summary>
public class InductionApiClientTests
{
    private static InductionSessionDto Session(string status = "InProgress", string? reference = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), "Site induction", Guid.NewGuid(), "Alex Operative", status,
        Array.Empty<InductionStepDto>(), Array.Empty<string>(), Array.Empty<InductionQuizQuestionDto>(),
        PassMark: 3, CompletionReference: reference, ExpiresUtc: null, AttemptCount: 0);

    [Fact]
    public async Task GetCurrent_returns_null_when_none_assigned()
    {
        var http = FakeHttp.Returning(HttpStatusCode.NotFound, new StringContent(string.Empty));
        var client = new InductionApiClient(http);

        Assert.Null(await client.GetCurrentAsync());
    }

    [Fact]
    public async Task GetCurrent_maps_the_session()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Session()));
        var client = new InductionApiClient(http);

        var session = await client.GetCurrentAsync();

        Assert.NotNull(session);
        Assert.Equal("Site induction", session!.TemplateName);
    }

    [Fact]
    public async Task SubmitQuiz_maps_the_result()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK,
            JsonContent.Create(new QuizResultDto(3, 3, Passed: true, AttemptCount: 1)));
        var client = new InductionApiClient(http);

        var result = await client.SubmitQuizAsync(Guid.NewGuid(), new SubmitQuizRequest(new Dictionary<string, int>()));

        Assert.True(result!.Passed);
        Assert.Equal(3, result.Correct);
    }

    [Fact]
    public async Task Finalize_returns_null_when_not_ready()
    {
        // The server answers 409 when required steps aren't done / the quiz isn't passed.
        var http = FakeHttp.Returning(HttpStatusCode.Conflict, new StringContent(string.Empty));
        var client = new InductionApiClient(http);

        Assert.Null(await client.FinalizeAsync(Guid.NewGuid(), new FinalizeInductionRequest("Alex Operative", true)));
    }

    [Fact]
    public async Task Finalize_maps_the_completed_session()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Session("Passed", "IND-ABCD1234")));
        var client = new InductionApiClient(http);

        var done = await client.FinalizeAsync(Guid.NewGuid(), new FinalizeInductionRequest("Alex Operative", true));

        Assert.Equal("Passed", done!.Status);
        Assert.Equal("IND-ABCD1234", done.CompletionReference);
    }
}
