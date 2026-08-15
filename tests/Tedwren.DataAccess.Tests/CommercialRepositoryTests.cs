using Tedwren.Abstractions.Configuration;
using Tedwren.DataAccess;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;
using Tedwren.DataAccess.Migrations;
using Tedwren.DataAccess.Repositories;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// Integration tests for the Dapper <b>commercial-database</b> repositories (launch list, leads, affiliates)
/// against a real SQL Server, exercising the commercial migration area and the admin connection factory. Run
/// only when TEDWREN_TEST_SQLSERVER supplies a connection string; otherwise skipped. Rows are purpose-created
/// with fresh GUIDs and deleted afterwards, so existing data is never modified.
/// </summary>
public sealed class CommercialRepositoryTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEDWREN_TEST_SQLSERVER");

    private static AdminDbConnectionFactory CreateFactory() => new(new AdminSqlDataAccessOptions
    {
        Provider = DatabaseProvider.SqlServer,
        ConnectionString = ConnectionString!,
    });

    private static async Task MigrateAsync(AdminDbConnectionFactory factory) =>
        await new MigrationRunner(new SqlServerDialect()).RunAsync(factory, MigrationArea.Commercial);

    [SkippableFact]
    public async Task LaunchSignup_RoundTrips_WithDedupeUnsubscribeAndDelete()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");
        var factory = CreateFactory();
        await MigrateAsync(factory);
        var repo = new LaunchSignupRepository(factory);

        var email = $"rt-{Guid.NewGuid():N}@example.com";
        var signup = new LaunchSignup { Email = email, Source = "test", UnsubscribeToken = Guid.NewGuid().ToString("N") };
        try
        {
            await repo.AddAsync(signup);

            var byEmail = await repo.GetByEmailAsync(email.ToUpperInvariant());
            Assert.NotNull(byEmail);
            Assert.Equal(signup.Id, byEmail!.Id);

            var byToken = await repo.GetByUnsubscribeTokenAsync(signup.UnsubscribeToken!);
            Assert.Equal(signup.Id, byToken!.Id);

            signup.Notified = true;
            signup.NotifiedUtc = DateTimeOffset.UtcNow;
            signup.Unsubscribed = true;
            await repo.UpdateAsync(signup);
            var reloaded = await repo.GetByEmailAsync(email);
            Assert.True(reloaded!.Notified);
            Assert.True(reloaded.Unsubscribed);
        }
        finally
        {
            await repo.DeleteAsync(signup.Id);
        }

        Assert.Null(await repo.GetByEmailAsync(email));
    }

    [SkippableFact]
    public async Task Lead_RoundTrips_WithNotesAffiliateAndOpenLookup()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");
        var factory = CreateFactory();
        await MigrateAsync(factory);
        var repo = new LeadRepository(factory);

        var affiliateId = Guid.NewGuid();
        var lead = new Lead
        {
            CompanyName = $"RT {Guid.NewGuid():N}",
            ContactEmail = "rt@example.com",
            Model = LeadModel.MainContractor,
            NumberOfSites = 5,
            EstimatedRevenue = 12345.67m,
            AffiliateId = affiliateId,
            Status = LeadStatus.Qualified,
        };
        try
        {
            await repo.AddAsync(lead);

            var fetched = await repo.GetAsync(lead.Id);
            Assert.Equal(lead.CompanyName, fetched!.CompanyName);
            Assert.Equal(LeadModel.MainContractor, fetched.Model);
            Assert.Equal(12345.67m, fetched.EstimatedRevenue);

            var byAffiliate = await repo.ListByAffiliateAsync(affiliateId);
            Assert.Contains(byAffiliate, l => l.Id == lead.Id);

            var open = await repo.FindOpenByCompanyAndEmailAsync(lead.CompanyName, "RT@example.com");
            Assert.Equal(lead.Id, open!.Id);

            await repo.AddNoteAsync(new LeadNote { LeadId = lead.Id, Author = "Tester", Body = "A note", StatusChange = "New → Qualified" });
            var notes = await repo.ListNotesAsync(lead.Id);
            Assert.Single(notes);
            Assert.Equal("A note", notes[0].Body);
        }
        finally
        {
            await using var connection = (Microsoft.Data.SqlClient.SqlConnection)factory.Create();
            await connection.OpenAsync();
            await Dapper.SqlMapper.ExecuteAsync(connection,
                "DELETE FROM LeadNotes WHERE LeadId = @Id; DELETE FROM Leads WHERE Id = @Id;", new { Id = lead.Id });
        }
    }

    [SkippableFact]
    public async Task Affiliate_Payout_AndAgreement_RoundTrip()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "TEDWREN_TEST_SQLSERVER not set — DB integration tests skipped.");
        var factory = CreateFactory();
        await MigrateAsync(factory);
        var repo = new AffiliateRepository(factory);

        var affiliate = new Affiliate
        {
            Name = $"RT {Guid.NewGuid():N}",
            ContactEmail = "aff@example.com",
            AffiliateRatePct = 0.20m,
            ProfitMarginPct = 0.33m,
        };
        var payout = new AffiliatePayout { AffiliateId = affiliate.Id, Amount = 990m, Reference = "Q1" };
        var agreement = new AffiliateAgreement
        {
            AffiliateId = affiliate.Id,
            Token = Guid.NewGuid().ToString("N"),
            TermsHtml = "<p>terms</p>",
            CountersignatoryName = "James Darby",
            CountersignatoryTitle = "Director",
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        };
        try
        {
            await repo.AddAsync(affiliate);
            Assert.Equal(affiliate.Id, (await repo.GetAsync(affiliate.Id))!.Id);

            await repo.AddPayoutAsync(payout);
            var payouts = await repo.ListPayoutsAsync(affiliate.Id);
            Assert.Single(payouts);
            payout.Status = AffiliatePayoutStatus.Paid;
            payout.PaidUtc = DateTimeOffset.UtcNow;
            await repo.UpdatePayoutAsync(payout);
            Assert.Equal(AffiliatePayoutStatus.Paid, (await repo.GetPayoutAsync(payout.Id))!.Status);

            await repo.AddAgreementAsync(agreement);
            var byToken = await repo.GetAgreementByTokenAsync(agreement.Token);
            Assert.Equal(agreement.Id, byToken!.Id);
            agreement.Status = AffiliateAgreementStatus.Signed;
            agreement.SignedByName = "Pat";
            agreement.SignedUtc = DateTimeOffset.UtcNow;
            agreement.PdfBytes = new byte[] { 1, 2, 3 };
            await repo.UpdateAgreementAsync(agreement);
            var signed = await repo.GetAgreementByAffiliateAsync(affiliate.Id);
            Assert.Equal(AffiliateAgreementStatus.Signed, signed!.Status);
            Assert.Equal(3, signed.PdfBytes!.Length);
        }
        finally
        {
            await using var connection = (Microsoft.Data.SqlClient.SqlConnection)factory.Create();
            await connection.OpenAsync();
            await Dapper.SqlMapper.ExecuteAsync(connection,
                "DELETE FROM AffiliateAgreements WHERE AffiliateId = @Id; DELETE FROM AffiliatePayouts WHERE AffiliateId = @Id; DELETE FROM Affiliates WHERE Id = @Id;",
                new { Id = affiliate.Id });
        }
    }
}
