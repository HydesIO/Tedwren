using System.Text;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.CompliancePacks;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.CompliancePacks;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the compliance-pack service (SUB-13–SUB-26, R7–R9): the readiness gate (SUB-14), account-free
/// token + passcode access with tracking (R8/SUB-20), CSV/ZIP/PDF export (SUB-16), revoke (SUB-21) and
/// re-issue-supersedes (R7).
/// </summary>
public sealed class CompliancePackServiceTests
{
    private static readonly Guid Company = Guid.NewGuid();
    private static readonly Guid ExpiredPerson = Guid.NewGuid();
    private static readonly Guid ValidPerson = Guid.NewGuid();

    private static CompliancePackService CreateService()
    {
        var quals = new FakeQualifications();
        quals.SetCard(ExpiredPerson, ComplianceState.NonCompliant, "Expired", new DateOnly(2026, 6, 1));
        quals.SetCard(ValidPerson, ComplianceState.Compliant, "Valid", new DateOnly(2028, 1, 1));

        return new CompliancePackService(
            new InMemoryCompliancePackRepository(new InMemoryCompliancePackStore()),
            quals,
            new FakeEngagements());
    }

    private static BuildPackRequest Request(Guid person, bool acknowledge, Guid? supersedes = null) =>
        new(Company, "Weekly pack", "Alex Morgan", null, null, null, new[] { person }, null, null, acknowledge, supersedes);

    [Fact]
    public async Task Build_WithExpiredCard_IsBlockedUntilAcknowledged()
    {
        var service = CreateService();

        var blocked = await service.BuildAsync(Request(ExpiredPerson, acknowledge: false));
        Assert.False(blocked.Sent);
        Assert.Contains(blocked.Issues, i => i.Blocking);
        Assert.Null(blocked.PackId);

        var sent = await service.BuildAsync(Request(ExpiredPerson, acknowledge: true));
        Assert.True(sent.Sent);
        Assert.NotNull(sent.Token);
        Assert.False(string.IsNullOrWhiteSpace(sent.Passcode));   // a passcode is generated when none supplied
    }

    [Fact]
    public async Task Build_WithValidCards_SendsImmediately() =>
        Assert.True((await CreateService().BuildAsync(Request(ValidPerson, acknowledge: false))).Sent);

    [Fact]
    public async Task View_RequiresCorrectPasscode_AndRecordsAnOpen()
    {
        var service = CreateService();
        var sent = await service.BuildAsync(Request(ValidPerson, acknowledge: false));

        var wrong = await service.ViewAsync(sent.Token!, "WRONG");
        Assert.False(wrong.Granted);

        var ok = await service.ViewAsync(sent.Token!, sent.Passcode!);
        Assert.True(ok.Granted);
        Assert.NotNull(ok.Pack);

        var list = await service.ListForCompanyAsync(Company);
        Assert.Equal(1, list.Single().OpenedCount);   // the open was tracked (SUB-20)
    }

    [Fact]
    public async Task Revoke_MakesThePackInaccessible()
    {
        var service = CreateService();
        var sent = await service.BuildAsync(Request(ValidPerson, acknowledge: false));

        Assert.True(await service.RevokeAsync(Company, sent.PackId!.Value));

        var view = await service.ViewAsync(sent.Token!, sent.Passcode!);
        Assert.False(view.Granted);
        Assert.Contains("revoked", view.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reissue_SupersedesThePriorPack()
    {
        var service = CreateService();
        var first = await service.BuildAsync(Request(ValidPerson, acknowledge: false));

        await service.BuildAsync(Request(ValidPerson, acknowledge: false, supersedes: first.PackId));

        var priorView = await service.ViewAsync(first.Token!, first.Passcode!);
        Assert.False(priorView.Granted);
        Assert.Contains("replaced", priorView.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Downloads_ProduceCsvZipAndPdf_AndRecordDownloads()
    {
        var service = CreateService();
        var sent = await service.BuildAsync(Request(ValidPerson, acknowledge: false));

        var csv = await service.DownloadCsvAsync(sent.Token!, sent.Passcode!);
        Assert.NotNull(csv);
        Assert.StartsWith("Operative,Qualification,Status,Expires,Verification", Encoding.UTF8.GetString(csv!));

        var zip = await service.DownloadZipAsync(sent.Token!, sent.Passcode!);
        Assert.Equal(new byte[] { (byte)'P', (byte)'K' }, zip!.Take(2).ToArray());

        var pdf = await service.DownloadPdfAsync(sent.Token!, sent.Passcode!);
        Assert.StartsWith("%PDF-1.4", Encoding.ASCII.GetString(pdf!.Take(8).ToArray()));

        var list = await service.ListForCompanyAsync(Company);
        Assert.Equal(3, list.Single().DownloadedCount);
    }

    /// <summary>Qualification service fake returning preset cards per person.</summary>
    private sealed class FakeQualifications : IQualificationService
    {
        private readonly Dictionary<Guid, List<QualificationCardDto>> _cards = new();

        public void SetCard(Guid personId, ComplianceState state, string label, DateOnly expiresOn) =>
            _cards[personId] = new List<QualificationCardDto>
            {
                new(Guid.NewGuid(), personId, Guid.NewGuid(), "CSCS — Skilled Worker", "CITB", "123", "Holder",
                    new DateOnly(2024, 1, 1), expiresOn, state, label, CardVerificationState.CustomerChecked,
                    "Customer-checked", false, "Alex", DateTimeOffset.UtcNow, false),
            };

        public Task<IReadOnlyList<QualificationCardDto>> GetCardsForPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<QualificationCardDto>>(_cards.TryGetValue(personId, out var c) ? c : new List<QualificationCardDto>());

        public Task<IReadOnlyList<QualificationTypeDto>> GetQualificationTypesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid> CaptureCardAsync(CaptureCardRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ConfirmCardAsync(ConfirmCardRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid?> RenewCardAsync(RenewCardRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CompetencyShortfallDto> GetShortfallAsync(Guid personId, string trade, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>Engagement repository fake returning a fixed operative name.</summary>
    private sealed class FakeEngagements : Tedwren.Application.Persistence.IEngagementRepository
    {
        public Task<Engagement?> GetByCompanyAndPersonAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Engagement?>(new Engagement { CompanyId = companyId, PersonId = personId, Name = "Test Operative" });

        public Task<IReadOnlyList<Engagement>> GetActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Engagement>> GetActiveByPersonAsync(Guid personId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Engagement?> GetAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CountActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Engagement engagement, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Engagement engagement, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
