using Dapper;
using Tedwren.Abstractions.Configuration;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Entities;
using Tedwren.Domain.ValueObjects;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration tests for the Dapper repositories against a real SQL Server. They run only when the
/// TEDWREN_TEST_SQLSERVER environment variable supplies a connection string (CI / dev with LocalDB or a
/// shared instance); otherwise each test is skipped. All rows are purpose-created with fresh GUIDs and
/// deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class OrganisationRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    /// <summary>Builds a connection factory for the configured test SQL Server.</summary>
    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Engagement_RoundTrips_AndPhoneNumberIsUniquePerPerson()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(factory, new SqlServerDialect()).RunAsync();

        var companies = new CompanyRepository(factory);
        var people = new PersonRepository(factory);
        var engagements = new EngagementRepository(factory);

        var company = new Company { Name = "IntegrationTest " + Guid.NewGuid() };
        var phone = PhoneNumber.Parse("+4477009" + Random.Shared.Next(0, 99999).ToString("D5"));
        var person = new Person { PhoneNumber = phone };
        var engagement = new Engagement { CompanyId = company.Id, PersonId = person.Id, Name = "Test Operative", Trade = "Groundworks" };

        try
        {
            await companies.AddAsync(company);
            await people.AddAsync(person);
            await engagements.AddAsync(engagement);

            var active = await engagements.GetActiveByCompanyAsync(company.Id);
            Assert.Contains(active, e => e.Id == engagement.Id);

            var byPhone = await people.GetByPhoneAsync(phone);
            Assert.NotNull(byPhone);
            Assert.Equal(person.Id, byPhone!.Id);

            // SF-1: the unique index rejects a second person with the same number.
            await Assert.ThrowsAnyAsync<Exception>(() => people.AddAsync(new Person { PhoneNumber = phone }));
        }
        finally
        {
            await CleanupAsync(factory, company.Id, person.Id);
        }
    }

    /// <summary>Removes the purpose-created rows (child engagements first for the foreign keys).</summary>
    private static async Task CleanupAsync(DbConnectionFactory factory, Guid companyId, Guid personId)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync("DELETE FROM Engagements WHERE CompanyId = @companyId", new { companyId });
        await connection.ExecuteAsync("DELETE FROM Persons WHERE Id = @personId", new { personId });
        await connection.ExecuteAsync("DELETE FROM Companies WHERE Id = @companyId", new { companyId });
    }
}
