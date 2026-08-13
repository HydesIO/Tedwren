using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock form-template repository (the test-only <c>DataSource=InMemory</c>
/// path). Registered as a singleton so the repository sees one dataset across requests. Tests construct it
/// with <c>seed:false</c> for a clean store; the default seeds a small deterministic demo form so the API
/// returns data when run in-memory.
/// </summary>
public sealed class InMemoryFormStore
{
    /// <summary>Form template versions by id.</summary>
    public ConcurrentDictionary<Guid, FormTemplate> Templates { get; } = new();

    /// <summary>Form submissions by id.</summary>
    public ConcurrentDictionary<Guid, FormSubmission> Submissions { get; } = new();

    /// <summary>Captured submission files by id.</summary>
    public ConcurrentDictionary<Guid, FormSubmissionFile> Files { get; } = new();

    /// <summary>Creates the store and loads the demo seed.</summary>
    public InMemoryFormStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemoryFormStore(bool seed)
    {
        if (seed)
        {
            Seed();
        }
    }

    /// <summary>Loads a small, deterministic demo form (a published welfare check) so the in-memory API has data.</summary>
    private void Seed()
    {
        var ownerCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");
        var template = new FormTemplate
        {
            CompanyId = ownerCompanyId,
            Name = "Daily Welfare Check",
            Description = "A short daily welfare and housekeeping check.",
            Status = Domain.Enums.FormTemplateStatus.Published,
            Sections = new List<FormSectionDef>
            {
                new("s1", "Welfare", new List<FormField>
                {
                    new("f1", Domain.Enums.FormFieldKind.RagStatus, "Toilets clean and stocked", null, true, null, null, 0),
                    new("f2", Domain.Enums.FormFieldKind.YesNo, "Drinking water available", null, true, null, null, 1),
                    new("f3", Domain.Enums.FormFieldKind.LongText, "Notes", "Anything to flag", false, null, null, 2),
                }, 0),
            },
        };
        Templates[template.Id] = template;
    }
}
