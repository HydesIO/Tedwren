using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Common;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Rams;

/// <summary>
/// The store-agnostic RAMS submission &amp; approval service (PRD §8.2). A submission gets an immediate reference;
/// the site manager approves / rejects / returns with a written reason; a resubmission is a new version in the same
/// family, leaving earlier versions intact (append-only, R4/R16). Everything is scoped to the reviewing company
/// (R15). The uploaded document is stored through <see cref="IImageStore"/> (no permanent public URL, R9) after
/// size/type validation.
/// </summary>
public sealed class RamsService : IRamsService
{
    /// <summary>Hours a submission may await review before it is flagged overdue (PRD §8.2 — the &gt;48h view).</summary>
    private const int OverdueHours = 48;

    private readonly IRamsRepository _rams;
    private readonly IImageStore _images;

    /// <summary>Creates the service over the RAMS repository and the document store.</summary>
    public RamsService(IRamsRepository rams, IImageStore images)
    {
        _rams = rams;
        _images = images;
    }

    /// <summary>Submits a RAMS (or the next version when a family id is supplied) and returns it with its reference.</summary>
    public async Task<RamsSubmissionDto> SubmitAsync(Guid companyId, SubmitRamsRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(request.ContractorName))
        {
            throw new ArgumentException("A contractor name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("A RAMS title is required.", nameof(request));
        }

        // Resubmission: verify the family belongs to the company and take the next version; otherwise a fresh family v1.
        var familyId = Guid.NewGuid();
        var version = 1;
        if (request.FamilyId is { } fam)
        {
            var maxVersion = await _rams.GetMaxVersionAsync(companyId, fam, cancellationToken);
            if (maxVersion == 0)
            {
                throw new ArgumentException("The RAMS to resubmit was not found.", nameof(request));
            }

            familyId = fam;
            version = maxVersion + 1;
        }

        string? fileReference = null;
        if (!string.IsNullOrWhiteSpace(request.FileBase64))
        {
            UploadValidation.Validate(request.FileBase64, request.FileContentType, UploadKind.Document);
            var bytes = Convert.FromBase64String(StripDataUrl(request.FileBase64));
            fileReference = await _images.SaveAsync(bytes, request.FileContentType ?? "application/pdf", cancellationToken);
        }

        var submission = new RamsSubmission
        {
            CompanyId = companyId,
            FamilyId = familyId,
            Version = version,
            Reference = BuildReference(),
            ContractorName = request.ContractorName.Trim(),
            Title = request.Title.Trim(),
            SiteId = request.SiteId,
            SiteName = string.IsNullOrWhiteSpace(request.SiteName) ? null : request.SiteName.Trim(),
            FileReference = fileReference,
            Status = RamsStatus.Submitted,
        };
        await _rams.AddAsync(submission, cancellationToken);
        return ToDto(submission);
    }

    /// <summary>Returns a company's RAMS submissions, newest first.</summary>
    public async Task<IReadOnlyList<RamsSubmissionDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        (await _rams.GetByCompanyAsync(companyId, cancellationToken)).Select(ToDto).ToList();

    /// <summary>Returns the submissions awaiting review (oldest first), each flagged when awaiting more than 48 hours.</summary>
    public async Task<IReadOnlyList<RamsSubmissionDto>> GetReviewQueueAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        (await _rams.GetByCompanyAsync(companyId, cancellationToken))
            .Where(r => r.Status == RamsStatus.Submitted)
            .OrderBy(r => r.SubmittedUtc)
            .Select(ToDto)
            .ToList();

    /// <summary>Approves a submitted RAMS and makes it the live version (spec Stage 3).</summary>
    public Task<bool> ApproveAsync(Guid companyId, Guid id, string reviewer, CancellationToken cancellationToken = default) =>
        DecideAsync(companyId, id, RamsStatus.Approved, reviewer, note: null, cancellationToken);

    /// <summary>Approves a submitted RAMS with reviewer comments (a required note) and makes it the live version (spec Stage 3).</summary>
    public Task<bool> ApproveWithCommentsAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default) =>
        DecideAsync(companyId, id, RamsStatus.ApprovedWithComments, reviewer, note, cancellationToken);

    /// <summary>Rejects a submitted RAMS with a required note.</summary>
    public Task<bool> RejectAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default) =>
        DecideAsync(companyId, id, RamsStatus.Rejected, reviewer, note, cancellationToken);

    /// <summary>Returns a submitted RAMS for changes with a required note.</summary>
    public Task<bool> ReturnAsync(Guid companyId, Guid id, string reviewer, string note, CancellationToken cancellationToken = default) =>
        DecideAsync(companyId, id, RamsStatus.Returned, reviewer, note, cancellationToken);

    /// <summary>
    /// Registers a RAMS from an already-stored document (spec Stage 2→3): the subcontractor uploaded its RAMS on
    /// the onboarding link and it is bridged into the review queue, reusing the stored file reference rather than
    /// re-uploading it. A supplied family id is used as-is — so the first upload seeds the family (v1) and each
    /// later upload resubmits into it as the next version — leaving earlier versions intact (append-only, R4/R16).
    /// </summary>
    public async Task<RamsSubmissionDto> RegisterFromDocumentAsync(Guid companyId, RegisterRamsFromDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(request.ContractorName))
        {
            throw new ArgumentException("A contractor name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("A RAMS title is required.", nameof(request));
        }

        // Unlike SubmitAsync, a supplied family id need not already exist: the bridge chooses the family up front
        // (the subcontractor's configured RamsFamilyId), so version = maxVersion + 1 gives v1 for a new family.
        var familyId = request.FamilyId ?? Guid.NewGuid();
        var version = request.FamilyId is { } fam
            ? await _rams.GetMaxVersionAsync(companyId, fam, cancellationToken) + 1
            : 1;

        var submission = new RamsSubmission
        {
            CompanyId = companyId,
            FamilyId = familyId,
            Version = version,
            Reference = BuildReference(),
            ContractorName = request.ContractorName.Trim(),
            Title = request.Title.Trim(),
            SiteId = request.SiteId,
            SiteName = string.IsNullOrWhiteSpace(request.SiteName) ? null : request.SiteName.Trim(),
            FileReference = string.IsNullOrWhiteSpace(request.FileReference) ? null : request.FileReference.Trim(),
            Status = RamsStatus.Submitted,
        };
        await _rams.AddAsync(submission, cancellationToken);
        return ToDto(submission);
    }

    /// <summary>Applies a review decision, scoped to the company (R15). Reject/return/approve-with-comments require a note; only a submitted RAMS can be decided.</summary>
    private async Task<bool> DecideAsync(Guid companyId, Guid id, RamsStatus decision, string reviewer, string? note, CancellationToken cancellationToken)
    {
        var submission = await _rams.GetAsync(id, cancellationToken);
        if (submission is null || submission.CompanyId != companyId)
        {
            return false;
        }

        if (submission.Status != RamsStatus.Submitted)
        {
            throw new InvalidOperationException($"A RAMS that is {submission.Status} cannot be reviewed again.");
        }

        if (RequiresNote(decision) && string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("A written note is required for this decision.", nameof(note));
        }

        submission.Status = decision;
        submission.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        submission.ReviewedBy = string.IsNullOrWhiteSpace(reviewer) ? "System" : reviewer.Trim();
        submission.ReviewedUtc = DateTimeOffset.UtcNow;

        // Spec Stage 3: an approval (with or without comments) makes this the live version operatives read and
        // sign; any earlier live version in the family is cleared. History is preserved — only the pointer moves.
        if (decision is RamsStatus.Approved or RamsStatus.ApprovedWithComments)
        {
            await PromoteToLiveAsync(submission, cancellationToken);
        }

        await _rams.UpdateAsync(submission, cancellationToken);
        return true;
    }

    /// <summary>Whether a decision needs a written note: reject, return, or approve-with-comments (the comments).</summary>
    private static bool RequiresNote(RamsStatus decision) =>
        decision is RamsStatus.Rejected or RamsStatus.Returned or RamsStatus.ApprovedWithComments;

    /// <summary>Marks the submission live and clears any earlier live version in its family (spec Stage 3; append-only preserved).</summary>
    private async Task PromoteToLiveAsync(RamsSubmission submission, CancellationToken cancellationToken)
    {
        submission.IsLive = true;
        var family = await _rams.GetByFamilyAsync(submission.CompanyId, submission.FamilyId, cancellationToken);
        foreach (var sibling in family)
        {
            if (sibling.Id != submission.Id && sibling.IsLive)
            {
                sibling.IsLive = false;
                await _rams.UpdateAsync(sibling, cancellationToken);
            }
        }
    }

    /// <summary>Builds a human-readable submission reference (date + short random suffix).</summary>
    private static string BuildReference() =>
        $"RAMS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    /// <summary>Strips a "data:...;base64," prefix from a base64 payload, if present.</summary>
    private static string StripDataUrl(string base64)
    {
        var comma = base64.IndexOf(',');
        return base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0 ? base64[(comma + 1)..] : base64;
    }

    /// <summary>Maps a submission to its DTO, computing the awaiting time and the &gt;48h overdue flag.</summary>
    private static RamsSubmissionDto ToDto(RamsSubmission r)
    {
        var awaitingHours = r.Status == RamsStatus.Submitted ? (DateTimeOffset.UtcNow - r.SubmittedUtc).TotalHours : 0;
        return new RamsSubmissionDto(
            r.Id, r.FamilyId, r.Version, r.Reference, r.ContractorName, r.Title, r.SiteId, r.SiteName,
            r.FileReference is not null, r.Status.ToString(), r.ReviewNote, r.ReviewedBy, r.ReviewedUtc, r.SubmittedUtc,
            Math.Round(awaitingHours, 1), r.Status == RamsStatus.Submitted && awaitingHours > OverdueHours, r.IsLive);
    }
}
