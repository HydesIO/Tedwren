using Tedwren.Application.DemoData;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the demo-data service over the in-memory repositories: seeding produces the expected dataset,
/// deleting removes it completely, and reseeding is idempotent (deterministic ids, no duplication).
/// </summary>
public sealed class DemoDataServiceTests
{
    /// <summary>Builds a service over fresh, unseeded in-memory stores so only demo data is present.</summary>
    private static DemoDataService CreateService()
    {
        var orgStore = new InMemoryOrganisationStore(seed: false);
        var siteStore = new InMemorySiteStore(seed: false);
        var attendanceStore = new InMemoryAttendanceStore();
        var qualStore = new InMemoryQualificationStore(seed: false);
        var userStore = new InMemoryUserStore();

        return new DemoDataService(
            new InMemoryCompanyRepository(orgStore),
            new InMemoryUserRepository(userStore),
            new InMemoryEntitlementRepository(),
            new InMemorySiteRepository(siteStore),
            new InMemoryPersonRepository(orgStore),
            new InMemoryEngagementRepository(orgStore),
            new InMemoryQualificationCardRepository(qualStore),
            new InMemoryAttendanceRepository(attendanceStore),
            new InMemoryMandateRepository(),
            new InMemoryPaymentRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new InMemoryPayoutRepository(),
            new DemoDataProgressState());
    }

    [Fact]
    public async Task Seed_creates_the_expected_dataset()
    {
        var service = CreateService();

        var status = await service.SeedAsync();

        Assert.True(status.Present);
        Assert.Equal(2, status.Companies);
        Assert.Equal(5, status.Users);
        Assert.Equal(16, status.Sites);          // 10 main + 6 subcontractor (incl. dispersed/retrofit)
        Assert.Equal(30, status.Operatives);      // 25 main + 5 subcontractor
        Assert.Equal(26, status.Payments);        // 2 companies × (12 months + 1 re-take)
        Assert.True(status.QualificationCards >= 30);
        Assert.True(status.AttendanceRecords > 0);
    }

    [Fact]
    public async Task Delete_removes_the_entire_dataset()
    {
        var service = CreateService();
        await service.SeedAsync();

        await service.DeleteAsync();

        var status = await service.GetStatusAsync();
        Assert.False(status.Present);
        Assert.Equal(0, status.Companies);
        Assert.Equal(0, status.Users);
        Assert.Equal(0, status.Sites);
        Assert.Equal(0, status.Operatives);
        Assert.Equal(0, status.Payments);
        Assert.Equal(0, status.QualificationCards);
        Assert.Equal(0, status.AttendanceRecords);
    }

    [Fact]
    public async Task Reseeding_is_idempotent()
    {
        var service = CreateService();

        var first = await service.SeedAsync();
        var second = await service.SeedAsync();   // recreate: clears then rebuilds

        Assert.Equal(first.Companies, second.Companies);
        Assert.Equal(first.Users, second.Users);
        Assert.Equal(first.Sites, second.Sites);
        Assert.Equal(first.Operatives, second.Operatives);
        Assert.Equal(first.QualificationCards, second.QualificationCards);
        Assert.Equal(first.AttendanceRecords, second.AttendanceRecords);
        Assert.Equal(first.Payments, second.Payments);
    }
}
