using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Rams;

/// <summary>The Gate-5 RAMS status for an operative at a site.</summary>
public enum RamsGateStatus
{
    /// <summary>RAMS is not enforced for this worker (HSE module not held, or no subcontractor RAMS obligation).</summary>
    NotApplicable,

    /// <summary>The subcontractor has no approved live RAMS — work cannot start until it is approved (§506).</summary>
    NoApprovedRams,

    /// <summary>An approved RAMS exists but the operative has not signed the current live version (spec Gate 5).</summary>
    MustSign,

    /// <summary>The operative has signed the current live RAMS version.</summary>
    Signed,
}

/// <summary>The outcome of a RAMS-gate evaluation, with the live RAMS to sign when <see cref="Status"/> is <see cref="RamsGateStatus.MustSign"/>.</summary>
public sealed record RamsGateResult(RamsGateStatus Status, string Detail, Guid? LiveRamsId = null, int? LiveVersion = null);

/// <summary>
/// The single source of truth for "is this operative cleared on RAMS at this site?" (Subcontractor Onboarding spec
/// Gate 5). Shared by the manager's site-entry decision (MC-8 fifth check) and the operative's own site sign-in, so
/// the two paths never diverge. Resolves the site's main contractor, gates on the HSE module (MC-8), finds the
/// operative's subcontractor onboarding config for that MC, then requires an approved live RAMS (§506) that the
/// operative has signed on its current version (append-only signatures, R4/R16). Also serves the operative sign
/// flow (fetch the live RAMS, record a signature). All reads are current (R3) and cheap (R14).
/// </summary>
public sealed class RamsGate
{
    private const string HseModule = "hse";

    private readonly ISiteRepository _sites;
    private readonly IEntitlementService _entitlements;
    private readonly ISubcontractorOnboardingConfigRepository _configs;
    private readonly IRamsRepository _rams;
    private readonly IRamsAcknowledgementRepository _acks;
    private readonly IEngagementRepository _engagements;

    /// <summary>Creates the gate over the site, entitlement, config, RAMS, acknowledgement and engagement repositories.</summary>
    public RamsGate(
        ISiteRepository sites,
        IEntitlementService entitlements,
        ISubcontractorOnboardingConfigRepository configs,
        IRamsRepository rams,
        IRamsAcknowledgementRepository acks,
        IEngagementRepository engagements)
    {
        _sites = sites;
        _entitlements = entitlements;
        _configs = configs;
        _rams = rams;
        _acks = acks;
        _engagements = engagements;
    }

    /// <summary>Evaluates Gate 5 for an operative signing in at a site (MC-8 fifth check; HSE-gated).</summary>
    public async Task<RamsGateResult> EvaluateAsync(Guid personId, Guid siteId, CancellationToken cancellationToken = default)
    {
        var site = await _sites.GetByIdAsync(siteId, cancellationToken);
        if (site is null)
        {
            return new(RamsGateStatus.NotApplicable, "RAMS not enforced — unknown site.");
        }

        var mcCompanyId = site.CompanyId;
        if (!await _entitlements.IsEnabledAsync(mcCompanyId, HseModule, cancellationToken))
        {
            return new(RamsGateStatus.NotApplicable, "RAMS module not held — check does not apply.");
        }

        var config = await ResolveConfigForMcAsync(personId, mcCompanyId, cancellationToken);
        if (config is null)
        {
            return new(RamsGateStatus.NotApplicable, "No subcontractor RAMS obligation for this worker.");
        }

        if (config.RamsFamilyId is not { } familyId)
        {
            return new(RamsGateStatus.NoApprovedRams, "No RAMS on record for this subcontractor — work cannot start until it is approved.");
        }

        var live = await _rams.GetLiveForFamilyAsync(mcCompanyId, familyId, cancellationToken);
        if (live is null || !IsApproved(live.Status))
        {
            return new(RamsGateStatus.NoApprovedRams, "RAMS is not yet approved — work cannot start until it is approved.");
        }

        var ack = await _acks.GetLatestForPersonAsync(mcCompanyId, personId, familyId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (ack is null || ack.Version != live.Version || !ack.IsValid(now))
        {
            return new(RamsGateStatus.MustSign, "You must read and sign the current RAMS before starting.", live.Id, live.Version);
        }

        return new(RamsGateStatus.Signed, $"RAMS signed (v{live.Version}).", live.Id, live.Version);
    }

    /// <summary>Returns the operative's current live approved RAMS to read and sign, or null when none applies.</summary>
    public async Task<LiveRamsDto?> GetLiveForOperativeAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        foreach (var config in await ConfigsForPersonAsync(personId, cancellationToken))
        {
            if (config.RamsFamilyId is not { } familyId)
            {
                continue;
            }

            var live = await _rams.GetLiveForFamilyAsync(config.InviterCompanyId, familyId, cancellationToken);
            if (live is not null && IsApproved(live.Status))
            {
                return ToLiveDto(live);
            }
        }

        return null;
    }

    /// <summary>Records an operative's signature of a live RAMS version (Gate 5), or null when the submission is not the current live version.</summary>
    public async Task<RamsAcknowledgementDto?> AcknowledgeAsync(Guid personId, SignRamsRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.SignatureName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("A signature is required.", nameof(request));
        }

        var submission = await _rams.GetAsync(request.RamsSubmissionId, cancellationToken);
        if (submission is null || !submission.IsLive || !IsApproved(submission.Status))
        {
            return null;   // not the current live approved version — nothing to sign
        }

        var expiresUtc = await ResolveExpiryAsync(personId, submission, cancellationToken);
        var ack = new RamsAcknowledgement
        {
            CompanyId = submission.CompanyId,
            PersonId = personId,
            FamilyId = submission.FamilyId,
            Version = submission.Version,
            SignatureName = name,
            SignedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = expiresUtc,
        };
        await _acks.AddAsync(ack, cancellationToken);
        return new RamsAcknowledgementDto(ack.Id, ack.FamilyId, ack.Version, ack.SignedUtc, ack.ExpiresUtc);
    }

    /// <summary>Whether a RAMS status is a signable, approved live state.</summary>
    private static bool IsApproved(RamsStatus status) =>
        status is RamsStatus.Approved or RamsStatus.ApprovedWithComments;

    /// <summary>The operative's onboarding config whose inviting main contractor is this site's MC, or null.</summary>
    private async Task<SubcontractorOnboardingConfig?> ResolveConfigForMcAsync(Guid personId, Guid mcCompanyId, CancellationToken cancellationToken)
    {
        foreach (var config in await ConfigsForPersonAsync(personId, cancellationToken))
        {
            if (config.InviterCompanyId == mcCompanyId)
            {
                return config;
            }
        }

        return null;
    }

    /// <summary>The subcontractor onboarding configs the operative falls under, via their active engagements (usually one).</summary>
    private async Task<IReadOnlyList<SubcontractorOnboardingConfig>> ConfigsForPersonAsync(Guid personId, CancellationToken cancellationToken)
    {
        var engagements = await _engagements.GetActiveByPersonAsync(personId, cancellationToken);
        var configs = new List<SubcontractorOnboardingConfig>();
        foreach (var companyId in engagements.Select(e => e.CompanyId).Distinct())
        {
            var config = await _configs.GetBySubcontractorCompanyAsync(companyId, cancellationToken);
            if (config is not null)
            {
                configs.Add(config);
            }
        }

        return configs;
    }

    /// <summary>The re-sign deadline for a signature under the MC's RAMS review cycle (SO-4), or null when no cycle is set.</summary>
    private async Task<DateTimeOffset?> ResolveExpiryAsync(Guid personId, RamsSubmission submission, CancellationToken cancellationToken)
    {
        foreach (var config in await ConfigsForPersonAsync(personId, cancellationToken))
        {
            if (config.InviterCompanyId == submission.CompanyId && config.RamsFamilyId == submission.FamilyId &&
                config.RamsReviewCycleMonths is { } months && months > 0)
            {
                return DateTimeOffset.UtcNow.AddMonths(months);
            }
        }

        return null;
    }

    /// <summary>Maps a live RAMS submission to the operative-facing DTO (file exposed only as a flag, R9).</summary>
    private static LiveRamsDto ToLiveDto(RamsSubmission s) =>
        new(s.Id, s.FamilyId, s.Version, s.Title, s.Reference, s.ContractorName,
            !string.IsNullOrEmpty(s.FileReference), s.Status.ToString(), s.SubmittedUtc);
}
