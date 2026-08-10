using System.Collections.Concurrent;
using Tedwren.Application.Inductions;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock induction repositories (API <c>DataSource=Mock</c>). Registered as a
/// singleton. Seeded with the default template so an induction can be run without any configuration.
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
        if (seed)
        {
            Templates[DefaultInductionTemplate.TemplateId] = DefaultInductionTemplate.Template;
        }
    }
}
