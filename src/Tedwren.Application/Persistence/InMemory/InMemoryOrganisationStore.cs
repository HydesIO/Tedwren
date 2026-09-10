using System.Collections.Concurrent;
using Tedwren.Application.Auth;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
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
        var kingsway = AddCompany("Kingsway M&E", "Subcontractor", "Mechanical & Electrical");

        // Main Contractor (Meridian) operatives — engaged only by Meridian. People carry fixed demo ids so the
        // attendance/decision/induction/card seeds in the other stores line up to these same operatives.
        var fletcher = AddPerson("+447700900001", DemoSeed.FletcherPersonId);
        AddEngagement(meridian.Id, fletcher.Id, "James Fletcher", "Bricklayer");
        var marsh = AddPerson("+447700900002", DemoSeed.MarshPersonId);
        AddEngagement(meridian.Id, marsh.Id, "Daniel Marsh", "Site Supervisor");

        // Subcontractor (Apex) operatives — a wholly separate set of people, so there is no cross-contamination
        // between the two demo accounts.
        var okafor = AddPerson("+447700900101", DemoSeed.OkaforPersonId);
        AddEngagement(apex.Id, okafor.Id, "Samuel Okafor", "Groundworker");
        var reyes = AddPerson("+447700900102", DemoSeed.ReyesPersonId);
        AddEngagement(apex.Id, reyes.Id, "Carlos Reyes", "Plant Operator");

        // Second subcontractor (Kingsway) — a live tenant, not an empty shell.
        var pearce = AddPerson("+447700900201", DemoSeed.PearcePersonId);
        AddEngagement(kingsway.Id, pearce.Id, "Owen Pearce", "Electrician");

        // Company documents — insurances / accreditations (SUB-4). These are a subcontractor feature (they
        // populate the compliance pack), so the subcontractor tenants carry them, with a spread of expiries so
        // the roll-up shows valid / expiring-soon / lapsed states. Dates are relative to "today" so the demo
        // stays fresh regardless of when it runs.
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        AddDocument(apex.Id, "Employer's Liability Insurance", "Insurance", today.AddMonths(8), "ELI-2291043");
        AddDocument(apex.Id, "Public Liability Insurance", "Insurance", today.AddDays(21), "PLI-771208");   // expiring soon
        AddDocument(apex.Id, "CHAS Accreditation", "Accreditation", today.AddMonths(4), "CHAS-55120");
        AddDocument(apex.Id, "Constructionline Gold", "Accreditation", today.AddDays(-9), "CL-338841");     // lapsed
        AddDocument(kingsway.Id, "Employer's Liability Insurance", "Insurance", today.AddMonths(5), "ELI-6640021");
        AddDocument(kingsway.Id, "SafeContractor", "Accreditation", today.AddMonths(2), "SC-20981");
    }

    /// <summary>Adds a seed company (optionally with a fixed id, for tenant alignment) and returns it. The
    /// typed product (SF-22/SUB-24/MC-23) is derived from the free-text <paramref name="type"/> so the two
    /// demo tenants render as their real products.</summary>
    private Company AddCompany(string name, string type, string trade, Guid? id = null)
    {
        var orgType = type.Replace(" ", string.Empty).Equals("MainContractor", StringComparison.OrdinalIgnoreCase)
            ? OrgType.MainContractor
            : OrgType.Subcontractor;
        var company = id is null
            ? new Company { Name = name, Type = type, Trade = trade, OrgType = orgType }
            : new Company { Id = id.Value, Name = name, Type = type, Trade = trade, OrgType = orgType };
        Companies[company.Id] = company;
        return company;
    }

    /// <summary>Adds a seed person (optionally with a fixed id, so other stores' seeds can reference it) and returns it.</summary>
    private Person AddPerson(string mobile, Guid? id = null)
    {
        var person = id is null
            ? new Person { PhoneNumber = PhoneNumber.Parse(mobile) }
            : new Person { Id = id.Value, PhoneNumber = PhoneNumber.Parse(mobile) };
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

    /// <summary>Adds a seed company document — an insurance/accreditation/policy (SUB-4).</summary>
    private void AddDocument(Guid companyId, string name, string type, DateOnly expiresOn, string reference)
    {
        var document = new CompanyDocument
        {
            CompanyId = companyId,
            Name = name,
            Type = type,
            ExpiresOn = expiresOn,
            Reference = reference,
        };
        CompanyDocuments[document.Id] = document;
    }
}
