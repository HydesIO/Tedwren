using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Trades;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.CompliancePacks;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Trades;

/// <summary>
/// The trade self-service onboarding workflow (UAT-023, SUB-4/MC-27). Inviting a trade creates the trade company
/// (as a subcontractor, via the organisation service) plus a tokenised invitation; the trade uploads its own
/// documents (registration/RAMS/insurance/accreditations) from the link, stored privately (R9); a manager then
/// approves, rejects or returns the submission (the review state machine is <see cref="TradeOnboardingWorkflow"/>,
/// R18 vocabulary). The review queue and decisions are scoped to the inviting tenant (R15). Audit is best-effort
/// and email is a stub until PRD-Phase 7, so the shareable link is the primary hand-off (like onboarding/packs).
/// </summary>
public sealed class TradeOnboardingService : ITradeOnboardingService
{
    private readonly ITradeInviteRepository _invites;
    private readonly ICompanyRepository _companies;
    private readonly ICompanyDocumentRepository _documents;
    private readonly IOrganisationService _organisation;
    private readonly IImageStore _images;
    private readonly IAuditService? _audit;
    private readonly ICurrentUserService? _currentUser;
    private readonly IEmailSender? _email;
    private readonly ISubcontractorOnboardingConfigRepository? _subcontractorConfigs;

    /// <summary>Default invite lifetime (SUB-18: 30 days), mirroring the onboarding link.</summary>
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// The default documents a trade is asked to provide (SUB-4) — surfaced as guidance on the link. Used for a
    /// plain trade invite; when the invite was created by the subcontractor-onboarding wizard, the configured
    /// required-document headings (spec Stage 1 / §4) are surfaced instead (see <see cref="ResolveRequestedDocumentTypesAsync"/>).
    /// </summary>
    private static readonly IReadOnlyList<string> DefaultRequestedDocumentTypes =
        new[] { "Registration", "RAMS", "Insurance", "Accreditation" };

    /// <summary>
    /// Creates the service over its repositories and the organisation service. Audit, current-user and email
    /// collaborators are optional so unit tests can construct the service bare (the established pattern); the
    /// composition root supplies them. Without a current user the tenant scope is not enforced (tests run open).
    /// </summary>
    public TradeOnboardingService(
        ITradeInviteRepository invites,
        ICompanyRepository companies,
        ICompanyDocumentRepository documents,
        IOrganisationService organisation,
        IImageStore images,
        IAuditService? audit = null,
        ICurrentUserService? currentUser = null,
        IEmailSender? email = null,
        ISubcontractorOnboardingConfigRepository? subcontractorConfigs = null)
    {
        _invites = invites;
        _companies = companies;
        _documents = documents;
        _organisation = organisation;
        _images = images;
        _audit = audit;
        _currentUser = currentUser;
        _email = email;
        _subcontractorConfigs = subcontractorConfigs;
    }

    /// <summary>Creates the trade company and its invitation link, returning the token + (optional) passcode.</summary>
    public async Task<TradeInviteLinkDto> InviteTradeAsync(CreateTradeInviteRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        var name = (request.CompanyName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("A company name is required.", nameof(request));
        }

        var inviterCompanyId = await ResolveTenantAsync(cancellationToken)
            ?? throw new InvalidOperationException("A signed-in company is required to invite a trade.");

        // Create the trade company (a subcontractor) through the organisation service, which audits the creation.
        var contactName = Trimmed(request.ContactName);
        var contactEmail = Trimmed(request.ContactEmail);
        var tradeCompanyId = await _organisation.CreateCompanyAsync(new CreateCompanyRequest(
            name, Trimmed(request.Type), Trimmed(request.Trade), RegistrationNumber: null, Address: null,
            ContactName: contactName, ContactEmail: contactEmail, ContactPhone: null,
            OrgType: Tedwren.Abstractions.Common.OrgType.Subcontractor), cancellationToken);

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
            CompanyId = tradeCompanyId,
            InviterCompanyId = inviterCompanyId,
            ContactName = contactName,
            ContactEmail = contactEmail,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(LinkLifetime),
            CreatedByUserId = createdByUserId,
        };
        await _invites.AddAsync(invite, cancellationToken);
        await AuditAsync(inviterCompanyId, "Trade invited", name, contactEmail, cancellationToken);

        return new TradeInviteLinkDto(invite.Token, passcode, invite.ExpiresUtc);
    }

    /// <summary>Returns the trade-facing view for a token, or null when the link/passcode is invalid or expired.</summary>
    public async Task<TradeInviteViewDto?> GetByTokenAsync(string token, string? passcode, CancellationToken cancellationToken = default)
    {
        var invite = await AuthorizeAsync(token, passcode, cancellationToken);
        return invite is null ? null : await BuildViewAsync(invite, cancellationToken);
    }

    /// <summary>Uploads a document (optionally with a file) against the invited trade. Null when the link is invalid.</summary>
    public async Task<TradeInviteViewDto?> SubmitDocumentAsync(string token, string? passcode, SubmitTradeDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var invite = await AuthorizeAsync(token, passcode, cancellationToken);
        if (invite is null)
        {
            return null;
        }

        var type = string.IsNullOrWhiteSpace(request.Type) ? "Document" : request.Type.Trim();
        var name = string.IsNullOrWhiteSpace(request.Name) ? type : request.Name.Trim();

        string? fileReference = null;
        if (!string.IsNullOrWhiteSpace(request.FileBase64))
        {
            Common.UploadValidation.Validate(request.FileBase64, request.FileContentType, Common.UploadKind.Document);
            var bytes = Convert.FromBase64String(request.FileBase64);
            fileReference = await _images.SaveAsync(bytes, request.FileContentType ?? "application/octet-stream", cancellationToken);
        }

        await _documents.AddAsync(new CompanyDocument
        {
            CompanyId = invite.CompanyId,
            Name = name,
            Type = type,
            ExpiresOn = request.ExpiresOn,
            Reference = Trimmed(request.Reference),
            FileReference = fileReference,
        }, cancellationToken);

        return await BuildViewAsync(invite, cancellationToken);
    }

    /// <summary>Submits the trade's documents for manager review (Invited/Returned → Submitted).</summary>
    public async Task<TradeInviteViewDto?> SubmitForReviewAsync(string token, string? passcode, CancellationToken cancellationToken = default)
    {
        var invite = await AuthorizeAsync(token, passcode, cancellationToken);
        if (invite is null)
        {
            return null;
        }

        if (!TradeOnboardingWorkflow.CanSubmit(invite.Status))
        {
            throw new InvalidOperationException("This submission cannot be sent for review in its current state.");
        }

        invite.Status = TradeOnboardingStatus.Submitted;
        invite.SubmittedUtc = DateTimeOffset.UtcNow;
        invite.ReviewNote = null;   // clear any prior "returned" note now it is resubmitted
        await _invites.UpdateAsync(invite, cancellationToken);

        var company = await _companies.GetByIdAsync(invite.CompanyId, cancellationToken);
        await AuditAsync(invite.InviterCompanyId, "Trade submitted for review", company?.Name ?? "Trade", null, cancellationToken);
        // Best-effort notify the inviting company's contact that a submission is waiting (stub → outbox, PRD-Phase 7).
        await NotifyAsync((await _companies.GetByIdAsync(invite.InviterCompanyId, cancellationToken))?.ContactEmail,
            "A trade submission is ready to review",
            $"{company?.Name ?? "A trade"} has submitted their onboarding documents for review.", cancellationToken);

        return await BuildViewAsync(invite, cancellationToken);
    }

    /// <summary>Lists the trade submissions for the caller's tenant that a manager should review (R15).</summary>
    public async Task<IReadOnlyList<TradeReviewItemDto>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return Array.Empty<TradeReviewItemDto>();
        }

        var invites = await _invites.GetByInviterCompanyAsync(tenant.Value, cancellationToken);
        var items = new List<TradeReviewItemDto>();
        foreach (var invite in invites)
        {
            items.Add(await BuildReviewItemAsync(invite, cancellationToken));
        }

        return items;
    }

    /// <summary>Approves a submission awaiting review.</summary>
    public Task<TradeReviewItemDto?> ApproveAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default) =>
        DecideAsync(inviteId, TradeOnboardingWorkflow.CanApprove, TradeOnboardingStatus.Approved, request.Note, requireNote: false, "Trade approved", cancellationToken);

    /// <summary>Rejects a submission awaiting review (a note is required).</summary>
    public Task<TradeReviewItemDto?> RejectAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default) =>
        DecideAsync(inviteId, TradeOnboardingWorkflow.CanReject, TradeOnboardingStatus.Rejected, request.Note, requireNote: true, "Trade rejected", cancellationToken);

    /// <summary>Returns a submission to the trade for changes (a note is required).</summary>
    public Task<TradeReviewItemDto?> ReturnAsync(Guid inviteId, ReviewTradeRequest request, CancellationToken cancellationToken = default) =>
        DecideAsync(inviteId, TradeOnboardingWorkflow.CanReturn, TradeOnboardingStatus.Returned, request.Note, requireNote: true, "Trade returned", cancellationToken);

    /// <summary>Shared guard-and-mutate for the three review decisions (mirrors the timesheet workflow).</summary>
    private async Task<TradeReviewItemDto?> DecideAsync(
        Guid inviteId, Func<TradeOnboardingStatus, bool> canDecide, TradeOnboardingStatus newStatus,
        string? note, bool requireNote, string auditAction, CancellationToken cancellationToken)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        var invite = await _invites.GetByIdAsync(inviteId, cancellationToken);
        // R15: only the inviting tenant may review its own submissions (fail-open only when unauthenticated in tests).
        if (invite is null || (tenant is not null && invite.InviterCompanyId != tenant))
        {
            return null;
        }

        if (!canDecide(invite.Status))
        {
            throw new InvalidOperationException("This submission is not awaiting review.");
        }

        if (requireNote && string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("A note is required for this decision.", nameof(note));
        }

        invite.Status = newStatus;
        invite.DecidedBy = await ActorAsync(cancellationToken);
        invite.DecidedUtc = DateTimeOffset.UtcNow;
        invite.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await _invites.UpdateAsync(invite, cancellationToken);

        var company = await _companies.GetByIdAsync(invite.CompanyId, cancellationToken);
        await AuditAsync(invite.InviterCompanyId, auditAction, company?.Name ?? "Trade", invite.ReviewNote, cancellationToken);
        // Best-effort notify the trade of the outcome (stub → outbox, PRD-Phase 7).
        await NotifyAsync(invite.ContactEmail, $"Your onboarding was {newStatus.ToString().ToLowerInvariant()}",
            invite.ReviewNote ?? $"Your onboarding submission was {newStatus.ToString().ToLowerInvariant()}.", cancellationToken);

        return await BuildReviewItemAsync(invite, cancellationToken);
    }

    /// <summary>Validates the token, usability and passcode; returns the invite or null.</summary>
    private async Task<TradeInvite?> AuthorizeAsync(string token, string? passcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var invite = await _invites.GetByTokenAsync(token, cancellationToken);
        if (invite is null || !invite.IsUsable(DateTimeOffset.UtcNow))
        {
            return null;
        }

        if (invite.PasscodeHash is not null &&
            (string.IsNullOrEmpty(passcode) || !PackPasscode.Verify(passcode, invite.PasscodeHash)))
        {
            return null;
        }

        return invite;
    }

    /// <summary>Builds the trade-facing view (its uploaded docs never leak the private blob id, R9).</summary>
    private async Task<TradeInviteViewDto> BuildViewAsync(TradeInvite invite, CancellationToken cancellationToken)
    {
        var company = await _companies.GetByIdAsync(invite.CompanyId, cancellationToken);
        var docs = await _documents.GetByCompanyAsync(invite.CompanyId, cancellationToken);
        return new TradeInviteViewDto(
            company?.Name ?? "your company",
            invite.Status.ToString(),
            invite.ReviewNote,
            docs.Select(d => new TradeDocumentDto(d.Name, d.Type, d.ExpiresOn, d.FileReference is not null, FileReference: null)).ToList(),
            await ResolveRequestedDocumentTypesAsync(invite.CompanyId, cancellationToken));
    }

    /// <summary>
    /// The document headings the trade is asked to provide: the configured required-document headings when a
    /// subcontractor-onboarding configuration exists for the company (spec Stage 1 / §4), else the default
    /// trade-invite set (SUB-4). Falling back keeps the plain trade-invite path working when no configuration was
    /// created (phase independence).
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolveRequestedDocumentTypesAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (_subcontractorConfigs is not null)
        {
            var config = await _subcontractorConfigs.GetBySubcontractorCompanyAsync(companyId, cancellationToken);
            if (config is not null && config.RequiredDocuments.Count > 0)
            {
                return config.RequiredDocuments.Select(d => d.Heading).ToList();
            }
        }

        return DefaultRequestedDocumentTypes;
    }

    /// <summary>Builds the manager-facing review item (includes the blob reference so the manager can view files).</summary>
    private async Task<TradeReviewItemDto> BuildReviewItemAsync(TradeInvite invite, CancellationToken cancellationToken)
    {
        var company = await _companies.GetByIdAsync(invite.CompanyId, cancellationToken);
        var docs = await _documents.GetByCompanyAsync(invite.CompanyId, cancellationToken);
        return new TradeReviewItemDto(
            invite.Id,
            invite.CompanyId,
            company?.Name ?? "Trade",
            invite.ContactName,
            invite.ContactEmail,
            invite.Status.ToString(),
            invite.SubmittedUtc,
            invite.ReviewNote,
            docs.Select(d => new TradeDocumentDto(d.Name, d.Type, d.ExpiresOn, d.FileReference is not null, d.FileReference)).ToList());
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

    /// <summary>The current user's display name for decision/audit attribution, or "System".</summary>
    private async Task<string> ActorAsync(CancellationToken cancellationToken) =>
        _currentUser is null ? "System" : (await _currentUser.GetCurrentAsync(cancellationToken)).Name;

    /// <summary>Records an audit entry, best-effort — a failure never breaks the mutation (SF-20).</summary>
    private async Task AuditAsync(Guid companyId, string action, string entity, string? reference, CancellationToken cancellationToken)
    {
        if (_audit is null)
        {
            return;
        }

        try
        {
            var actor = await ActorAsync(cancellationToken);
            await _audit.RecordAsync(new RecordAuditRequest(companyId, actor, action, entity, reference, "Onboarding"), cancellationToken);
        }
        catch
        {
            // Audit is a side effect, never the operation's success criterion.
        }
    }

    /// <summary>Sends a notification email, best-effort — no-ops without a sender or recipient (stub → outbox, PRD-Phase 7).</summary>
    private async Task NotifyAsync(string? toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (_email is null || string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        try
        {
            await _email.SendAsync(toEmail, subject, body, cancellationToken);
        }
        catch
        {
            // Notification is a side effect; delivery failures never break the workflow.
        }
    }

    /// <summary>Trims a value to null when blank.</summary>
    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
