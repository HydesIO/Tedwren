using Tedwren.Domain.ValueObjects;

namespace Tedwren.Domain.Entities;

/// <summary>
/// One property within a dispersed scheme (SF-26): its own address and its own boundary, grouped under a
/// single <see cref="Site"/>. Adding a property must require nothing to be printed, installed, delivered or
/// attached at that property — it is a pure data addition here.
/// </summary>
public sealed class SiteProperty
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The scheme (site) this property belongs to.</summary>
    public required Guid SiteId { get; init; }

    /// <summary>The property's address.</summary>
    public required string Address { get; set; }

    /// <summary>Number of units at the property (e.g. dwellings), if relevant.</summary>
    public int Units { get; set; }

    /// <summary>The property's own boundary, used to verify sign-in at this property (SF-14/SF-26).</summary>
    public required Geofence Boundary { get; set; }

    /// <summary>When the property record was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
