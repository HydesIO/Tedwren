using System.Collections.Concurrent;
using Tedwren.Application.Auth;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock repositories (API <c>DataSource=Mock</c>). Registered as a
/// singleton so the company/person/engagement repositories operate on one consistent dataset. Seeded
/// with a small demo dataset so the API returns data without a database; it is deliberately minimal
/// (visual parity with the rich sample data is the client mock's job, not this store's).
/// </summary>
public sealed class InMemoryOrganisationStore
{
    /// <summary>Companies by id.</summary>
    public ConcurrentDictionary<Guid, Company> Companies { get; } = new();

    /// <summary>People by id.</summary>
    public ConcurrentDictionary<Guid, Person> People { get; } = new();

    /// <summary>Engagements by id.</summary>
    public ConcurrentDictionary<Guid, Engagement> Engagements { get; } = new();

    /// <summary>Company documents by id (SUB-4).</summary>
    public ConcurrentDictionary<Guid, CompanyDocument> CompanyDocuments { get; } = new();

    /// <summary>Creates the store and loads the demo seed.</summary>
    public InMemoryOrganisationStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemoryOrganisationStore(bool seed)
    {
        if (seed)
        {
            Seed();
        }
    }

    /// <summary>Loads a small, deterministic demo dataset (a few companies with operatives).</summary>
    private void Seed()
    {
        // The two demo tenants are deliberately independent (R15): the Main Contractor (Meridian) is the
        // bootstrap admin's tenant, and the Subcontractor (Apex) is its own separate tenant. They own their
        // own operatives and share no people, so neither demo account can surface the other's data.
        var meridian = AddCompany("Meridian Construction Ltd", "Main Contractor", "General Build", AdminUserSeeder.SeedCompanyId);
        var apex = AddCompany("Apex Groundworks", "Subcontractor", "Groundworks", AdminUserSeeder.SubcontractorSeedCompanyId);
        AddCompany("Kingsway M&E", "Subcontractor", "Mechanical & Electrical");

        // Main Contractor (Meridian) operatives — engaged only by Meridian.
        var fletcher = AddPerson("+447700900001");
        AddEngagement(meridian.Id, fletcher.Id, "James Fletcher", "Bricklayer");
        var marsh = AddPerson("+447700900002");
        AddEngagement(meridian.Id, marsh.Id, "Daniel Marsh", "Site Supervisor");

        // Subcontractor (Apex) operatives — a wholly separate set of people, so there is no cross-contamination
        // between the two demo accounts.
        var okafor = AddPerson("+447700900101");
        AddEngagement(apex.Id, okafor.Id, "Samuel Okafor", "Groundworker");
        var reyes = AddPerson("+447700900102");
        AddEngagement(apex.Id, reyes.Id, "Carlos Reyes", "Plant Operator");
    }

    /// <summary>Adds a seed company (optionally with a fixed id, for tenant alignment) and returns it.</summary>
    private Company AddCompany(string name, string type, string trade, Guid? id = null)
    {
        var company = id is null
            ? new Company { Name = name, Type = type, Trade = trade }
            : new Company { Id = id.Value, Name = name, Type = type, Trade = trade };
        Companies[company.Id] = company;
        return company;
    }

    /// <summary>Adds a seed person and returns it.</summary>
    private Person AddPerson(string mobile)
    {
        var person = new Person { PhoneNumber = PhoneNumber.Parse(mobile) };
        People[person.Id] = person;
        return person;
    }

    /// <summary>Adds a seed engagement and returns it.</summary>
    private Engagement AddEngagement(Guid companyId, Guid personId, string name, string trade)
    {
        var engagement = new Engagement { CompanyId = companyId, PersonId = personId, Name = name, Trade = trade };
        Engagements[engagement.Id] = engagement;
        return engagement;
    }
}
