using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Services;
using Tedwren.UiComponents.SampleData;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IAuditService"/> for client <c>DataSource=Mock</c>. It wraps the existing sample-data audit
/// entries and maps them to the shared DTOs so the Audit Log screen looks exactly as before. Free-text and
/// date-range filtering are applied in-proc; recording is demo-only (the sample data is static).
/// </summary>
public sealed class ClientMockAuditService : IAuditService
{
    private readonly IListSampleDataService _list;

    /// <summary>Creates the service over the sample-data list service.</summary>
    public ClientMockAuditService(IListSampleDataService list) => _list = list;

    /// <summary>Maps and filters the sample audit entries by free text and date range (SF-20).</summary>
    public Task<IReadOnlyList<AuditEntryDto>> SearchAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AuditEntryDto> results = _list.GetAuditEntries()
            .Where(e => Matches(e, text) && InRange(e, from, to))
            .OrderByDescending(e => e.Timestamp)
            .Select(ToDto)
            .ToList();
        return Task.FromResult(results);
    }

    /// <summary>Renders the matching sample entries as CSV (SF-20).</summary>
    public async Task<string> ExportCsvAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var entries = await SearchAsync(companyId, text, from, to, cancellationToken);
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("When,Actor,Action,Entity,Reference,Category");
        foreach (var e in entries)
        {
            builder.AppendLine(string.Join(',', new[]
            {
                Csv(e.OccurredUtc.ToString("u")), Csv(e.Actor), Csv(e.Action),
                Csv(e.Entity), Csv(e.Reference), Csv(e.Category),
            }));
        }

        return builder.ToString();
    }

    /// <summary>Demo record — sample data is static, so nothing is persisted.</summary>
    public Task RecordAsync(RecordAuditRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>Maps a sample audit entry to the shared DTO. The sample data has no reference field.</summary>
    private static AuditEntryDto ToDto(AuditEntry e) =>
        new(Guid.Empty, new DateTimeOffset(e.Timestamp, TimeSpan.Zero), e.Actor, e.Action, e.Entity, null, e.Category);

    /// <summary>Case-insensitive free-text match across actor/action/entity/category.</summary>
    private static bool Matches(AuditEntry e, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var t = text.Trim();
        return e.Actor.Contains(t, StringComparison.OrdinalIgnoreCase)
            || e.Action.Contains(t, StringComparison.OrdinalIgnoreCase)
            || e.Entity.Contains(t, StringComparison.OrdinalIgnoreCase)
            || e.Category.Contains(t, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Inclusive date-range filter over the entry timestamp.</summary>
    private static bool InRange(AuditEntry e, DateOnly? from, DateOnly? to)
    {
        var day = DateOnly.FromDateTime(e.Timestamp);
        if (from is { } f && day < f)
        {
            return false;
        }

        return to is not { } t || day <= t;
    }

    /// <summary>Quotes a CSV field when it contains a comma, quote or newline (RFC 4180).</summary>
    private static string Csv(string? value)
    {
        var v = value ?? string.Empty;
        return v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r')
            ? "\"" + v.Replace("\"", "\"\"") + "\""
            : v;
    }
}
