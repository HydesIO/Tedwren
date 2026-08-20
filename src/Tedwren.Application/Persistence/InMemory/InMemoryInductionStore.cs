using System.Collections.Concurrent;
using Tedwren.Application.Inductions;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock induction repositories (API <c>DataSource=Mock</c>). Registered as a
/// singleton. Seeded with the default template (MC-3) and one completed induction record for the main contractor
/// demo tenant (Meridian), so the induction records surface has data. Induction is a main-contractor feature
/// (§6.1, SUB-11) — the subcontractor tenants have none.
/// </summary>
public sealed class InMemoryInductionStore
{
    /// <summary>Templates by id.</summary>
    public ConcurrentDictionary<Guid, InductionTemplate> Templates { get; } = new();

    /// <summary>Sessions by id.</summary>
    public ConcurrentDictionary<Guid, InductionSession> Sessions { get; } = new();

    /// <summary>Creates the store and loads the default template.</summary>
    public InMemoryInductionStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the default template (tests pass false for a clean store).</summary>
    public InMemoryInductionStore(bool seed)
    {
        if (!seed)
        {
            return;
        }

        Templates[DefaultInductionTemplate.TemplateId] = DefaultInductionTemplate.Template;

        // A completed induction for a Meridian operative (James Fletcher), valid for the template's window (MC-7).
        var now = DateTimeOffset.UtcNow;
        var session = new InductionSession
        {
            TemplateId = DefaultInductionTemplate.TemplateId,
            CompanyId = DefaultInductionTemplate.DemoCompanyId,   // Meridian (MC-3)
            PersonId = DemoSeed.FletcherPersonId,
            PersonName = "James Fletcher",
            Status = InductionStatus.Passed,
            StartedUtc = now.AddDays(-14),
            CompletedUtc = now.AddDays(-14),
            CompletionReference = "IND-2026-000481",
            ExpiresUtc = now.AddDays(DefaultInductionTemplate.Template.ValidityDays - 14),
            AttemptCount = 1,
            LastScore = 3,
            CompletedStepIds = DefaultInductionTemplate.Template.Steps.Select(s => s.Id).ToList(),
            SignatureName = "James Fletcher",
            ConsentGiven = true,
        };
        Sessions[session.Id] = session;
    }
}
