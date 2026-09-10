using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IPersonRepository"/>. The mobile number is stored/queried as its normalised value.</summary>
public sealed class PersonRepository : RepositoryBase, IPersonRepository
{
    private const string Columns = "Id, PhoneNumber, EmergencyContactName, EmergencyContactPhone, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public PersonRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Deletes a person by id (demo-data teardown).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM Persons WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Returns the person holding this mobile number, or null (SF-1).</summary>
    public async Task<Person?> GetByPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<PersonRow>(
            $"SELECT {Columns} FROM Persons WHERE PhoneNumber = @PhoneNumber",
            new { PhoneNumber = phoneNumber.Value }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns a person by id, or null.</summary>
    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<PersonRow>(
            $"SELECT {Columns} FROM Persons WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new person.</summary>
    public Task AddAsync(Person person, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Persons (Id, PhoneNumber, EmergencyContactName, EmergencyContactPhone, CreatedUtc) " +
            "VALUES (@Id, @PhoneNumber, @EmergencyContactName, @EmergencyContactPhone, @CreatedUtc)",
            new
            {
                person.Id,
                PhoneNumber = person.PhoneNumber.Value,
                person.EmergencyContactName,
                person.EmergencyContactPhone,
                person.CreatedUtc,
            },
            cancellationToken);

    /// <summary>Persists an existing person's editable fields (emergency contact, UAT-010).</summary>
    public Task UpdateAsync(Person person, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Persons SET EmergencyContactName = @EmergencyContactName, EmergencyContactPhone = @EmergencyContactPhone WHERE Id = @Id",
            new { person.Id, person.EmergencyContactName, person.EmergencyContactPhone },
            cancellationToken);

    /// <summary>Maps a queried row to the domain entity, re-parsing the normalised number.</summary>
    private static Person ToEntity(PersonRow r) => new()
    {
        Id = r.Id,
        PhoneNumber = PhoneNumber.Parse(r.PhoneNumber),
        EmergencyContactName = r.EmergencyContactName,
        EmergencyContactPhone = r.EmergencyContactPhone,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record PersonRow(
        Guid Id, string PhoneNumber, string? EmergencyContactName, string? EmergencyContactPhone, DateTimeOffset CreatedUtc);
}
