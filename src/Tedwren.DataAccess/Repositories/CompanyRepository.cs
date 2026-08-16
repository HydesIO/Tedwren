using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ICompanyRepository"/>. SQL is ANSI-portable across SQL Server and PostgreSQL.</summary>
public sealed class CompanyRepository : RepositoryBase, ICompanyRepository
{
    private const string Columns =
        "Id, Name, Type, Trade, RegistrationNumber, Address, ContactName, ContactEmail, ContactPhone, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public CompanyRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Deletes a company by id (demo-data teardown).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM Companies WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Returns all companies ordered by name.</summary>
    public async Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<CompanyRow>($"SELECT {Columns} FROM Companies ORDER BY Name", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a company by id, or null.</summary>
    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<CompanyRow>(
            $"SELECT {Columns} FROM Companies WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Inserts a new company.</summary>
    public Task AddAsync(Company company, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Companies (Id, Name, Type, Trade, RegistrationNumber, Address, ContactName, ContactEmail, ContactPhone, CreatedUtc) " +
            "VALUES (@Id, @Name, @Type, @Trade, @RegistrationNumber, @Address, @ContactName, @ContactEmail, @ContactPhone, @CreatedUtc)",
            new
            {
                company.Id, company.Name, company.Type, company.Trade, company.RegistrationNumber,
                company.Address, company.ContactName, company.ContactEmail, company.ContactPhone, company.CreatedUtc,
            },
            cancellationToken);

    /// <summary>Updates an existing company's editable fields.</summary>
    public Task UpdateAsync(Company company, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Companies SET Name = @Name, Type = @Type, Trade = @Trade, RegistrationNumber = @RegistrationNumber, " +
            "Address = @Address, ContactName = @ContactName, ContactEmail = @ContactEmail, ContactPhone = @ContactPhone " +
            "WHERE Id = @Id",
            new
            {
                company.Id, company.Name, company.Type, company.Trade, company.RegistrationNumber,
                company.Address, company.ContactName, company.ContactEmail, company.ContactPhone,
            },
            cancellationToken);

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static Company ToEntity(CompanyRow r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Type = r.Type,
        Trade = r.Trade,
        RegistrationNumber = r.RegistrationNumber,
        Address = r.Address,
        ContactName = r.ContactName,
        ContactEmail = r.ContactEmail,
        ContactPhone = r.ContactPhone,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record CompanyRow(
        Guid Id, string Name, string? Type, string? Trade, string? RegistrationNumber,
        string? Address, string? ContactName, string? ContactEmail, string? ContactPhone, DateTimeOffset CreatedUtc);
}
