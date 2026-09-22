using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Mobile.Core.Api;
using Tedwren.Web.App.Pages.Operative;

namespace Tedwren.Web.App.Tests;

/// <summary>bUnit render test for the operative induction page (Gate 4): it loads the current session and renders the steps, quiz and complete action.</summary>
public class InductionPageTests : TestContext
{
    /// <summary>An HttpClient that answers the "current induction" GET with a canned in-progress session.</summary>
    private static HttpClient StubReturning(InductionSessionDto session) =>
        new(new StubHandler(session)) { BaseAddress = new Uri("https://api.test/") };

    [Fact]
    public void Renders_the_induction_steps_quiz_and_complete_action()
    {
        var session = new InductionSessionDto(
            Guid.NewGuid(), Guid.NewGuid(), "Site induction", Guid.NewGuid(), "Alex Operative", "InProgress",
            new[] { new InductionStepDto("s1", "Declaration", "Read the site rules", true) },
            Array.Empty<string>(),
            new[] { new InductionQuizQuestionDto("q1", "Where do you sign in?", new[] { "Gate office", "The pub" }) },
            PassMark: 1, CompletionReference: null, ExpiresUtc: null, AttemptCount: 0);

        Services.AddSingleton(new InductionApiClient(StubReturning(session)));

        var cut = RenderComponent<Induction>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Site induction", cut.Markup);
            Assert.Contains("Read the site rules", cut.Markup);   // the step
            Assert.Contains("Where do you sign in?", cut.Markup);  // the quiz question
            Assert.Contains("Complete induction", cut.Markup);     // the finalise action
        });
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly InductionSessionDto _session;
        public StubHandler(InductionSessionDto session) => _session = session;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(_session), Encoding.UTF8, "application/json"),
            });
    }
}
