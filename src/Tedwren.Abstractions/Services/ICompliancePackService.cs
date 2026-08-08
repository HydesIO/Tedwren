using Tedwren.Abstractions.Contracts.CompliancePacks;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The compliance-pack service (SUB-13–SUB-26, R7–R9). A pack is a fixed-at-send snapshot (R7) of chosen
/// operatives' compliance, built only after not-site-ready problems are surfaced and acknowledged (SUB-14),
/// reachable by a recipient with no account via an opaque token + passcode until expiry (R8, SUB-18), with
/// open/download tracking (SUB-20), revocation (SUB-21) and re-issue-supersedes semantics (R7). There are no
/// permanent public asset URLs — every access goes through the token/passcode/expiry gate (R9). Sender-facing
/// reads are company-scoped (R15); sending is restricted to nominated roles by the caller (SUB-22).
/// </summary>
public interface ICompliancePackService
{
    /// <summary>Previews the readiness problems for a selection without sending (SUB-14).</summary>
    Task<IReadOnlyList<ReadinessIssueDto>> PreviewReadinessAsync(Guid companyId, IReadOnlyList<Guid> operativeIds, CancellationToken cancellationToken = default);

    /// <summary>Builds and sends a pack; refuses (returning the issues) when blocking problems are unacknowledged (SUB-14).</summary>
    Task<BuildPackResult> BuildAsync(BuildPackRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists a company's packs with status and access tallies (SUB-20).</summary>
    Task<IReadOnlyList<PackListItemDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Recipient view via token + passcode; records an open when granted (R8, SUB-20).</summary>
    Task<PackAccessResult> ViewAsync(string token, string passcode, CancellationToken cancellationToken = default);

    /// <summary>Recipient download as CSV via token + passcode; records a download when granted (SUB-16/SUB-20).</summary>
    Task<byte[]?> DownloadCsvAsync(string token, string passcode, CancellationToken cancellationToken = default);

    /// <summary>Recipient download as a ZIP (CSV + manifest) via token + passcode (SUB-16).</summary>
    Task<byte[]?> DownloadZipAsync(string token, string passcode, CancellationToken cancellationToken = default);

    /// <summary>Recipient download as a PDF via token + passcode (SUB-16).</summary>
    Task<byte[]?> DownloadPdfAsync(string token, string passcode, CancellationToken cancellationToken = default);

    /// <summary>Revokes a pack so it is no longer accessible (SUB-21). Returns false if not found.</summary>
    Task<bool> RevokeAsync(Guid companyId, Guid packId, CancellationToken cancellationToken = default);
}
