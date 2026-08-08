using System.Collections.Concurrent;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock site repositories (API <c>DataSource=Mock</c>). Registered as a
/// singleton so the site and property repositories share one dataset. Seeded with a small demo dataset —
/// including one dispersed retrofit scheme with no compound (SF-25/SF-26) — so the API returns data.
/// </summary>
public sealed class InMemorySiteStore
{
    /// <summary>Sites by id.</summary>
    public ConcurrentDictionary<Guid, Site> Sites { get; } = new();

    /// <summary>Properties by id.</summary>
    public ConcurrentDictionary<Guid, SiteProperty> Properties { get; } = new();

    /// <summary>Creates the store and loads the demo seed.</summary>
    public InMemorySiteStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemorySiteStore(bool seed)
    {
        if (seed)
        {
            Seed();
        }
    }

    /// <summary>Loads a small, deterministic demo dataset (a boundaried site and a dispersed no-compound scheme).</summary>
    private void Seed()
    {
        var ownerCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

        var tower = new Site
        {
            CompanyId = ownerCompanyId,
            Name = "Meridian Tower",
            Client = "Meridian Estates",
            Region = "London",
            Address = "Meridian Tower, London",
            HasCompound = true,
            Boundary = new Geofence(51.5074, -0.1278, 150),
        };
        Sites[tower.Id] = tower;

        // A retrofit scheme: no compound, dispersed properties each with their own boundary (SF-25/SF-26).
        var retrofit = new Site
        {
            CompanyId = ownerCompanyId,
            Name = "Riverside Retrofit",
            Client = "Riverside Homes",
            Region = "Birmingham",
            Address = "Riverside estate, Birmingham",
            HasCompound = false,
            IsDispersed = true,
        };
        Sites[retrofit.Id] = retrofit;
        AddProperty(retrofit.Id, "1–12 Riverside Gardens", 12, new Geofence(52.4862, -1.8904, 60));
        AddProperty(retrofit.Id, "13–28 Riverside Gardens", 16, new Geofence(52.4870, -1.8890, 60));
    }

    /// <summary>Adds a seed property.</summary>
    private void AddProperty(Guid siteId, string address, int units, Geofence boundary)
    {
        var property = new SiteProperty { SiteId = siteId, Address = address, Units = units, Boundary = boundary };
        Properties[property.Id] = property;
    }
}
