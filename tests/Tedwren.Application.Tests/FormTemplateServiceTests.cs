using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Forms;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the Forms Library template service (PRD-Phase 2 checklist/inspection engine): create → draft v1,
/// publish freezes, editing a published version creates a new draft (v2) while the published version stays
/// intact (R4/R10/R16), draft edits mutate in place, archive retires from the library, the list shows the
/// latest version per family, and one company can never see another's template (R15).
/// </summary>
public sealed class FormTemplateServiceTests
{
    private static readonly Guid Company = Guid.Parse("33333333-3333-4333-8333-000000000001");

    /// <summary>A fixed-tenant current-user stub so the service scopes to <see cref="Company"/> (R15).</summary>
    private sealed class StubCurrentUser : ICurrentUserService
    {
        private readonly Guid? _companyId;
        public StubCurrentUser(Guid? companyId) => _companyId = companyId;
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Test Admin", "Administrator", _companyId));
    }

    private static FormTemplateService CreateService(Guid? companyId, out InMemoryFormStore store)
    {
        store = new InMemoryFormStore(seed: false);
        return new FormTemplateService(new InMemoryFormTemplateRepository(store), new StubCurrentUser(companyId));
    }

    private static CreateFormTemplateRequest SampleCreate(string name = "Site Inspection") => new(
        Company, name, "A short site inspection.",
        new List<FormSectionDto>
        {
            new("s1", "General", new List<FormFieldDto>
            {
                new("f1", "RagStatus", "Housekeeping", null, true, null, null, 0),
                new("f2", "YesNo", "Fire exits clear", null, true, null, null, 1),
            }, 0),
        });

    [Fact] // Create yields a Draft version 1 the library lists.
    public async Task Create_ProducesDraftVersionOne()
    {
        var service = CreateService(Company, out _);

        var id = await service.CreateAsync(SampleCreate());
        var template = await service.GetTemplateAsync(id);

        Assert.NotNull(template);
        Assert.Equal("Draft", template!.Status);
        Assert.Equal(1, template.Version);
        Assert.Equal(2, template.Sections.Sum(s => s.Fields.Count));

        var list = await service.GetTemplatesAsync();
        Assert.Single(list);
    }

    [Fact] // A blank name is rejected.
    public async Task Create_BlankName_Throws()
    {
        var service = CreateService(Company, out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(SampleCreate(name: "   ")));
    }

    [Fact] // Publish freezes the draft; the fill endpoint then serves it.
    public async Task Publish_FreezesDraft_AndFillReturnsIt()
    {
        var service = CreateService(Company, out _);
        var id = await service.CreateAsync(SampleCreate());

        Assert.Null(await service.GetTemplateForFillAsync(id)); // not yet published

        var published = await service.PublishAsync(id);
        Assert.Equal("Published", published!.Status);
        Assert.NotNull(await service.GetTemplateForFillAsync(id));
    }

    [Fact] // R4/R10/R16 — editing a published version creates a new draft v2; v1 stays intact.
    public async Task Update_PublishedTemplate_CreatesNewDraftVersion()
    {
        var service = CreateService(Company, out _);
        var v1Id = await service.CreateAsync(SampleCreate());
        await service.PublishAsync(v1Id);

        var v2 = await service.UpdateAsync(v1Id, new UpdateFormTemplateRequest(
            "Site Inspection v2", "Revised.", SampleCreate().Sections));

        Assert.NotNull(v2);
        Assert.Equal(2, v2!.Version);
        Assert.Equal("Draft", v2.Status);
        Assert.NotEqual(v1Id, v2.Id);

        // The published v1 is untouched.
        var v1 = await service.GetTemplateAsync(v1Id);
        Assert.Equal("Published", v1!.Status);
        Assert.Equal("Site Inspection", v1.Name);

        // The library shows the latest version of the family only.
        var list = await service.GetTemplatesAsync();
        var row = Assert.Single(list);
        Assert.Equal(2, row.Version);
    }

    [Fact] // A draft edit mutates in place (no new version).
    public async Task Update_Draft_MutatesInPlace()
    {
        var service = CreateService(Company, out _);
        var id = await service.CreateAsync(SampleCreate());

        var updated = await service.UpdateAsync(id, new UpdateFormTemplateRequest(
            "Renamed", null, SampleCreate().Sections));

        Assert.Equal(id, updated!.Id);
        Assert.Equal(1, updated.Version);
        Assert.Equal("Renamed", updated.Name);
    }

    [Fact] // Archive removes the family from the library listing.
    public async Task Archive_RemovesFromLibrary()
    {
        var service = CreateService(Company, out _);
        var id = await service.CreateAsync(SampleCreate());
        await service.PublishAsync(id);

        var archived = await service.ArchiveAsync(id);
        Assert.Equal("Archived", archived!.Status);
        Assert.Empty(await service.GetTemplatesAsync());
    }

    [Fact] // R15 — a template owned by another company is invisible (treated as not found).
    public async Task GetTemplate_CrossTenant_ReturnsNull()
    {
        var owner = CreateService(Company, out var store);
        var id = await owner.CreateAsync(SampleCreate());

        var otherCompany = Guid.Parse("44444444-4444-4444-8444-000000000002");
        var intruder = new FormTemplateService(new InMemoryFormTemplateRepository(store),
            new StubCurrentUser(otherCompany));

        Assert.Null(await intruder.GetTemplateAsync(id));
        Assert.Empty(await intruder.GetTemplatesAsync());
    }
}
