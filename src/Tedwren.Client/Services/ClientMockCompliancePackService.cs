using System.Text;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.CompliancePacks;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ICompliancePackService"/> for client <c>DataSource=Mock</c>. It fabricates a small set of
/// operatives (one with an expired card, one expiring soon) so the readiness gate (SUB-14), build, list,
/// revoke and recipient-view flows are demonstrable in-proc without a backend. Recipient downloads render CSV;
/// the ZIP/PDF formats are exercised in <c>Api</c> mode against the real service (SUB-16).
/// </summary>
public sealed class ClientMockCompliancePackService : ICompliancePackService
{
    /// <summary>The demo company the fabricated packs belong to.</summary>
    public static readonly Guid DemoCompanyId = Guid.Parse("55555555-5555-4555-8555-000000000001");

    /// <summary>The demo operatives the console offers for pack building (id → name).</summary>
    public static readonly IReadOnlyList<(Guid Id, string Name)> DemoOperatives = new[]
    {
        (Guid.Parse("55555555-5555-4555-8555-000000000010"), "M. Adeyemi"),
        (Guid.Parse("55555555-5555-4555-8555-000000000011"), "J. Fletcher"),
        (Guid.Parse("55555555-5555-4555-8555-000000000012"), "R. Okafor"),
    };

    private readonly Dictionary<Guid, StoredPack> _packs = new();

    /// <summary>Creates the service and seeds one already-sent demo pack so the list is populated.</summary>
    public ClientMockCompliancePackService()
    {
        var subjects = DemoOperatives.Select(o => Snapshot(o.Id, o.Name)).ToList();
        var pack = new StoredPack(
            Guid.NewGuid(), "Riverside Phase 2 — weekly", "Riverside Phase 2", "tok-demo-riverside", "RIVER26",
            "Alex Morgan", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(28), "Active", subjects)
        { Opened = 3, Downloaded = 1 };
        _packs[pack.Id] = pack;
    }

    /// <summary>Previews readiness for a selection (SUB-14).</summary>
    public Task<IReadOnlyList<ReadinessIssueDto>> PreviewReadinessAsync(Guid companyId, IReadOnlyList<Guid> operativeIds, CancellationToken cancellationToken = default) =>
        Task.FromResult(Readiness(operativeIds.Select(id => Snapshot(id, NameOf(id))).ToList()));

    /// <summary>Builds/sends a pack; refuses when blocking problems are unacknowledged (SUB-14).</summary>
    public Task<BuildPackResult> BuildAsync(BuildPackRequest request, CancellationToken cancellationToken = default)
    {
        var subjects = request.OperativeIds.Select(id => Snapshot(id, NameOf(id))).ToList();
        var issues = Readiness(subjects);
        if (issues.Any(i => i.Blocking) && !request.AcknowledgeIssues)
        {
            return Task.FromResult(new BuildPackResult(false, null, null, null, issues));
        }

        var passcode = string.IsNullOrWhiteSpace(request.Passcode) ? "MOCK" + Random.Shared.Next(1000, 9999) : request.Passcode.Trim();
        var pack = new StoredPack(
            Guid.NewGuid(), request.Title, null, "tok-" + Guid.NewGuid().ToString("N")[..12], passcode,
            request.CreatedBy, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(request.ExpiryDays is > 0 ? request.ExpiryDays.Value : 30),
            "Active", subjects);
        _packs[pack.Id] = pack;

        if (request.SupersedesPackId is { } prior && _packs.TryGetValue(prior, out var superseded))
        {
            superseded.Status = "Superseded";
        }

        return Task.FromResult(new BuildPackResult(true, pack.Id, pack.Token, passcode, Array.Empty<ReadinessIssueDto>()));
    }

    /// <summary>Lists the company's packs with tallies (SUB-20).</summary>
    public Task<IReadOnlyList<PackListItemDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PackListItemDto> items = _packs.Values
            .OrderByDescending(p => p.CreatedUtc)
            .Select(p => new PackListItemDto(p.Id, p.Title, p.SiteName, p.Status, p.CreatedBy, p.CreatedUtc, p.ExpiresUtc, p.Subjects.Count, p.Opened, p.Downloaded))
            .ToList();
        return Task.FromResult(items);
    }

    /// <summary>Recipient view via token + passcode (R8).</summary>
    public Task<PackAccessResult> ViewAsync(string token, string passcode, CancellationToken cancellationToken = default)
    {
        var pack = _packs.Values.FirstOrDefault(p => p.Token == token);
        if (pack is null || pack.Status != "Active")
        {
            return Task.FromResult(new PackAccessResult(false, "This link is not valid or has been withdrawn.", null));
        }

        if (!string.Equals(pack.Passcode, passcode, StringComparison.Ordinal))
        {
            return Task.FromResult(new PackAccessResult(false, "Incorrect passcode.", null));
        }

        pack.Opened++;
        return Task.FromResult(new PackAccessResult(true, null, ToView(pack)));
    }

    /// <summary>Recipient CSV download (SUB-16).</summary>
    public Task<byte[]?> DownloadCsvAsync(string token, string passcode, CancellationToken cancellationToken = default)
    {
        var pack = _packs.Values.FirstOrDefault(p => p.Token == token && p.Passcode == passcode && p.Status == "Active");
        if (pack is null)
        {
            return Task.FromResult<byte[]?>(null);
        }

        pack.Downloaded++;
        var sb = new StringBuilder("Operative,Qualification,Status,Expires,Verification\n");
        foreach (var s in pack.Subjects)
        {
            foreach (var c in s.Cards)
            {
                sb.Append(s.Name).Append(',').Append(c.QualificationName).Append(',').Append(c.StatusLabel).Append(',')
                    .Append(c.ExpiresOn?.ToString("yyyy-MM-dd") ?? string.Empty).Append(',').Append(c.VerificationLabel).Append('\n');
            }
        }

        return Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    /// <summary>ZIP download is exercised in <c>Api</c> mode against the real service (SUB-16).</summary>
    public Task<byte[]?> DownloadZipAsync(string token, string passcode, CancellationToken cancellationToken = default) => DownloadCsvAsync(token, passcode, cancellationToken);

    /// <summary>PDF download is exercised in <c>Api</c> mode against the real service (SUB-16).</summary>
    public Task<byte[]?> DownloadPdfAsync(string token, string passcode, CancellationToken cancellationToken = default) => DownloadCsvAsync(token, passcode, cancellationToken);

    /// <summary>Revokes a pack (SUB-21).</summary>
    public Task<bool> RevokeAsync(Guid companyId, Guid packId, CancellationToken cancellationToken = default)
    {
        if (!_packs.TryGetValue(packId, out var pack))
        {
            return Task.FromResult(false);
        }

        pack.Status = "Revoked";
        return Task.FromResult(true);
    }

    /// <summary>The fabricated compliance snapshot for a demo operative (varied so readiness has something to flag).</summary>
    private static PackSubjectDto Snapshot(Guid id, string name)
    {
        var cards = name switch
        {
            "M. Adeyemi" => new[]
            {
                new PackCardDto("CSCS — Skilled Worker", "Valid", ComplianceState.Compliant, new DateOnly(2027, 3, 1), "Customer-checked"),
                new PackCardDto("First Aid at Work", "Expiring soon", ComplianceState.AtRisk, new DateOnly(2026, 8, 20), "Read"),
            },
            "J. Fletcher" => new[]
            {
                new PackCardDto("CSCS — Skilled Worker", "Expired", ComplianceState.NonCompliant, new DateOnly(2026, 6, 1), "Customer-checked"),
            },
            _ => new[]
            {
                new PackCardDto("CSCS — Skilled Worker", "Valid", ComplianceState.Compliant, new DateOnly(2028, 1, 1), "CSCS-verified"),
            },
        };
        return new PackSubjectDto(id, name, cards);
    }

    /// <summary>Readiness rules mirroring the domain check (SUB-14).</summary>
    private static IReadOnlyList<ReadinessIssueDto> Readiness(IReadOnlyList<PackSubjectDto> subjects)
    {
        var issues = new List<ReadinessIssueDto>();
        foreach (var s in subjects)
        {
            foreach (var c in s.Cards)
            {
                if (c.State == ComplianceState.NonCompliant)
                {
                    issues.Add(new ReadinessIssueDto(s.Name, c.QualificationName, true, "Card has expired"));
                }
                else if (c.State == ComplianceState.AtRisk)
                {
                    issues.Add(new ReadinessIssueDto(s.Name, c.QualificationName, false, "Card is expiring soon"));
                }
            }
        }

        return issues;
    }

    private static string NameOf(Guid id) => DemoOperatives.FirstOrDefault(o => o.Id == id).Name ?? "Operative";

    private static CompliancePackViewDto ToView(StoredPack p) =>
        new(p.Id, p.Title, p.SiteName, null, null, p.ExpiresUtc, p.Subjects);

    /// <summary>Internal mutable pack record for the mock store.</summary>
    private sealed class StoredPack
    {
        public StoredPack(Guid id, string title, string? siteName, string token, string passcode, string createdBy,
            DateTimeOffset createdUtc, DateTimeOffset expiresUtc, string status, IReadOnlyList<PackSubjectDto> subjects)
        {
            Id = id; Title = title; SiteName = siteName; Token = token; Passcode = passcode; CreatedBy = createdBy;
            CreatedUtc = createdUtc; ExpiresUtc = expiresUtc; Status = status; Subjects = subjects;
        }

        public Guid Id { get; }
        public string Title { get; }
        public string? SiteName { get; }
        public string Token { get; }
        public string Passcode { get; }
        public string CreatedBy { get; }
        public DateTimeOffset CreatedUtc { get; }
        public DateTimeOffset ExpiresUtc { get; }
        public string Status { get; set; }
        public IReadOnlyList<PackSubjectDto> Subjects { get; }
        public int Opened { get; set; }
        public int Downloaded { get; set; }
    }
}
