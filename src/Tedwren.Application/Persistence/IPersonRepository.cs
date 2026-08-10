using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="Person"/>, keyed for lookup by normalised mobile number (SF-1).</summary>
public interface IPersonRepository
{
    /// <summary>Returns the person holding this mobile number, or null (SF-1 identity lookup).</summary>
    Task<Person?> GetByPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Returns a person by id, or null.</summary>
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new person.</summary>
    Task AddAsync(Person person, CancellationToken cancellationToken = default);
}
