using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IQualificationCardRepository"/>. SQL is ANSI-portable across both engines.</summary>
public sealed class QualificationCardRepository : RepositoryBase, IQualificationCardRepository
{
    private const string Columns =
        "Id, PersonId, QualificationTypeId, CardNumber, HolderName, IssuedOn, ExpiresOn, CaptureSource, " +
        "ImageReference, VerificationState, NeedsReview, ConfirmedBy, ConfirmedUtc, SupersedesCardId, " +
        "SupersededByCardId, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public QualificationCardRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns all cards held by a person (including superseded), newest first.</summary>
    public async Task<IReadOnlyList<QualificationCard>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM QualificationCards WHERE PersonId = @PersonId ORDER BY CreatedUtc DESC",
            new { PersonId = personId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a card by id, or null.</summary>
    public async Task<QualificationCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM QualificationCards WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns current (non-superseded) cards that carry an expiry date (the expiry engine's input).</summary>
    public async Task<IReadOnlyList<QualificationCard>> GetCurrentWithExpiryAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM QualificationCards WHERE SupersededByCardId IS NULL AND ExpiresOn IS NOT NULL",
            null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Counts current (non-superseded) cards per qualification type.</summary>
    public async Task<IReadOnlyDictionary<Guid, int>> GetHeldByCountsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<HeldByRow>(
            "SELECT QualificationTypeId AS TypeId, COUNT(*) AS Count FROM QualificationCards " +
            "WHERE SupersededByCardId IS NULL GROUP BY QualificationTypeId",
            null, cancellationToken);
        return rows.ToDictionary(r => r.TypeId, r => r.Count);
    }

    /// <summary>Inserts a new card.</summary>
    public Task AddAsync(QualificationCard card, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO QualificationCards (Id, PersonId, QualificationTypeId, CardNumber, HolderName, IssuedOn, " +
            "ExpiresOn, CaptureSource, ImageReference, VerificationState, NeedsReview, ConfirmedBy, ConfirmedUtc, " +
            "SupersedesCardId, SupersededByCardId, CreatedUtc) " +
            "VALUES (@Id, @PersonId, @QualificationTypeId, @CardNumber, @HolderName, @IssuedOn, @ExpiresOn, " +
            "@CaptureSource, @ImageReference, @VerificationState, @NeedsReview, @ConfirmedBy, @ConfirmedUtc, " +
            "@SupersedesCardId, @SupersededByCardId, @CreatedUtc)",
            ToParam(card),
            cancellationToken);

    /// <summary>Updates the mutable fields of an existing card (confirmation and supersession).</summary>
    public Task UpdateAsync(QualificationCard card, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE QualificationCards SET VerificationState = @VerificationState, NeedsReview = @NeedsReview, " +
            "ConfirmedBy = @ConfirmedBy, ConfirmedUtc = @ConfirmedUtc, SupersededByCardId = @SupersededByCardId " +
            "WHERE Id = @Id",
            ToParam(card),
            cancellationToken);

    /// <summary>Flattens a card to Dapper parameters (enums stored as their integer values).</summary>
    private static object ToParam(QualificationCard c) => new
    {
        c.Id,
        c.PersonId,
        c.QualificationTypeId,
        c.CardNumber,
        c.HolderName,
        c.IssuedOn,
        c.ExpiresOn,
        CaptureSource = (int)c.CaptureSource,
        c.ImageReference,
        VerificationState = (int)c.VerificationState,
        c.NeedsReview,
        c.ConfirmedBy,
        c.ConfirmedUtc,
        c.SupersedesCardId,
        c.SupersededByCardId,
        c.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static QualificationCard ToEntity(Row r) => new()
    {
        Id = r.Id,
        PersonId = r.PersonId,
        QualificationTypeId = r.QualificationTypeId,
        CardNumber = r.CardNumber,
        HolderName = r.HolderName,
        IssuedOn = r.IssuedOn,
        ExpiresOn = r.ExpiresOn,
        CaptureSource = (CardCaptureSource)r.CaptureSource,
        ImageReference = r.ImageReference,
        VerificationState = (CardVerificationState)r.VerificationState,
        NeedsReview = r.NeedsReview,
        ConfirmedBy = r.ConfirmedBy,
        ConfirmedUtc = r.ConfirmedUtc,
        SupersedesCardId = r.SupersedesCardId,
        SupersededByCardId = r.SupersededByCardId,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into (enums read as ints).</summary>
    private sealed record Row(
        Guid Id, Guid PersonId, Guid QualificationTypeId, string? CardNumber, string? HolderName,
        DateOnly? IssuedOn, DateOnly? ExpiresOn, int CaptureSource, string? ImageReference,
        int VerificationState, bool NeedsReview, string? ConfirmedBy, DateTimeOffset? ConfirmedUtc,
        Guid? SupersedesCardId, Guid? SupersededByCardId, DateTimeOffset CreatedUtc);

    /// <summary>Row shape for the grouped holder-count query.</summary>
    private sealed record HeldByRow(Guid TypeId, int Count);
}
