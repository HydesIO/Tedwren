using System.Collections.Concurrent;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock compliance-pack repository (API <c>DataSource=Mock</c>). Registered
/// as a singleton so packs and their access events persist across requests within the host.
/// </summary>
public sealed class InMemoryCompliancePackStore
{
    /// <summary>Packs by id.</summary>
    public ConcurrentDictionary<Guid, CompliancePack> Packs { get; } = new();

    /// <summary>Access events by id (append-only, SUB-20).</summary>
    public ConcurrentDictionary<Guid, PackAccessEvent> AccessEvents { get; } = new();
}
