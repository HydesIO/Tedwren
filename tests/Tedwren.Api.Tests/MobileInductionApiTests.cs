using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the operative induction surface (<c>/api/mobile/inductions/*</c>, Gate 4): an operative resumes/starts
/// their main-contractor induction, completes the steps, passes the server-scored quiz (R5) and finalises to
/// receive the induction number — all bound to the operative token (R15). Also confirms a console token is rejected.
/// </summary>
public sealed class MobileInductionApiTests
{
    // The default induction template's quiz answers (cloned from the shipped default): q1=0, q2=1, q3=2.
    private static readonly Dictionary<string, int> CorrectAnswers = new() { ["q1"] = 0, ["q2"] = 1, ["q3"] = 2 };

    [Fact]
    public async Task Operative_completes_the_induction_and_receives_a_number()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            SeedCompanyInduction(factory, companyId);
            var client = Authed(factory, token);

            // Resolve/start the operative's induction.
            var start = await client.GetAsync("/api/mobile/inductions/current");
            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
            var session = await start.Content.ReadFromJsonAsync<InductionSessionDto>();
            Assert.NotNull(session);

            // Complete every step, then pass the quiz.
            foreach (var step in session!.Steps)
            {
                var stepResponse = await client.PostAsync($"/api/mobile/inductions/{session.Id}/steps/{step.Id}/complete", content: null);
                Assert.Equal(HttpStatusCode.OK, stepResponse.StatusCode);
            }

            var quiz = await client.PostAsJsonAsync($"/api/mobile/inductions/{session.Id}/quiz", new SubmitQuizRequest(CorrectAnswers));
            var quizResult = await quiz.Content.ReadFromJsonAsync<QuizResultDto>();
            Assert.True(quizResult!.Passed);

            // Finalise → the induction number is issued.
            var finalize = await client.PostAsJsonAsync($"/api/mobile/inductions/{session.Id}/finalize", new FinalizeInductionRequest("Alex Operative", true));
            Assert.Equal(HttpStatusCode.OK, finalize.StatusCode);
            var done = await finalize.Content.ReadFromJsonAsync<InductionSessionDto>();
            Assert.Equal("Passed", done!.Status);
            Assert.StartsWith("IND-", done.CompletionReference);
        }
    }

    [Fact]
    public async Task Induction_is_rejected_for_a_console_token()
    {
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().GetAsync("/api/mobile/inductions/current");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- helpers ----------------------------------------------------------------------------------------

    private static HttpClient Authed(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Seeds an induction template owned by the operative's company, so "current" resolves via the fallback.</summary>
    private static void SeedCompanyInduction(WebApplicationFactory<Program> factory, Guid companyId)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IInductionService>()
            .CreateDefaultTemplateAsync(new CreateInductionTemplateRequest(companyId, "Site induction", 365, 3))
            .GetAwaiter().GetResult();
    }

    private static (WebApplicationFactory<Program> Factory, string Token, Guid CompanyId, Guid PersonId) CreateOperativeHost()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Auth:TestBypass", "false"));

        Guid companyId;
        Guid personId;
        using (var scope = factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var company = new Company { Name = "Acme Subcontractor", OrgType = OrgType.Subcontractor };
            sp.GetRequiredService<ICompanyRepository>().AddAsync(company).GetAwaiter().GetResult();

            var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900701") };
            sp.GetRequiredService<IPersonRepository>().AddAsync(person).GetAwaiter().GetResult();

            sp.GetRequiredService<IEngagementRepository>()
                .AddAsync(new Engagement { CompanyId = company.Id, PersonId = person.Id, Name = "Alex Operative" })
                .GetAwaiter().GetResult();

            companyId = company.Id;
            personId = person.Id;
        }

        var token = new JwtOperativeTokenIssuer(new JwtOptions())
            .IssueAccessToken(personId, companyId, "Alex Operative", "device-1").Token;
        return (factory, token, companyId, personId);
    }
}
