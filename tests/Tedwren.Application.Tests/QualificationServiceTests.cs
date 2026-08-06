using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Qualifications;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the qualification-card and competency rules (SF-5/SF-6/SF-8/SF-10/SF-11/SF-12) on
/// <see cref="QualificationService"/> over the seeded in-memory repositories.
/// </summary>
public sealed class QualificationServiceTests
{
    /// <summary>Builds the service over the seeded in-memory store (default library present).</summary>
    private static QualificationService CreateSut()
    {
        var store = new InMemoryQualificationStore(seed: true);
        return new QualificationService(
            new InMemoryQualificationTypeRepository(store),
            new InMemoryQualificationCardRepository(store),
            new InMemoryTradeRequirementRepository(store));
    }

    /// <summary>Finds the "CSCS Card" type id from the default library.</summary>
    private static async Task<Guid> CscsTypeIdAsync(QualificationService service)
    {
        var types = await service.GetQualificationTypesAsync();
        return types.Single(t => t.Name == "CSCS Card").Id;
    }

    [Fact] // SF-12
    public async Task DefaultLibrary_IsAvailable()
    {
        var service = CreateSut();

        var types = await service.GetQualificationTypesAsync();

        Assert.Equal(7, types.Count);
        Assert.Contains(types, t => t.Name == "CSCS Card" && t.IsCscsVerifiable);
    }

    [Fact] // SF-5
    public async Task CaptureCard_IsUnchecked_AndNotConfirmed()
    {
        var service = CreateSut();
        var person = Guid.NewGuid();
        var cscs = await CscsTypeIdAsync(service);

        await service.CaptureCardAsync(new CaptureCardRequest(person, cscs, "CS123", "Joe Smith", null, null, NeedsReview: true));
        var card = Assert.Single(await service.GetCardsForPersonAsync(person));

        Assert.Equal(CardVerificationState.ReadUnchecked, card.VerificationState);
        Assert.True(card.NeedsReview);
        Assert.Null(card.ConfirmedBy);
    }

    [Fact] // SF-6
    public async Task ConfirmCard_RecordsWhoAndWhen_AndClearsReview()
    {
        var service = CreateSut();
        var person = Guid.NewGuid();
        var cscs = await CscsTypeIdAsync(service);
        var id = await service.CaptureCardAsync(new CaptureCardRequest(person, cscs, "CS123", "Joe Smith", null, null, NeedsReview: true));

        var confirmed = await service.ConfirmCardAsync(new ConfirmCardRequest(id, "Alex Morgan"));
        var card = Assert.Single(await service.GetCardsForPersonAsync(person));

        Assert.True(confirmed);
        Assert.Equal(CardVerificationState.CustomerChecked, card.VerificationState);
        Assert.Equal("Alex Morgan", card.ConfirmedBy);
        Assert.NotNull(card.ConfirmedUtc);
        Assert.False(card.NeedsReview);
    }

    [Fact] // SF-8
    public async Task CardStatus_IsComputedFromExpiry()
    {
        var service = CreateSut();
        var person = Guid.NewGuid();
        var cscs = await CscsTypeIdAsync(service);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await service.CaptureCardAsync(new CaptureCardRequest(person, cscs, "A", null, null, today.AddDays(-1), false));
        var card = Assert.Single(await service.GetCardsForPersonAsync(person));

        Assert.Equal(ComplianceState.NonCompliant, card.State);   // expired
        Assert.Equal("Expired", card.StatusLabel);
    }

    [Fact] // SF-10
    public async Task RenewCard_SupersedesOld_AndRetainsIt()
    {
        var service = CreateSut();
        var person = Guid.NewGuid();
        var cscs = await CscsTypeIdAsync(service);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oldId = await service.CaptureCardAsync(new CaptureCardRequest(person, cscs, "OLD", null, null, today.AddDays(10), false));

        var newId = await service.RenewCardAsync(new RenewCardRequest(oldId, "NEW", today, today.AddYears(5)));
        var cards = await service.GetCardsForPersonAsync(person);

        Assert.NotNull(newId);
        Assert.Equal(2, cards.Count);                                   // old retained (SF-10)
        Assert.True(cards.Single(c => c.Id == oldId).IsSuperseded);
        Assert.False(cards.Single(c => c.Id == newId!.Value).IsSuperseded);
    }

    [Fact] // HeldBy count (SF-12 library)
    public async Task HeldBy_CountsCurrentCards()
    {
        var service = CreateSut();
        var cscs = await CscsTypeIdAsync(service);
        await service.CaptureCardAsync(new CaptureCardRequest(Guid.NewGuid(), cscs, "A", null, null, null, false));

        var types = await service.GetQualificationTypesAsync();

        Assert.Equal(1, types.Single(t => t.Id == cscs).HeldBy);
    }

    [Fact] // SF-11
    public async Task Shortfall_ListsRequiredButUnheldQualifications()
    {
        var service = CreateSut();
        var person = Guid.NewGuid();
        var cscs = await CscsTypeIdAsync(service);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Groundworks requires CSCS Card + Manual Handling. Give the person only a valid CSCS card.
        await service.CaptureCardAsync(new CaptureCardRequest(person, cscs, "A", null, null, today.AddYears(2), false));
        var shortfall = await service.GetShortfallAsync(person, "Groundworks");

        Assert.Equal("Groundworks", shortfall.Trade);
        Assert.Equal(new[] { "Manual Handling" }, shortfall.MissingQualifications);
    }
}
