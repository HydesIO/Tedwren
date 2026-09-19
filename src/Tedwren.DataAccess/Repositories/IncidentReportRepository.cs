using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IIncidentReportRepository"/> for accident / incident records (PRD §8.2), scoped by company (R15).</summary>
public sealed class IncidentReportRepository : RepositoryBase, IIncidentReportRepository
{
    private const string SelectColumns =
        "SELECT Id, CompanyId, Reference, Kind, Description, Location, OccurredUtc, InjuredPersonName, InjuryDetail, " +
        "Severity, ImmediateCause, RootCause, CorrectiveActions, Status, RiddorReportable, RiddorCategory, " +
        "ReportedBy, ReportedUtc, InvestigatedBy, ClosedUtc FROM IncidentReports";

    /// <summary>Creates the repository over the connection factory.</summary>
    public IncidentReportRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new incident record.</summary>
    public async Task AddAsync(IncidentReport report, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "INSERT INTO IncidentReports (Id, CompanyId, Reference, Kind, Description, Location, OccurredUtc, " +
            "InjuredPersonName, InjuryDetail, Severity, ImmediateCause, RootCause, CorrectiveActions, Status, " +
            "RiddorReportable, RiddorCategory, ReportedBy, ReportedUtc, InvestigatedBy, ClosedUtc) VALUES " +
            "(@Id, @CompanyId, @Reference, @Kind, @Description, @Location, @OccurredUtc, @InjuredPersonName, " +
            "@InjuryDetail, @Severity, @ImmediateCause, @RootCause, @CorrectiveActions, @Status, @RiddorReportable, " +
            "@RiddorCategory, @ReportedBy, @ReportedUtc, @InvestigatedBy, @ClosedUtc)",
            ToParams(report), cancellationToken);

    /// <summary>Returns a company's incident records, newest first.</summary>
    public async Task<IReadOnlyList<IncidentReport>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            SelectColumns + " WHERE CompanyId = @CompanyId ORDER BY ReportedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <summary>Returns a single incident record by id, or null if none exists.</summary>
    public async Task<IncidentReport?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(SelectColumns + " WHERE Id = @Id", new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    /// <summary>Updates an incident record's investigation/close-out state.</summary>
    public async Task UpdateAsync(IncidentReport report, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "UPDATE IncidentReports SET ImmediateCause = @ImmediateCause, RootCause = @RootCause, " +
            "CorrectiveActions = @CorrectiveActions, Status = @Status, RiddorReportable = @RiddorReportable, " +
            "RiddorCategory = @RiddorCategory, InvestigatedBy = @InvestigatedBy, ClosedUtc = @ClosedUtc WHERE Id = @Id",
            ToParams(report), cancellationToken);

    /// <summary>The parameter set shared by insert and update.</summary>
    private static object ToParams(IncidentReport r) => new
    {
        r.Id,
        r.CompanyId,
        r.Reference,
        Kind = (int)r.Kind,
        r.Description,
        r.Location,
        r.OccurredUtc,
        r.InjuredPersonName,
        r.InjuryDetail,
        Severity = (int)r.Severity,
        r.ImmediateCause,
        r.RootCause,
        r.CorrectiveActions,
        Status = (int)r.Status,
        r.RiddorReportable,
        r.RiddorCategory,
        r.ReportedBy,
        r.ReportedUtc,
        r.InvestigatedBy,
        r.ClosedUtc,
    };

    /// <summary>Maps a flat row to an incident record entity.</summary>
    private static IncidentReport Map(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Reference = r.Reference,
        Kind = (IncidentKind)r.Kind,
        Description = r.Description,
        Location = r.Location,
        OccurredUtc = r.OccurredUtc,
        InjuredPersonName = r.InjuredPersonName,
        InjuryDetail = r.InjuryDetail,
        Severity = (SafetySeverity)r.Severity,
        ImmediateCause = r.ImmediateCause,
        RootCause = r.RootCause,
        CorrectiveActions = r.CorrectiveActions,
        Status = (IncidentStatus)r.Status,
        RiddorReportable = r.RiddorReportable,
        RiddorCategory = r.RiddorCategory,
        ReportedBy = r.ReportedBy,
        ReportedUtc = r.ReportedUtc,
        InvestigatedBy = r.InvestigatedBy,
        ClosedUtc = r.ClosedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id,
        Guid CompanyId,
        string Reference,
        int Kind,
        string Description,
        string? Location,
        DateTimeOffset OccurredUtc,
        string? InjuredPersonName,
        string? InjuryDetail,
        int Severity,
        string? ImmediateCause,
        string? RootCause,
        string? CorrectiveActions,
        int Status,
        bool RiddorReportable,
        string? RiddorCategory,
        string ReportedBy,
        DateTimeOffset ReportedUtc,
        string? InvestigatedBy,
        DateTimeOffset? ClosedUtc);
}
