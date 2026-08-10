using System.Collections.Concurrent;
using Tedwren.Application.Qualifications;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock qualification repositories (API <c>DataSource=Mock</c>).
/// Registered as a singleton so the type/card/requirement repositories share one dataset. Seeded with
/// the default library (SF-12) and default trade requirements (SF-11); cards start empty and are added
/// through capture, so holder counts are honest (zero until a card is captured).
/// </summary>
public sealed class InMemoryQualificationStore
{
    /// <summary>Qualification types by id.</summary>
    public ConcurrentDictionary<Guid, QualificationType> Types { get; } = new();

    /// <summary>Qualification cards by id.</summary>
    public ConcurrentDictionary<Guid, QualificationCard> Cards { get; } = new();

    /// <summary>Trade requirements (small list; scanned per lookup).</summary>
    public IReadOnlyList<TradeQualificationRequirement> TradeRequirements { get; }

    /// <summary>Creates the store and loads the default library seed.</summary>
    public InMemoryQualificationStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the default seed (tests pass false for a clean store).</summary>
    public InMemoryQualificationStore(bool seed)
    {
        if (!seed)
        {
            TradeRequirements = Array.Empty<TradeQualificationRequirement>();
            return;
        }

        foreach (var type in DefaultQualificationLibrary.Types)
        {
            Types[type.Id] = type;
        }

        TradeRequirements = DefaultQualificationLibrary.TradeRequirements;
    }
}
