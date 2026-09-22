using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Application.Inductions;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the operative-facing induction facade (Subcontractor Onboarding spec Stage 4 / Gate 4): it resolves
/// the operative's main-contractor induction — the one their subcontractor was configured with, or their own
/// company's template as a fallback — starts/returns their session, and guards every action to the operative's
/// own session (R15).
/// </summary>
public sealed class OperativeInductionServiceTests
{
    private static readonly Guid MainContractor = Guid.Parse("77777777-7777-4777-8777-000000000001");
    private static readonly Guid Subcontractor = Guid.Parse("88888888-8888-4888-8888-000000000002");

    private sealed record Sut(OperativeInductionService Operative, InductionService Inductions, InMemorySubcontractorOnboardingConfigRepository Configs);

    private static Sut CreateSut()
    {
        var store = new InMemoryInductionStore();
        var inductions = new InductionService(new InMemoryInductionTemplateRepository(store), new InMemoryInductionSessionRepository(store));
        var configs = new InMemorySubcontractorOnboardingConfigRepository();
        return new Sut(new OperativeInductionService(inductions, configs), inductions, configs);
    }

    private static async Task LinkConfigAsync(Sut sut, Guid inviter, Guid subcontractor, Guid templateId) =>
        await sut.Configs.AddAsync(new SubcontractorOnboardingConfig
        {
            Id = Guid.NewGuid(),
            InviterCompanyId = inviter,
            SubcontractorCompanyId = subcontractor,
            TradeInviteId = Guid.NewGuid(),
            InductionTemplateId = templateId,
            CreatedUtc = DateTimeOffset.UtcNow,
        });

    [Fact] // Primary resolution — the operative gets the induction their subcontractor was configured with (§6.1).
    public async Task GetCurrent_ResolvesViaSubcontractorConfig()
    {
        var sut = CreateSut();
        var templateId = await sut.Inductions.CreateDefaultTemplateAsync(new CreateInductionTemplateRequest(MainContractor, "Site induction", 365, 3));
        await LinkConfigAsync(sut, MainContractor, Subcontractor, templateId);

        var session = await sut.Operative.GetCurrentAsync(Subcontractor, Guid.NewGuid(), "Alex Operative");

        Assert.NotNull(session);
        Assert.Equal(templateId, session!.TemplateId);
    }

    [Fact] // Fallback resolution — a direct main-contractor operative gets their own company's induction.
    public async Task GetCurrent_FallsBackToOwnCompanyTemplate()
    {
        var sut = CreateSut();
        var templateId = await sut.Inductions.CreateDefaultTemplateAsync(new CreateInductionTemplateRequest(MainContractor, "Site induction", 365, 3));

        var session = await sut.Operative.GetCurrentAsync(MainContractor, Guid.NewGuid(), "Alex Operative");

        Assert.NotNull(session);
        Assert.Equal(templateId, session!.TemplateId);
    }

    [Fact] // No induction configured for the operative's company → nothing to complete.
    public async Task GetCurrent_ReturnsNull_WhenNoInductionConfigured()
    {
        var sut = CreateSut();

        var session = await sut.Operative.GetCurrentAsync(Subcontractor, Guid.NewGuid(), "Alex Operative");

        Assert.Null(session);
    }

    [Fact] // R15 — an operative cannot act on another operative's session.
    public async Task Actions_AreScopedToTheOwningOperative()
    {
        var sut = CreateSut();
        var templateId = await sut.Inductions.CreateDefaultTemplateAsync(new CreateInductionTemplateRequest(MainContractor, "Site induction", 365, 3));
        await LinkConfigAsync(sut, MainContractor, Subcontractor, templateId);

        var mine = Guid.NewGuid();
        var session = await sut.Operative.GetCurrentAsync(Subcontractor, mine, "Alex Operative");
        Assert.NotNull(session);

        var intruder = Guid.NewGuid();
        Assert.Null(await sut.Operative.CompleteStepAsync(intruder, session!.Id, session.Steps[0].Id));
        Assert.Null(await sut.Operative.SubmitQuizAsync(intruder, session.Id, new SubmitQuizRequest(new Dictionary<string, int>())));

        // The owner can act on it.
        Assert.NotNull(await sut.Operative.CompleteStepAsync(mine, session.Id, session.Steps[0].Id));
    }
}
