using Tedwren.Application.Evidence;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies <see cref="EvidenceCaptureQueryService"/> (M7 evidence review): company scoping (R15), capturer-name
/// resolution from the engagement (PRD §5.1), per-operative filtering, and photo-reference passthrough (R9).
/// </summary>
public sealed class EvidenceCaptureQueryServiceTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    private static (EvidenceCaptureQueryService Service, InMemoryEvidenceItemRepository Items, InMemoryEngagementRepository Engagements) CreateSut()
    {
        var items = new InMemoryEvidenceItemRepository();
        var engagements = new InMemoryEngagementRepository(new InMemoryOrganisationStore());
        return (new EvidenceCaptureQueryService(items, engagements), items, engagements);
    }

    private static EvidenceItem Capture(Guid companyId, Guid personId, string? photo = "img-ref") => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        PersonId = personId,
        Note = "Cracked slab",
        Latitude = 51.5,
        Longitude = -0.1,
        PhotoReference = photo,
        CapturedUtc = DateTimeOffset.UtcNow.AddMinutes(-15),
    };

    [Fact]
    public async Task GetForCompany_returns_only_that_companys_captures_with_the_engagement_name()
    {
        var (service, items, engagements) = CreateSut();
        var person = Guid.NewGuid();
        await engagements.AddAsync(new Engagement { CompanyId = CompanyA, PersonId = person, Name = "Alex Operative" });
        var mine = Capture(CompanyA, person);
        await items.AddAsync(mine);
        await items.AddAsync(Capture(CompanyB, Guid.NewGuid())); // another company's capture must not leak (R15)

        var result = await service.GetForCompanyAsync(CompanyA);

        var dto = Assert.Single(result);
        Assert.Equal(mine.Id, dto.Id);
        Assert.Equal("Alex Operative", dto.PersonName);
        Assert.Equal("img-ref", dto.PhotoReference);
    }

    [Fact]
    public async Task GetForCompany_can_filter_to_one_operative()
    {
        var (service, items, engagements) = CreateSut();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        await engagements.AddAsync(new Engagement { CompanyId = CompanyA, PersonId = p1, Name = "Alex" });
        await engagements.AddAsync(new Engagement { CompanyId = CompanyA, PersonId = p2, Name = "Sam" });
        await items.AddAsync(Capture(CompanyA, p1));
        await items.AddAsync(Capture(CompanyA, p2));

        var result = await service.GetForCompanyAsync(CompanyA, p1);

        var dto = Assert.Single(result);
        Assert.Equal(p1, dto.PersonId);
    }

    [Fact]
    public async Task Get_by_id_for_another_company_returns_null()
    {
        var (service, items, _) = CreateSut();
        var capture = Capture(CompanyA, Guid.NewGuid());
        await items.AddAsync(capture);

        Assert.Null(await service.GetAsync(CompanyB, capture.Id));
        Assert.NotNull(await service.GetAsync(CompanyA, capture.Id));
    }

    [Fact]
    public async Task Get_falls_back_when_the_capturer_has_no_engagement_name()
    {
        var (service, items, _) = CreateSut();
        var capture = Capture(CompanyA, Guid.NewGuid(), photo: null);
        await items.AddAsync(capture);

        var dto = await service.GetAsync(CompanyA, capture.Id);

        Assert.Equal("Unknown operative", dto!.PersonName);
        Assert.Null(dto.PhotoReference);
    }
}
