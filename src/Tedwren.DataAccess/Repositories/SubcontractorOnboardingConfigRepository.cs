using System.Text.Json;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="ISubcontractorOnboardingConfigRepository"/> for subcontractor onboarding configurations
/// (spec Stage 1 / §4). The required-document headings are stored as a JSON column, mirroring how compliance
/// packs store their subjects and decisions their checks.
/// </summary>
public sealed class SubcontractorOnboardingConfigRepository : RepositoryBase, ISubcontractorOnboardingConfigRepository
{
    private const string Columns =
        "Id, InviterCompanyId, SubcontractorCompanyId, TradeInviteId, AccessPeriodMonths, RequiredDocumentsJson, " +
        "SsstsRequired, SmstsRequired, InductionValidityDays, InductionPassMark, InductionAttemptLimit, " +
        "RamsReviewCycleMonths, RamsFamilyId, LastRamsReviewReminderUtc, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public SubcontractorOnboardingConfigRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Inserts a new configuration.</summary>
    public Task AddAsync(SubcontractorOnboardingConfig config, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO SubcontractorOnboardingConfigs (Id, InviterCompanyId, SubcontractorCompanyId, TradeInviteId, " +
            "AccessPeriodMonths, RequiredDocumentsJson, SsstsRequired, SmstsRequired, InductionValidityDays, " +
            "InductionPassMark, InductionAttemptLimit, RamsReviewCycleMonths, RamsFamilyId, LastRamsReviewReminderUtc, CreatedUtc) VALUES " +
            "(@Id, @InviterCompanyId, @SubcontractorCompanyId, @TradeInviteId, @AccessPeriodMonths, @RequiredDocumentsJson, " +
            "@SsstsRequired, @SmstsRequired, @InductionValidityDays, @InductionPassMark, @InductionAttemptLimit, " +
            "@RamsReviewCycleMonths, @RamsFamilyId, @LastRamsReviewReminderUtc, @CreatedUtc)",
            ToParameters(config), cancellationToken);

    /// <summary>Returns the configuration for a trade invite, or null.</summary>
    public async Task<SubcontractorOnboardingConfig?> GetByTradeInviteAsync(Guid tradeInviteId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM SubcontractorOnboardingConfigs WHERE TradeInviteId = @TradeInviteId",
            new { TradeInviteId = tradeInviteId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the most recent configuration for a subcontractor company, or null.</summary>
    public async Task<SubcontractorOnboardingConfig?> GetBySubcontractorCompanyAsync(Guid subcontractorCompanyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM SubcontractorOnboardingConfigs WHERE SubcontractorCompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = subcontractorCompanyId }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns every configuration created by an inviting main contractor, newest first (R15 — the MC's own subcontractors).</summary>
    public async Task<IReadOnlyList<SubcontractorOnboardingConfig>> GetByInviterCompanyAsync(Guid inviterCompanyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM SubcontractorOnboardingConfigs WHERE InviterCompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = inviterCompanyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns every configuration with a RAMS review cycle set, across tenants (the reminder engine's candidate set).</summary>
    public async Task<IReadOnlyList<SubcontractorOnboardingConfig>> GetWithReviewCycleAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM SubcontractorOnboardingConfigs WHERE RamsReviewCycleMonths IS NOT NULL",
            new { }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Updates a configuration's mutable fields (including the RAMS family link set on first upload).</summary>
    public Task UpdateAsync(SubcontractorOnboardingConfig config, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE SubcontractorOnboardingConfigs SET AccessPeriodMonths = @AccessPeriodMonths, " +
            "RequiredDocumentsJson = @RequiredDocumentsJson, SsstsRequired = @SsstsRequired, SmstsRequired = @SmstsRequired, " +
            "InductionValidityDays = @InductionValidityDays, InductionPassMark = @InductionPassMark, " +
            "InductionAttemptLimit = @InductionAttemptLimit, RamsReviewCycleMonths = @RamsReviewCycleMonths, " +
            "RamsFamilyId = @RamsFamilyId, LastRamsReviewReminderUtc = @LastRamsReviewReminderUtc WHERE Id = @Id",
            ToParameters(config), cancellationToken);

    /// <summary>Records when a RAMS re-review reminder was last sent (idempotency marker), touching nothing else.</summary>
    public Task UpdateReviewReminderAsync(Guid id, DateTimeOffset remindedUtc, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE SubcontractorOnboardingConfigs SET LastRamsReviewReminderUtc = @RemindedUtc WHERE Id = @Id",
            new { Id = id, RemindedUtc = remindedUtc }, cancellationToken);

    /// <summary>Flattens a configuration to Dapper parameters (required documents serialised to JSON).</summary>
    private static object ToParameters(SubcontractorOnboardingConfig c) => new
    {
        c.Id,
        c.InviterCompanyId,
        c.SubcontractorCompanyId,
        c.TradeInviteId,
        c.AccessPeriodMonths,
        RequiredDocumentsJson = JsonSerializer.Serialize(c.RequiredDocuments),
        c.SsstsRequired,
        c.SmstsRequired,
        c.InductionValidityDays,
        c.InductionPassMark,
        c.InductionAttemptLimit,
        c.RamsReviewCycleMonths,
        c.RamsFamilyId,
        c.LastRamsReviewReminderUtc,
        c.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity (required documents deserialised from JSON).</summary>
    private static SubcontractorOnboardingConfig ToEntity(Row r) => new()
    {
        Id = r.Id,
        InviterCompanyId = r.InviterCompanyId,
        SubcontractorCompanyId = r.SubcontractorCompanyId,
        TradeInviteId = r.TradeInviteId,
        AccessPeriodMonths = r.AccessPeriodMonths,
        RequiredDocuments = string.IsNullOrWhiteSpace(r.RequiredDocumentsJson)
            ? Array.Empty<RequiredDocumentHeading>()
            : JsonSerializer.Deserialize<List<RequiredDocumentHeading>>(r.RequiredDocumentsJson) ?? new List<RequiredDocumentHeading>(),
        SsstsRequired = r.SsstsRequired,
        SmstsRequired = r.SmstsRequired,
        InductionValidityDays = r.InductionValidityDays,
        InductionPassMark = r.InductionPassMark,
        InductionAttemptLimit = r.InductionAttemptLimit,
        RamsReviewCycleMonths = r.RamsReviewCycleMonths,
        RamsFamilyId = r.RamsFamilyId,
        LastRamsReviewReminderUtc = r.LastRamsReviewReminderUtc,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid InviterCompanyId, Guid SubcontractorCompanyId, Guid TradeInviteId, int AccessPeriodMonths,
        string? RequiredDocumentsJson, bool SsstsRequired, bool SmstsRequired, int InductionValidityDays,
        int InductionPassMark, int InductionAttemptLimit, int? RamsReviewCycleMonths, Guid? RamsFamilyId,
        DateTimeOffset? LastRamsReviewReminderUtc, DateTimeOffset CreatedUtc);
}
