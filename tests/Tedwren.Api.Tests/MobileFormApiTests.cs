using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the M6 operative forms surface (<c>/api/mobile/forms/*</c>): assignments-for-me, published-template
/// fetch, and the idempotent operative submit that reuses the existing engine. Confirms the module gate (Q2),
/// console-token rejection (RequireOperative) and that a mobile submit lands in the same register the console reads.
/// </summary>
public sealed class MobileFormApiTests
{
    [Fact]
    public async Task Assignments_and_template_and_submit_flow_reuses_the_engine()
    {
        var (factory, token, companyId, personId) = CreateOperativeHost();
        using (factory)
        {
            EnableForms(factory, companyId, true);
            var templateVersionId = SeedPublishedFormWithAssignment(factory, companyId);
            var client = Authed(factory, token);

            // Assignments-for-me: the Organisation-scope form resolves to its published version.
            var assignments = await client.GetFromJsonAsync<List<MobileFormAssignmentDto>>("/api/mobile/forms/assignments");
            Assert.Single(assignments!);
            Assert.Equal(templateVersionId, assignments![0].TemplateVersionId);

            // Fetch the published template to fill.
            var template = await client.GetAsync($"/api/mobile/forms/templates/{templateVersionId}");
            Assert.Equal(HttpStatusCode.OK, template.StatusCode);
            var dto = await template.Content.ReadFromJsonAsync<FormTemplateDto>();
            Assert.Equal(templateVersionId, dto!.Id);

            // Submit twice with the same client id → one submission (idempotent), visible in the company register.
            var clientId = Guid.NewGuid();
            var request = new CreateFormSubmissionRequest(
                templateVersionId, "Organisation", null, null,
                new List<FormAnswerDto> { new("f1", "Green", Array.Empty<string>()) },
                Array.Empty<FormSubmissionFileInput>(),
                ClientId: clientId);

            var first = await client.PostAsJsonAsync("/api/mobile/forms/submissions", request);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            await client.PostAsJsonAsync("/api/mobile/forms/submissions", request);

            using var scope = factory.Services.CreateScope();
            var register = await scope.ServiceProvider.GetRequiredService<IFormSubmissionRepository>().GetByCompanyAsync(companyId);
            Assert.Single(register);
            Assert.Equal(clientId, register[0].Id);
            Assert.Equal(personId, register[0].PersonId);
        }
    }

    [Fact]
    public async Task Forms_are_forbidden_without_the_forms_module() // Q2 fail-closed
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            EnableForms(factory, companyId, false);
            var client = Authed(factory, token);
            var response = await client.GetAsync("/api/mobile/forms/assignments");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Forms_are_rejected_for_a_console_token()
    {
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().GetAsync("/api/mobile/forms/assignments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- helpers ----------------------------------------------------------------------------------------

    private static HttpClient Authed(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static void EnableForms(WebApplicationFactory<Program> factory, Guid companyId, bool enabled)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IEntitlementService>()
            .SetEnabledAsync(companyId, "forms", enabled).GetAwaiter().GetResult();
    }

    /// <summary>Seeds a published form family + an Organisation-scope assignment (via repos, bypassing the tenant claim), returns the version id.</summary>
    private static Guid SeedPublishedFormWithAssignment(WebApplicationFactory<Program> factory, Guid companyId)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var familyId = Guid.NewGuid();
        var template = new FormTemplate
        {
            CompanyId = companyId,
            FamilyId = familyId,
            Name = "Site Inspection",
            Version = 1,
            Status = FormTemplateStatus.Published,
            Sections = new List<FormSectionDef>
            {
                new("s1", "General", new List<FormField>
                {
                    new("f1", FormFieldKind.RagStatus, "Housekeeping", null, true, null, null, 0),
                }, 0),
            },
        };
        sp.GetRequiredService<IFormTemplateRepository>().AddAsync(template).GetAwaiter().GetResult();
        sp.GetRequiredService<IFormAssignmentRepository>().AddAsync(new FormAssignment
        {
            CompanyId = companyId,
            FormTemplateFamilyId = familyId,
            FormName = "Site Inspection",
            Scope = FormScope.Organisation,
            Schedule = FormSchedule.Daily,
        }).GetAwaiter().GetResult();
        return template.Id;
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

            var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900700") };
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
