using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.CompliancePacks;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Export;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.CompliancePacks;

/// <summary>
/// The compliance-pack business service (SUB-13–SUB-26, R7–R9). Builds a fixed-at-send snapshot (R7) of chosen
/// operatives' compliance, gates sending on acknowledged readiness problems (SUB-14), and serves the pack to
/// account-free recipients through a token + passcode + expiry gate with open/download tracking (R8, SUB-18,
/// SUB-20). Store-agnostic.
/// </summary>
public sealed class CompliancePackService : ICompliancePackService
{
    private const int DefaultExpiryDays = 30;   // SUB-18 / Q12

    private readonly ICompliancePackRepository _packs;
    private readonly IQualificationService _qualifications;
    private readonly IEngagementRepository _engagements;

    /// <summary>Creates the service over its repositories.</summary>
    public CompliancePackService(
        ICompliancePackRepository packs, IQualificationService qualifications, IEngagementRepository engagements)
    {
        _packs = packs;
        _qualifications = qualifications;
        _engagements = engagements;
    }

    /// <summary>Previews the readiness problems for a selection without sending (SUB-14).</summary>
    public async Task<IReadOnlyList<ReadinessIssueDto>> PreviewReadinessAsync(Guid companyId, IReadOnlyList<Guid> operativeIds, CancellationToken cancellationToken = default)
    {
        var subjects = await SnapshotAsync(companyId, operativeIds, cancellationToken);
        return PackReadiness.Evaluate(subjects).Select(ToIssueDto).ToList();
    }

    /// <summary>Builds and sends a pack; refuses when blocking problems are unacknowledged (SUB-14).</summary>
    public async Task<BuildPackResult> BuildAsync(BuildPackRequest request, CancellationToken cancellationToken = default)
    {
        var subjects = await SnapshotAsync(request.CompanyId, request.OperativeIds, cancellationToken);
        var issues = PackReadiness.Evaluate(subjects);

        // SUB-14: not-site-ready problems must be acknowledged before the pack can go out.
        if (PackReadiness.HasBlocking(issues) && !request.AcknowledgeIssues)
        {
            return new BuildPackResult(Sent: false, PackId: null, Token: null, Passcode: null, issues.Select(ToIssueDto).ToList());
        }

        var passcode = string.IsNullOrWhiteSpace(request.Passcode) ? PackPasscode.Generate() : request.Passcode.Trim();
        var pack = new CompliancePack
        {
            CompanyId = request.CompanyId,
            Title = request.Title,
            SiteId = request.SiteId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            CreatedBy = request.CreatedBy,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(request.ExpiryDays is > 0 ? request.ExpiryDays.Value : DefaultExpiryDays),
            Token = PackToken.Generate(),
            PasscodeHash = PackPasscode.Hash(passcode),
            Subjects = subjects,
        };
        await _packs.AddAsync(pack, cancellationToken);

        // R7: a re-issue supersedes the prior pack, which stays retained but inaccessible.
        if (request.SupersedesPackId is { } supersededId)
        {
            var prior = await _packs.GetAsync(request.CompanyId, supersededId, cancellationToken);
            if (prior is not null)
            {
                prior.Status = PackStatus.Superseded;
                prior.SupersededByPackId = pack.Id;
                await _packs.UpdateStatusAsync(prior, cancellationToken);
            }
        }

        return new BuildPackResult(Sent: true, pack.Id, pack.Token, passcode, Array.Empty<ReadinessIssueDto>());
    }

    /// <summary>Lists a company's packs with status and access tallies (SUB-20).</summary>
    public async Task<IReadOnlyList<PackListItemDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var packs = await _packs.GetByCompanyAsync(companyId, cancellationToken);
        var items = new List<PackListItemDto>(packs.Count);
        foreach (var pack in packs)
        {
            var (opened, downloaded) = await _packs.GetAccessTallyAsync(pack.Id, cancellationToken);
            items.Add(new PackListItemDto(
                pack.Id, pack.Title, pack.SiteName, pack.EffectiveStatus(now).ToString(), pack.CreatedBy,
                pack.CreatedUtc, pack.ExpiresUtc, pack.Subjects.Count, opened, downloaded));
        }

        return items;
    }

    /// <summary>Recipient view via token + passcode; records an open when granted (R8, SUB-20).</summary>
    public async Task<PackAccessResult> ViewAsync(string token, string passcode, CancellationToken cancellationToken = default)
    {
        var (pack, reason) = await AuthorizeAsync(token, passcode, cancellationToken);
        if (pack is null)
        {
            return new PackAccessResult(Granted: false, reason, null);
        }

        await _packs.AddAccessEventAsync(new PackAccessEvent { PackId = pack.Id, Kind = PackAccessKind.Opened }, cancellationToken);
        return new PackAccessResult(Granted: true, null, ToViewDto(pack));
    }

    /// <summary>Recipient CSV download via token + passcode (SUB-16/SUB-20).</summary>
    public Task<byte[]?> DownloadCsvAsync(string token, string passcode, CancellationToken cancellationToken = default) =>
        DownloadAsync(token, passcode, pack => System.Text.Encoding.UTF8.GetBytes(CsvWriter.Write(PackComposer.ToSheet(pack))), cancellationToken);

    /// <summary>Recipient ZIP download via token + passcode (SUB-16).</summary>
    public Task<byte[]?> DownloadZipAsync(string token, string passcode, CancellationToken cancellationToken = default) =>
        DownloadAsync(token, passcode, pack => PackComposer.ToZip(pack, CsvWriter.Write(PackComposer.ToSheet(pack))), cancellationToken);

    /// <summary>Recipient PDF download via token + passcode (SUB-16).</summary>
    public Task<byte[]?> DownloadPdfAsync(string token, string passcode, CancellationToken cancellationToken = default) =>
        DownloadAsync(token, passcode, pack => PdfWriter.Write(PackComposer.ToSheet(pack)), cancellationToken);

    /// <summary>Revokes a pack so it is no longer accessible (SUB-21).</summary>
    public async Task<bool> RevokeAsync(Guid companyId, Guid packId, CancellationToken cancellationToken = default)
    {
        var pack = await _packs.GetAsync(companyId, packId, cancellationToken);
        if (pack is null)
        {
            return false;
        }

        pack.Status = PackStatus.Revoked;
        await _packs.UpdateStatusAsync(pack, cancellationToken);
        return true;
    }

    /// <summary>Authorises a download, records it, and renders the bytes; null when access is refused.</summary>
    private async Task<byte[]?> DownloadAsync(string token, string passcode, Func<CompliancePack, byte[]> render, CancellationToken cancellationToken)
    {
        var (pack, _) = await AuthorizeAsync(token, passcode, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        await _packs.AddAccessEventAsync(new PackAccessEvent { PackId = pack.Id, Kind = PackAccessKind.Downloaded }, cancellationToken);
        return render(pack);
    }

    /// <summary>Validates the token, accessibility (SUB-18/SUB-21) and passcode (SUB-18); returns the pack or a reason.</summary>
    private async Task<(CompliancePack? Pack, string? Reason)> AuthorizeAsync(string token, string passcode, CancellationToken cancellationToken)
    {
        var pack = await _packs.GetByTokenAsync(token, cancellationToken);
        if (pack is null)
        {
            return (null, "This link is not valid.");
        }

        var status = pack.EffectiveStatus(DateTimeOffset.UtcNow);
        if (status != PackStatus.Active)
        {
            return (null, status switch
            {
                PackStatus.Revoked => "This pack has been revoked by the sender.",
                PackStatus.Superseded => "This pack has been replaced by a newer version.",
                _ => "This pack has expired.",
            });
        }

        return PackPasscode.Verify(passcode, pack.PasscodeHash)
            ? (pack, null)
            : (null, "Incorrect passcode.");
    }

    /// <summary>Snapshots each operative's current compliance into the fixed pack subjects (R7).</summary>
    private async Task<IReadOnlyList<PackSubject>> SnapshotAsync(Guid companyId, IReadOnlyList<Guid> operativeIds, CancellationToken cancellationToken)
    {
        var subjects = new List<PackSubject>(operativeIds.Count);
        foreach (var personId in operativeIds)
        {
            var engagement = await _engagements.GetByCompanyAndPersonAsync(companyId, personId, cancellationToken);
            var cards = await _qualifications.GetCardsForPersonAsync(personId, cancellationToken);
            subjects.Add(new PackSubject(
                personId,
                engagement?.Name ?? "Operative",
                cards.Select(ToPackCard).ToList()));
        }

        return subjects;
    }

    /// <summary>Maps a live card DTO to a frozen pack card, deriving currency from the compliance state.</summary>
    private static PackCard ToPackCard(Tedwren.Abstractions.Contracts.Qualifications.QualificationCardDto card) =>
        new(card.QualificationName, card.StatusLabel, ToCardStatus(card.State), card.ExpiresOn, card.VerificationLabel);

    /// <summary>Maps the neutral compliance state to the domain card currency for readiness (SUB-14).</summary>
    private static CardStatus ToCardStatus(ComplianceState state) => state switch
    {
        ComplianceState.NonCompliant => CardStatus.Expired,
        ComplianceState.AtRisk => CardStatus.ExpiringSoon,
        _ => CardStatus.Valid,
    };

    /// <summary>Maps the domain card currency back to a neutral compliance state for the view DTO.</summary>
    private static ComplianceState ToState(CardStatus status) => status switch
    {
        CardStatus.Expired => ComplianceState.NonCompliant,
        CardStatus.ExpiringSoon => ComplianceState.AtRisk,
        _ => ComplianceState.Compliant,
    };

    /// <summary>Maps a pack to the recipient-facing view DTO.</summary>
    private static CompliancePackViewDto ToViewDto(CompliancePack pack) => new(
        pack.Id, pack.Title, pack.SiteName, pack.FromDate, pack.ToDate, pack.ExpiresUtc,
        pack.Subjects.Select(s => new PackSubjectDto(
            s.PersonId, s.Name,
            s.Cards.Select(c => new PackCardDto(c.QualificationName, c.StatusLabel, ToState(c.Status), c.ExpiresOn, c.VerificationLabel)).ToList())).ToList());

    /// <summary>Maps a readiness issue to its DTO.</summary>
    private static ReadinessIssueDto ToIssueDto(PackReadiness.Issue issue) =>
        new(issue.SubjectName, issue.QualificationName, issue.Blocking, issue.Detail);
}
