using Dapper;
using Tedwren.Abstractions.Configuration;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration tests for the Dapper qualification repositories against a real SQL Server. They run only
/// when TEDWREN_TEST_SQLSERVER supplies a connection string (CI / dev); otherwise each test is skipped.
/// All rows are purpose-created with fresh GUIDs and deleted afterwards, so existing data is untouched.
/// </summary>
public sealed class QualificationRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Card_RoundTrips_AndRenewalSupersedesWhileRetaining()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);

        var types = new QualificationTypeRepository(factory);
        var cards = new QualificationCardRepository(factory);

        var type = new QualificationType { Name = "IntegrationTest " + Guid.NewGuid(), DefaultValidityMonths = 60 };
        var personId = Guid.NewGuid();
        var card = new QualificationCard
        {
            PersonId = personId,
            QualificationTypeId = type.Id,
            CardNumber = "CS-INT",
            ExpiresOn = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
            VerificationState = CardVerificationState.ReadUnchecked,
        };

        try
        {
            await types.AddAsync(type);
            await cards.AddAsync(card);

            var held = await cards.GetByPersonAsync(personId);
            Assert.Contains(held, c => c.Id == card.Id && c.CardNumber == "CS-INT");

            // SF-10: renew by marking the old card superseded — it must remain retrievable.
            card.SupersededByCardId = Guid.NewGuid();
            await cards.UpdateAsync(card);
            var reread = await cards.GetByIdAsync(card.Id);
            Assert.NotNull(reread);
            Assert.True(reread!.IsSuperseded);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM QualificationCards WHERE PersonId = @personId", new { personId });
            await connection.ExecuteAsync("DELETE FROM QualificationTypes WHERE Id = @id", new { id = type.Id });
        }
    }
}
