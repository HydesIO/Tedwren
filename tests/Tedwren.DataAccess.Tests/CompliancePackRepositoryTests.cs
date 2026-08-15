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
/// Integration tests for the compliance-pack Dapper repository against a real SQL Server. Run only when
/// TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created and
/// deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class CompliancePackRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static DbConnectionFactory CreateFactory() => new(new SqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    [SkippableFact]
    public async Task Pack_RoundTrips_WithSnapshotStatusAndAccessEvents()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");

        var factory = CreateFactory();
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Product);
        var repository = new CompliancePackRepository(factory);

        var companyId = Guid.NewGuid();
        var token = "tok-" + Guid.NewGuid().ToString("N");
        var pack = new CompliancePack
        {
            CompanyId = companyId,
            Title = "Test Pack",
            CreatedBy = "Reviewer",
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
            Token = token,
            PasscodeHash = "salt:hash",
            Subjects = new[]
            {
                new PackSubject(Guid.NewGuid(), "Test Operative", new[]
                {
                    new PackCard("CSCS", "Valid", CardStatus.Valid, new DateOnly(2028, 1, 1), "Customer-checked"),
                }),
            },
        };

        try
        {
            await repository.AddAsync(pack);

            // The snapshot round-trips by token (the recipient's only handle, R9).
            var byToken = await repository.GetByTokenAsync(token);
            Assert.NotNull(byToken);
            var subject = Assert.Single(byToken!.Subjects);
            Assert.Equal("Test Operative", subject.Name);
            Assert.Single(subject.Cards);

            // Access events tally (SUB-20).
            await repository.AddAccessEventAsync(new PackAccessEvent { PackId = pack.Id, Kind = PackAccessKind.Opened });
            await repository.AddAccessEventAsync(new PackAccessEvent { PackId = pack.Id, Kind = PackAccessKind.Downloaded });
            var (opened, downloaded) = await repository.GetAccessTallyAsync(pack.Id);
            Assert.Equal(1, opened);
            Assert.Equal(1, downloaded);

            // Revoke persists (SUB-21).
            pack.Status = PackStatus.Revoked;
            await repository.UpdateStatusAsync(pack);
            var reloaded = await repository.GetAsync(companyId, pack.Id);
            Assert.Equal(PackStatus.Revoked, reloaded!.Status);
        }
        finally
        {
            using var connection = factory.Create();
            await connection.ExecuteAsync("DELETE FROM PackAccessEvents WHERE PackId = @id", new { id = pack.Id });
            await connection.ExecuteAsync("DELETE FROM CompliancePacks WHERE Id = @id", new { id = pack.Id });
        }
    }
}
