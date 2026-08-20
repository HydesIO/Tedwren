using System.Collections.Concurrent;
using Tedwren.Application.Qualifications;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock qualification repositories (API <c>DataSource=Mock</c>).
/// Registered as a singleton so the type/card/requirement repositories share one dataset. Seeded with
/// the default library (SF-12), default trade requirements (SF-11) and a small set of confirmed cards for the
/// demo operatives (SF-5/SF-6) — one expiring within the warning window (SF-8/SF-9) — so compliance roll-ups
/// and the expiry digest have honest, non-empty data.
/// </summary>
public sealed class InMemoryQualificationStore
{
    /// <summary>Qualification types by id.</summary>
    public ConcurrentDictionary<Guid, QualificationType> Types { get; } = new();

    /// <summary>Qualification cards by id.</summary>
    public ConcurrentDictionary<Guid, QualificationCard> Cards { get; } = new();

    /// <summary>Trade requirements (small list; scanned per lookup).</summary>
    public IReadOnlyList<TradeQualificationRequirement> TradeRequirements { get; }

    /// <summary>Creates the store with the full demo seed — the default library <em>and</em> the demo operatives'
    /// cards. This is the constructor DI resolves for the mock API host; unit tests call <c>(bool seed)</c> to
    /// get the library without the demo cards (which would otherwise pollute global card scans).</summary>
    public InMemoryQualificationStore() : this(seed: true)
    {
        SeedCards();
    }

    /// <summary>Creates the store, optionally loading the default library — types (SF-12) and trade requirements
    /// (SF-11) — but <b>not</b> the demo cards. Tests pass false for a fully clean store.</summary>
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

    /// <summary>Seeds a few confirmed cards per demo operative, including one expiring soon (SF-9 warning window).</summary>
    private void SeedCards()
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        Guid TypeId(string name) => DefaultQualificationLibrary.Types.First(t => t.Name == name).Id;
        var cscs = TypeId("CSCS Card");

        // Meridian operatives.
        AddCard(DemoSeed.FletcherPersonId, cscs, "James Fletcher", today.AddMonths(30));
        AddCard(DemoSeed.FletcherPersonId, TypeId("First Aid at Work"), "James Fletcher", today.AddDays(40));   // expiring soon
        AddCard(DemoSeed.MarshPersonId, cscs, "Daniel Marsh", today.AddMonths(20));
        AddCard(DemoSeed.MarshPersonId, TypeId("SSSTS"), "Daniel Marsh", today.AddMonths(24));
        AddCard(DemoSeed.MarshPersonId, TypeId("First Aid at Work"), "Daniel Marsh", today.AddMonths(10));

        // Apex operatives (the subcontractor's compliance, surfaced in its packs and expiry digest, SUB-5).
        AddCard(DemoSeed.OkaforPersonId, cscs, "Samuel Okafor", today.AddMonths(18));
        AddCard(DemoSeed.OkaforPersonId, TypeId("Manual Handling"), "Samuel Okafor", today.AddMonths(14));
        AddCard(DemoSeed.ReyesPersonId, cscs, "Carlos Reyes", today.AddDays(25));                               // expiring soon

        // Kingsway operative.
        AddCard(DemoSeed.PearcePersonId, cscs, "Owen Pearce", today.AddMonths(22));
    }

    /// <summary>Adds one customer-confirmed card (SF-6) for a person, expiring on the given date (SF-8).</summary>
    private void AddCard(Guid personId, Guid typeId, string holderName, DateOnly expiresOn)
    {
        var card = new QualificationCard
        {
            PersonId = personId,
            QualificationTypeId = typeId,
            HolderName = holderName,
            IssuedOn = expiresOn.AddYears(-5),
            ExpiresOn = expiresOn,
            VerificationState = CardVerificationState.CustomerChecked,
            ConfirmedBy = "Demo Seed",
            ConfirmedUtc = DateTimeOffset.UtcNow.AddDays(-30),
        };
        Cards[card.Id] = card;
    }
}
