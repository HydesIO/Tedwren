using System.Globalization;
using System.Text;
using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Audit;

/// <summary>
/// The audit-trail service (SF-20): free-text + date-range search, CSV export, and recording new entries.
/// Data-store agnostic.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly IAuditRepository _audit;

    /// <summary>Creates the service over the audit repository.</summary>
    public AuditService(IAuditRepository audit) => _audit = audit;

    /// <summary>Searches the audit trail.</summary>
    public async Task<IReadOnlyList<AuditEntryDto>> SearchAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var entries = await _audit.SearchAsync(companyId, text, from, to, cancellationToken);
        return entries.Select(ToDto).ToList();
    }

    /// <summary>Exports the matching entries as CSV (SF-20).</summary>
    public async Task<string> ExportCsvAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var entries = await _audit.SearchAsync(companyId, text, from, to, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("When,Actor,Action,Entity,Reference,Category");
        foreach (var e in entries)
        {
            sb.Append(Csv(e.OccurredUtc.ToString("u", CultureInfo.InvariantCulture))).Append(',')
                .Append(Csv(e.Actor)).Append(',')
                .Append(Csv(e.Action)).Append(',')
                .Append(Csv(e.Entity)).Append(',')
                .Append(Csv(e.Reference)).Append(',')
                .Append(Csv(e.Category)).Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>Records an audit entry.</summary>
    public Task RecordAsync(RecordAuditRequest request, CancellationToken cancellationToken = default) =>
        _audit.AddAsync(new AuditEntry
        {
            CompanyId = request.CompanyId,
            Actor = request.Actor,
            Action = request.Action,
            Entity = request.Entity,
            Reference = request.Reference,
            Category = request.Category,
        }, cancellationToken);

    /// <summary>Maps an audit entry to its DTO.</summary>
    private static AuditEntryDto ToDto(AuditEntry e) =>
        new(e.Id, e.OccurredUtc, e.Actor, e.Action, e.Entity, e.Reference, e.Category);

    /// <summary>Escapes a CSV field, quoting when it contains a comma, quote or newline.</summary>
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
