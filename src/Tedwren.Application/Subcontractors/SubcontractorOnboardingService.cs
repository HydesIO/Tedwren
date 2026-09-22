using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Subcontractors;
using Tedwren.Abstractions.Services;
using Tedwren.Application.CompliancePacks;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Subcontractors;

/// <summary>
/// The main-contractor subcontractor set-up & configuration flow (Subcontractor Onboarding spec Stage 1 / §4).
/// Composes existing building blocks: it creates the subcontractor company through <see cref="IOrganisationService"/>
/// (as a <see cref="Tedwren.Abstractions.Common.OrgType.Subcontractor"/>), issues a tokenised
/// <see cref="TradeInvite"/> so the subcontractor uploads its documents from the same link the trade-onboarding
/// flow uses (landing in the existing review queue), and records a <see cref="SubcontractorOnboardingConfig"/>.
/// Tenant scope + audit come from the optional collaborators so unit tests can construct it bare (the established
/// pattern); the composition root supplies them. Reads/writes are scoped to the inviting main contractor (R15).
/// </summary>
public sealed class SubcontractorOnboardingService : ISubcontractorOnboardingService
{
    private readonly IOrganisationService _organisation;
    private readonly ITradeInviteRepository _invites;
    private readonly ISubcontractorOnboardingConfigRepository _configs;
    private readonly ICompanyDocumentRepository _documents;
    private readonly ICurrentUserService? _currentUser;
    private readonly IAuditService? _audit;
    private readonly IRamsRepository? _rams;
    private readonly IInductionService? _inductions;

    /// <summary>Default invite lifetime (SUB-18: 30 days), mirroring the onboarding/trade links.</summary>
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Creates the service over the organisation service and the invite, config + document repositories. The RAMS
    /// repository (review-cycle due-list) and the induction service (resolving the MC's induction template, Gate 4)
    /// are optional so unit tests can construct the service bare (the established pattern); the composition root
    /// supplies them.
    /// </summary>
    public SubcontractorOnboardingService(
        IOrganisationService organisation,
        ITradeInviteRepository invites,
        ISubcontractorOnboardingConfigRepository configs,
        ICompanyDocumentRepository documents,
        ICurrentUserService? currentUser = null,
        IAuditService? audit = null,
        IRamsRepository? rams = null,
        IInductionService? inductions = null)
    {
        _organisation = organisation;
        _invites = invites;
        _configs = configs;
        _documents = documents;
        _currentUser = currentUser;
        _audit = audit;
        _rams = rams;
        _inductions = inductions;
    }

    /// <summary>Sets up & configures a subcontractor, returning the shareable onboarding link and the created ids.</summary>
    public async Task<SubcontractorOnboardingResultDto> SetupAsync(SetupSubcontractorRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.CompanyName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("A company name is required.", nameof(request));
        }

        var inviterCompanyId = await ResolveTenantAsync(cancellationToken)
            ?? throw new InvalidOperationException("A signed-in company is required to onboard a subcontractor.");

        // 1. Create the subcontractor company (as a subcontractor), which the organisation service audits.
        var contactName = Trimmed(request.ContactName);
        var contactEmail = Trimmed(request.ContactEmail);
        var subcontractorCompanyId = await _organisation.CreateCompanyAsync(new CreateCompanyRequest(
            name, "Subcontractor", Trimmed(request.Trade), Trimmed(request.RegistrationNumber), Address: null,
            ContactName: contactName, ContactEmail: contactEmail, ContactPhone: Trimmed(request.ContactPhone),
            OrgType: Tedwren.Abstractions.Common.OrgType.Subcontractor), cancellationToken);

        // 2. Issue the onboarding invite so the subcontractor uploads its documents from the link (SUB-4/MC-27).
        string? passcode = null, passcodeHash = null;
        if (request.RequirePasscode)
        {
            passcode = PackPasscode.Generate();
            passcodeHash = PackPasscode.Hash(passcode);
        }

        var invite = new TradeInvite
        {
            Token = PackToken.Generate(),
            PasscodeHash = passcodeHash,
            CompanyId = subcontractorCompanyId,
            InviterCompanyId = inviterCompanyId,
            ContactName = contactName,
            ContactEmail = contactEmail,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(LinkLifetime),
            CreatedByUserId = await CurrentUserIdAsync(cancellationToken),
        };
        await _invites.AddAsync(invite, cancellationToken);

        // 3. Resolve the MC's induction template the operatives must complete (Gate 4). The induction is always the
        //    MC's own (§6.1): reuse the MC's existing template when it has one, else create one from these settings.
        var validityDays = request.InductionValidityDays <= 0 ? 365 : request.InductionValidityDays;
        var passMark = Math.Max(0, request.InductionPassMark);
        var attemptLimit = request.InductionAttemptLimit <= 0 ? 3 : request.InductionAttemptLimit;
        var inductionTemplateId = await ResolveOrCreateInductionTemplateAsync(inviterCompanyId, validityDays, passMark, attemptLimit, cancellationToken);

        // 4. Record the configuration (the Gate 1 required-document set, access period, SSSTS/SMSTS, induction, RAMS cycle).
        var config = new SubcontractorOnboardingConfig
        {
            Id = Guid.NewGuid(),
            InviterCompanyId = inviterCompanyId,
            SubcontractorCompanyId = subcontractorCompanyId,
            TradeInviteId = invite.Id,
            AccessPeriodMonths = request.AccessPeriodMonths <= 0 ? 12 : request.AccessPeriodMonths,
            RequiredDocuments = (request.RequiredDocuments ?? Array.Empty<RequiredDocumentSelection>())
                .Where(d => !string.IsNullOrWhiteSpace(d.Heading))
                .Select(d => new RequiredDocumentHeading(d.Heading.Trim(), d.RequiredBeforeWork))
                .ToList(),
            SsstsRequired = request.SsstsRequired,
            SmstsRequired = request.SmstsRequired,
            InductionValidityDays = validityDays,
            InductionPassMark = passMark,
            InductionAttemptLimit = attemptLimit,
            InductionTemplateId = inductionTemplateId,
            RamsReviewCycleMonths = request.RamsReviewCycleMonths,
            CreatedUtc = DateTimeOffset.UtcNow,
        };
        await _configs.AddAsync(config, cancellationToken);

        await AuditAsync(inviterCompanyId, "Subcontractor configured", name, contactEmail, cancellationToken);

        return new SubcontractorOnboardingResultDto(invite.Token, passcode, invite.ExpiresUtc, subcontractorCompanyId, invite.Id);
    }

    /// <summary>Returns the stored configuration for a subcontractor company (own tenant only, R15), or null.</summary>
    public async Task<SubcontractorOnboardingConfigDto?> GetBySubcontractorAsync(Guid subcontractorCompanyId, CancellationToken cancellationToken = default)
    {
        var config = await _configs.GetBySubcontractorCompanyAsync(subcontractorCompanyId, cancellationToken);
        if (config is null)
        {
            return null;
        }

        // R15: only the inviting tenant may read its own configuration (fail-open only when unauthenticated in tests).
        var tenant = await ResolveTenantAsync(cancellationToken);
        if (tenant is not null && config.InviterCompanyId != tenant)
        {
            return null;
        }

        return ToDto(config);
    }

    /// <summary>
    /// Evaluates Gate 1 for a subcontractor against current data (R3). A company with no configuration — or one
    /// owned by another tenant (R15) — clears vacuously (no "required before work" requirements apply).
    /// </summary>
    public async Task<Gate1StatusDto> EvaluateGate1Async(Guid subcontractorCompanyId, CancellationToken cancellationToken = default)
    {
        var config = await _configs.GetBySubcontractorCompanyAsync(subcontractorCompanyId, cancellationToken);
        if (config is null)
        {
            return new Gate1StatusDto(true, Array.Empty<Gate1RequirementDto>());
        }

        // R15: only the inviting tenant may read its subcontractor's gate; others see the vacuous cleared result.
        var tenant = await ResolveTenantAsync(cancellationToken);
        if (tenant is not null && config.InviterCompanyId != tenant)
        {
            return new Gate1StatusDto(true, Array.Empty<Gate1RequirementDto>());
        }

        var documents = await _documents.GetByCompanyAsync(subcontractorCompanyId, cancellationToken);
        return Gate1Evaluator.Evaluate(config, documents, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <summary>
    /// Lists the caller's subcontractors whose live RAMS is due for re-review under its configured cycle, as of
    /// <paramref name="asOf"/> (spec §4; beyond PRD v6.4 — informational only, never expires an approval). Scoped
    /// to the inviting tenant (R15); empty when unauthenticated or when no RAMS repository is configured.
    /// </summary>
    public async Task<IReadOnlyList<RamsReviewDueDto>> GetSubcontractorsDueForRamsReviewAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        if (tenant is null || _rams is null)
        {
            return Array.Empty<RamsReviewDueDto>();
        }

        var configs = await _configs.GetByInviterCompanyAsync(tenant.Value, cancellationToken);
        var due = new List<RamsReviewDueDto>();
        foreach (var config in configs)
        {
            var item = await BuildReviewDueAsync(config, asOf, cancellationToken);
            if (item is not null)
            {
                due.Add(item);
            }
        }

        return due;
    }

    /// <summary>
    /// Builds the review-due row for one configuration, or null when it has no cycle, no linked RAMS family, no
    /// approved live version, or the live version is not yet due. Reads are scoped to the reviewing company (R15).
    /// </summary>
    private async Task<RamsReviewDueDto?> BuildReviewDueAsync(SubcontractorOnboardingConfig config, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        if (_rams is null || config.RamsReviewCycleMonths is not { } months || config.RamsFamilyId is not { } familyId)
        {
            return null;
        }

        var family = await _rams.GetByFamilyAsync(config.InviterCompanyId, familyId, cancellationToken);
        var live = family.FirstOrDefault(r => r.IsLive);
        if (live?.ReviewedUtc is not { } approvedUtc)
        {
            return null;
        }

        if (RamsReviewCycle.DueUtc(months, approvedUtc) is not { } dueUtc || asOf < dueUtc)
        {
            return null;
        }

        return new RamsReviewDueDto(config.SubcontractorCompanyId, live.ContractorName, months, approvedUtc, dueUtc);
    }

    /// <summary>The signed-in tenant's company id, or null when unauthenticated (R15).</summary>
    private async Task<Guid?> ResolveTenantAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return null;
        }

        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return user.CompanyId;
    }

    /// <summary>The signed-in user's id for invite attribution, or null.</summary>
    private async Task<Guid?> CurrentUserIdAsync(CancellationToken cancellationToken) =>
        _currentUser is null ? null : (await _currentUser.GetCurrentAsync(cancellationToken)).UserId;

    /// <summary>
    /// Resolves the main contractor's induction template the subcontractor's operatives must complete (Gate 4).
    /// The induction is always the MC's own (§6.1): reuse the MC's existing template when it has one, else create
    /// one seeded from the shipped default and shaped by the configured induction settings. Returns null only when
    /// no induction service is wired (unit tests that run the set-up flow bare).
    /// </summary>
    private async Task<Guid?> ResolveOrCreateInductionTemplateAsync(
        Guid inviterCompanyId, int validityDays, int passMark, int attemptLimit, CancellationToken cancellationToken)
    {
        if (_inductions is null)
        {
            return null;
        }

        var existing = await _inductions.GetTemplatesAsync(inviterCompanyId, cancellationToken);
        if (existing.Count > 0)
        {
            return existing[0].Id;
        }

        return await _inductions.CreateDefaultTemplateAsync(
            new CreateInductionTemplateRequest(inviterCompanyId, "Site induction", validityDays, passMark, attemptLimit),
            cancellationToken);
    }

    /// <summary>Records an audit entry, best-effort — a failure never breaks the setup (SF-20).</summary>
    private async Task AuditAsync(Guid companyId, string action, string entity, string? reference, CancellationToken cancellationToken)
    {
        if (_audit is null)
        {
            return;
        }

        try
        {
            var actor = _currentUser is null ? "System" : (await _currentUser.GetCurrentAsync(cancellationToken)).Name;
            await _audit.RecordAsync(new RecordAuditRequest(companyId, actor, action, entity, reference, "Onboarding"), cancellationToken);
        }
        catch
        {
            // Audit is a side effect, never the operation's success criterion.
        }
    }

    /// <summary>Maps a configuration to its DTO.</summary>
    private static SubcontractorOnboardingConfigDto ToDto(SubcontractorOnboardingConfig c) => new(
        c.Id,
        c.SubcontractorCompanyId,
        c.TradeInviteId,
        c.AccessPeriodMonths,
        c.RequiredDocuments.Select(d => new RequiredDocumentSelection(d.Heading, d.RequiredBeforeWork)).ToList(),
        c.SsstsRequired,
        c.SmstsRequired,
        c.InductionValidityDays,
        c.InductionPassMark,
        c.InductionAttemptLimit,
        c.RamsReviewCycleMonths);

    /// <summary>Trims a value to null when blank.</summary>
    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
