using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IAttendanceRepository"/>. Append-only (R4). SQL is ANSI-portable across both engines,
/// including the "open sign-in" logic (a presence-counting sign-in with no later sign-out at the same site).
/// Numeric enum values are used directly: Type SignIn=0/SignOut=1/OvernightFlag=2, Outcome Refused=2.
/// </summary>
public sealed class AttendanceRepository : RepositoryBase, IAttendanceRepository
{
    private const string Columns =
        "Id, PersonId, SiteId, PropertyId, Type, Outcome, Method, Latitude, Longitude, " +
        "WithinBoundary, Reason, CorrectsRecordId, OccurredUtc, CreatedUtc";

    // A sign-in (Type=0) that counts for presence (Outcome<>2) with no later counting sign-out (Type=1) at the same site.
    private const string OpenSignInPredicate =
        "a.Type = 0 AND a.Outcome <> 2 AND NOT EXISTS (SELECT 1 FROM Attendance s " +
        "WHERE s.PersonId = a.PersonId AND s.SiteId = a.SiteId AND s.Type = 1 AND s.Outcome <> 2 " +
        "AND s.OccurredUtc > a.OccurredUtc)";

    /// <summary>Creates the repository over the connection factory.</summary>
    public AttendanceRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Deletes an attendance record by id (demo-data teardown).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM Attendance WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Appends an attendance record.</summary>
    public Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Attendance (Id, PersonId, SiteId, PropertyId, Type, Outcome, Method, Latitude, Longitude, " +
            "WithinBoundary, Reason, CorrectsRecordId, OccurredUtc, CreatedUtc) " +
            "VALUES (@Id, @PersonId, @SiteId, @PropertyId, @Type, @Outcome, @Method, @Latitude, @Longitude, " +
            "@WithinBoundary, @Reason, @CorrectsRecordId, @OccurredUtc, @CreatedUtc)",
            ToParameters(record), cancellationToken);

    /// <summary>Returns the person's current open sign-in anywhere, or null.</summary>
    public async Task<AttendanceRecord?> GetOpenSignInForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Attendance a WHERE a.PersonId = @PersonId AND {OpenSignInPredicate} " +
            "ORDER BY a.OccurredUtc DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY",
            new { PersonId = personId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the person's current open sign-in at a site, or null.</summary>
    public async Task<AttendanceRecord?> GetOpenSignInForPersonAtSiteAsync(Guid personId, Guid siteId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM Attendance a WHERE a.PersonId = @PersonId AND a.SiteId = @SiteId AND {OpenSignInPredicate} " +
            "ORDER BY a.OccurredUtc DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY",
            new { PersonId = personId, SiteId = siteId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the open sign-ins at a site.</summary>
    public async Task<IReadOnlyList<AttendanceRecord>> GetOnSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Attendance a WHERE a.SiteId = @SiteId AND {OpenSignInPredicate} ORDER BY a.OccurredUtc DESC",
            new { SiteId = siteId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns open sign-ins older than the cutoff that have not yet been flagged overnight (Type=2).</summary>
    public async Task<IReadOnlyList<AttendanceRecord>> GetUnflaggedOvernightSignInsAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Attendance a WHERE a.OccurredUtc < @Cutoff AND {OpenSignInPredicate} " +
            "AND NOT EXISTS (SELECT 1 FROM Attendance f WHERE f.Type = 2 AND f.CorrectsRecordId = a.Id)",
            new { Cutoff = cutoffUtc }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns the most recent records for a site, newest first.</summary>
    public async Task<IReadOnlyList<AttendanceRecord>> GetBySiteAsync(Guid siteId, int take, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 1000);
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Attendance WHERE SiteId = @SiteId ORDER BY OccurredUtc DESC OFFSET 0 ROWS FETCH NEXT {limit} ROWS ONLY",
            new { SiteId = siteId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a person's records in <c>[fromUtc, toUtc)</c>, oldest first (timesheet roll-up).</summary>
    public async Task<IReadOnlyList<AttendanceRecord>> GetByPersonInRangeAsync(Guid personId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Attendance WHERE PersonId = @PersonId AND OccurredUtc >= @FromUtc AND OccurredUtc < @ToUtc ORDER BY OccurredUtc",
            new { PersonId = personId, FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Flattens a record to Dapper parameters (enums as ints).</summary>
    private static object ToParameters(AttendanceRecord r) => new
    {
        r.Id,
        r.PersonId,
        r.SiteId,
        r.PropertyId,
        Type = (int)r.Type,
        Outcome = (int)r.Outcome,
        Method = (int)r.Method,
        r.Latitude,
        r.Longitude,
        r.WithinBoundary,
        r.Reason,
        r.CorrectsRecordId,
        r.OccurredUtc,
        r.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static AttendanceRecord ToEntity(Row r) => new()
    {
        Id = r.Id,
        PersonId = r.PersonId,
        SiteId = r.SiteId,
        PropertyId = r.PropertyId,
        Type = (AttendanceEventType)r.Type,
        Outcome = (AttendanceOutcome)r.Outcome,
        Method = (SignInMethod)r.Method,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        WithinBoundary = r.WithinBoundary,
        Reason = r.Reason,
        CorrectsRecordId = r.CorrectsRecordId,
        OccurredUtc = r.OccurredUtc,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid PersonId, Guid SiteId, Guid? PropertyId, int Type, int Outcome, int Method,
        double? Latitude, double? Longitude, bool? WithinBoundary, string? Reason, Guid? CorrectsRecordId,
        DateTimeOffset OccurredUtc, DateTimeOffset CreatedUtc);
}
