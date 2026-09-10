using System.Collections.Concurrent;
using Tedwren.Application.Auth;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock compliance-pack repository (API <c>DataSource=Mock</c>). Registered
/// as a singleton so packs and their access events persist across requests within the host. Sending compliance
/// packs is the subcontractor product's defining feature (SUB-13), so the demo pack belongs to the
/// subcontractor demo tenant (Apex Groundworks).
/// </summary>
public sealed class InMemoryCompliancePackStore
{
    /// <summary>Packs by id.</summary>
    public ConcurrentDictionary<Guid, CompliancePack> Packs { get; } = new();

    /// <summary>Access events by id (append-only, SUB-20).</summary>
    public ConcurrentDictionary<Guid, PackAccessEvent> AccessEvents { get; } = new();

    /// <summary>Creates the store and loads the demo seed.</summary>
    public InMemoryCompliancePackStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemoryCompliancePackStore(bool seed)
    {
        if (!seed)
        {
            return;
        }

        // One pack Apex has already sent (SUB-13), fixed at send (R7), with a recipient open recorded (SUB-20)
        // so the sender's "who opened it" view has data. Token/passcode are placeholders — the demo does not
        // exercise the recipient gate.
        var now = DateTimeOffset.UtcNow;
        var pack = new CompliancePack
        {
            CompanyId = AdminUserSeeder.SubcontractorSeedCompanyId,
            Title = "Weekly compliance pack — Meridian Tower",
            SiteName = "Meridian Tower",
            CreatedBy = "Grace Bello",
            CreatedUtc = now.AddDays(-3),
            ExpiresUtc = now.AddDays(27),
            Token = "demo-pack-apex-0001",
            PasscodeHash = "seeded-demo-passcode-hash",
            Status = PackStatus.Active,
            Subjects = new List<PackSubject>
            {
                new(Guid.NewGuid(), "Samuel Okafor", Array.Empty<PackCard>()),
                new(Guid.NewGuid(), "Carlos Reyes", Array.Empty<PackCard>()),
            },
        };
        Packs[pack.Id] = pack;

        var opened = new PackAccessEvent { PackId = pack.Id, Kind = PackAccessKind.Opened, OccurredUtc = now.AddDays(-2) };
        AccessEvents[opened.Id] = opened;
    }
}
