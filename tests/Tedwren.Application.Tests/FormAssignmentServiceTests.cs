using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Forms;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the Forms Library assignment service (PRD-Phase 2, requirement 5): assign a form to the
/// organisation or a site (a site assignment requires a site), list assignments, and tenant isolation on
/// delete (R15).
/// </summary>
public sealed class FormAssignmentServiceTests
{
    private static readonly Guid Company = Guid.Parse("77777777-7777-4777-8777-000000000001");

    private sealed class StubCurrentUser : ICurrentUserService
    {
        private readonly Guid? _companyId;
        public StubCurrentUser(Guid? companyId) => _companyId = companyId;
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Admin", "Administrator", _companyId));
    }

    private static (FormTemplateService Templates, FormAssignmentService Assignments) CreateServices(Guid? companyId, out InMemoryFormStore store)
    {
        store = new InMemoryFormStore(seed: false);
        var templateRepo = new InMemoryFormTemplateRepository(store);
        var assignmentRepo = new InMemoryFormAssignmentRepository(store);
        var user = new StubCurrentUser(companyId);
        return (new FormTemplateService(templateRepo, user), new FormAssignmentService(assignmentRepo, templateRepo, user));
    }

    private static async Task<FormTemplateSummaryDto> PublishedFormAsync(FormTemplateService templates)
    {
        var id = await templates.CreateAsync(new CreateFormTemplateRequest(
            Company, "Daily Diary", null,
            new List<FormSectionDto> { new("s1", "General", new List<FormFieldDto>
            {
                new("f1", "ShortText", "Weather", null, true, null, null, 0),
            }, 0) }));
        await templates.PublishAsync(id);
        return (await templates.GetTemplatesAsync()).Single();
    }

    [Fact] // Requirement 5 — a form is assigned to the organisation and listed.
    public async Task Assign_Organisation_IsListed()
    {
        var (templates, assignments) = CreateServices(Company, out _);
        var form = await PublishedFormAsync(templates);

        await assignments.CreateAsync(new CreateFormAssignmentRequest(form.FamilyId, "Organisation", null, null, null, "Daily", "alerts@example.com"));

        var list = await assignments.GetAssignmentsAsync();
        var row = Assert.Single(list);
        Assert.Equal("Daily Diary", row.FormName);
        Assert.Equal("Organisation", row.Scope);
        Assert.Equal("Daily", row.Schedule);
        Assert.Equal("alerts@example.com", row.FailureAlertEmail);
    }

    [Fact] // A site assignment requires a site.
    public async Task Assign_Site_WithoutSite_Throws()
    {
        var (templates, assignments) = CreateServices(Company, out _);
        var form = await PublishedFormAsync(templates);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            assignments.CreateAsync(new CreateFormAssignmentRequest(form.FamilyId, "Site", null, null, null, "AdHoc", null)));
    }

    [Fact] // An unknown form family is rejected.
    public async Task Assign_UnknownForm_Throws()
    {
        var (_, assignments) = CreateServices(Company, out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            assignments.CreateAsync(new CreateFormAssignmentRequest(Guid.NewGuid(), "Organisation", null, null, null, "AdHoc", null)));
    }

    [Fact] // R15 — another company cannot delete this company's assignment.
    public async Task Delete_CrossTenant_ReturnsFalse()
    {
        var (templates, assignments) = CreateServices(Company, out var store);
        var form = await PublishedFormAsync(templates);
        var id = await assignments.CreateAsync(new CreateFormAssignmentRequest(form.FamilyId, "Organisation", null, null, null, "AdHoc", null));

        var otherCompany = Guid.Parse("88888888-8888-4888-8888-000000000002");
        var intruder = new FormAssignmentService(new InMemoryFormAssignmentRepository(store),
            new InMemoryFormTemplateRepository(store), new StubCurrentUser(otherCompany));

        Assert.False(await intruder.DeleteAsync(id));
        Assert.True(await assignments.DeleteAsync(id));   // the owner can
    }
}
