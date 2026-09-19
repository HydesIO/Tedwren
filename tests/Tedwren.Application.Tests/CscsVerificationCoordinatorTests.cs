using Tedwren.Abstractions.Contracts.Entitlements;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Qualifications;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for the CSCS verification coordinator (PRD-Phase 1, §8.1): the module gate, and the mapping from a
/// raw CSCS lookup to the induction/entry decision — verified, expired (blocks induction), unrecognised (blocks
/// entry) and unavailable (human fallback, never blocks).
/// </summary>
public sealed class CscsVerificationCoordinatorTests
{
    private static readonly Guid Company = Guid.NewGuid();

    /// <summary>Entitlement stub reporting a fixed enabled state.</summary>
    private sealed class FakeEntitlements : IEntitlementService
    {
        private readonly bool _enabled;
        public FakeEntitlements(bool enabled) => _enabled = enabled;
        public Task<bool> IsEnabledAsync(Guid companyId, string moduleKey, CancellationToken cancellationToken = default) => Task.FromResult(_enabled);
        public Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetEnabledAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>Verifier stub returning a fixed lookup result.</summary>
    private sealed class FakeVerifier : ICscsVerificationService
    {
        private readonly CscsVerificationResult _result;
        public FakeVerifier(CscsVerificationResult result) => _result = result;
        public Task<CscsVerificationResult> VerifyAsync(string cardNumber, string? scheme, CancellationToken cancellationToken = default) => Task.FromResult(_result);
    }

    private static CscsVerificationCoordinator Sut(bool entitled, CscsVerificationStatus status, DateOnly? expiry = null) =>
        new(new FakeVerifier(new CscsVerificationResult(status, expiry)), new FakeEntitlements(entitled));

    [Fact]
    public async Task NotEntitled_MakesNoCall_AndCardStaysCustomerChecked()
    {
        var result = await Sut(entitled: false, CscsVerificationStatus.Verified).CheckAsync(Company, "CARD1", null);

        Assert.Equal(CscsCheckOutcome.NotEntitled, result.Outcome);
        Assert.Equal(CardVerificationState.CustomerChecked, result.State);
        Assert.False(result.BlocksInduction);
        Assert.False(result.BlocksEntry);
    }

    [Fact]
    public async Task Verified_ProducesCscsVerifiedState_WithExpiry()
    {
        var expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        var result = await Sut(entitled: true, CscsVerificationStatus.Verified, expiry).CheckAsync(Company, "CARD1", null);

        Assert.Equal(CscsCheckOutcome.VerifiedLive, result.Outcome);
        Assert.Equal(CardVerificationState.CscsVerified, result.State);
        Assert.Equal(expiry, result.Expiry);
        Assert.False(result.BlocksInduction);
    }

    [Fact]
    public async Task Expired_BlocksTheInduction()
    {
        var result = await Sut(entitled: true, CscsVerificationStatus.Expired).CheckAsync(Company, "CARD1", null);

        Assert.Equal(CscsCheckOutcome.ExpiredBlocksInduction, result.Outcome);
        Assert.True(result.BlocksInduction);
    }

    [Fact]
    public async Task NotFound_ContinuesInduction_ButBlocksEntry()
    {
        var result = await Sut(entitled: true, CscsVerificationStatus.NotFound).CheckAsync(Company, "CARD1", null);

        Assert.Equal(CscsCheckOutcome.UnrecognisedBlocksEntry, result.Outcome);
        Assert.False(result.BlocksInduction);
        Assert.True(result.BlocksEntry);
    }

    [Fact]
    public async Task Unavailable_FallsBackToHumanCheck_NeverBlocks()
    {
        var result = await Sut(entitled: true, CscsVerificationStatus.Unavailable).CheckAsync(Company, "CARD1", null);

        Assert.Equal(CscsCheckOutcome.HumanFallback, result.Outcome);
        Assert.Equal(CardVerificationState.CustomerChecked, result.State);
        Assert.False(result.BlocksInduction);
        Assert.False(result.BlocksEntry);
    }

    [Fact]
    public async Task UnconfiguredService_ReportsUnavailable()
    {
        var result = await new UnconfiguredCscsVerificationService().VerifyAsync("CARD1", null);

        Assert.Equal(CscsVerificationStatus.Unavailable, result.Status);
    }
}
