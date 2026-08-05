using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IPersonRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryPersonRepository : IPersonRepository
{
    private readonly InMemoryOrganisationStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryPersonRepository(InMemoryOrganisationStore store) => _store = store;

    /// <summary>Returns the person holding this mobile number, or null (SF-1).</summary>
    public Task<Person?> GetByPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.People.Values.FirstOrDefault(p => p.PhoneNumber.Equals(phoneNumber)));

    /// <summary>Returns a person by id, or null.</summary>
    public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.People.GetValueOrDefault(id));

    /// <summary>Adds a person to the store.</summary>
    public Task AddAsync(Person person, CancellationToken cancellationToken = default)
    {
        _store.People[person.Id] = person;
        return Task.CompletedTask;
    }
}
