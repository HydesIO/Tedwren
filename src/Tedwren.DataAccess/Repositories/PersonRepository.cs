using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IPersonRepository"/>. The mobile number is stored/queried as its normalised value.</summary>
public sealed class PersonRepository : RepositoryBase, IPersonRepository
{
    /// <summary>Creates the repository over the connection factory.</summary>
    public PersonRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns the person holding this mobile number, or null (SF-1).</summary>
    public async Task<Person?> GetByPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<PersonRow>(
            "SELECT Id, PhoneNumber, CreatedUtc FROM Persons WHERE PhoneNumber = @PhoneNumber",
            new { PhoneNumber = phoneNumber.Value }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns a person by id, or null.</summary>
    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<PersonRow>(
            "SELECT Id, PhoneNumber, CreatedUtc FROM Persons WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new person.</summary>
    public Task AddAsync(Person person, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Persons (Id, PhoneNumber, CreatedUtc) VALUES (@Id, @PhoneNumber, @CreatedUtc)",
            new { person.Id, PhoneNumber = person.PhoneNumber.Value, person.CreatedUtc }, cancellationToken);

    /// <summary>Maps a queried row to the domain entity, re-parsing the normalised number.</summary>
    private static Person ToEntity(PersonRow r) => new()
    {
        Id = r.Id,
        PhoneNumber = PhoneNumber.Parse(r.PhoneNumber),
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record PersonRow(Guid Id, string PhoneNumber, DateTimeOffset CreatedUtc);
}
