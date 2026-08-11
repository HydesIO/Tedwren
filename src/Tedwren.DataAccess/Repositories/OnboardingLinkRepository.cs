using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IOnboardingLinkRepository"/> for self-service onboarding links (SF-4, SUB-2).</summary>
public sealed class OnboardingLinkRepository : RepositoryBase, IOnboardingLinkRepository
{
    private const string Columns =
        "Id, Token, PasscodeHash, CompanyId, Name, Trade, ExpiresUtc, Status, PersonId, EngagementId, CreatedByUserId, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public OnboardingLinkRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Inserts a new onboarding link.</summary>
    public Task AddAsync(OnboardingLink link, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO OnboardingLinks (Id, Token, PasscodeHash, CompanyId, Name, Trade, ExpiresUtc, Status, " +
            "PersonId, EngagementId, CreatedByUserId, CreatedUtc) VALUES " +
            "(@Id, @Token, @PasscodeHash, @CompanyId, @Name, @Trade, @ExpiresUtc, @Status, " +
            "@PersonId, @EngagementId, @CreatedByUserId, @CreatedUtc)",
            ToParameters(link), cancellationToken);

    /// <summary>Returns the link for a token, or null.</summary>
    public async Task<OnboardingLink?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM OnboardingLinks WHERE Token = @Token", new { Token = token }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Updates a link's mutable fields (status, person/engagement).</summary>
    public Task UpdateAsync(OnboardingLink link, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE OnboardingLinks SET Name = @Name, Trade = @Trade, Status = @Status, " +
            "PersonId = @PersonId, EngagementId = @EngagementId WHERE Id = @Id",
            ToParameters(link), cancellationToken);

    /// <summary>Flattens a link to Dapper parameters (enum as int).</summary>
    private static object ToParameters(OnboardingLink l) => new
    {
        l.Id,
        l.Token,
        l.PasscodeHash,
        l.CompanyId,
        l.Name,
        l.Trade,
        l.ExpiresUtc,
        Status = (int)l.Status,
        l.PersonId,
        l.EngagementId,
        l.CreatedByUserId,
        l.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static OnboardingLink ToEntity(Row r) => new()
    {
        Id = r.Id,
        Token = r.Token,
        PasscodeHash = r.PasscodeHash,
        CompanyId = r.CompanyId,
        Name = r.Name,
        Trade = r.Trade,
        ExpiresUtc = r.ExpiresUtc,
        Status = (OnboardingLinkStatus)r.Status,
        PersonId = r.PersonId,
        EngagementId = r.EngagementId,
        CreatedByUserId = r.CreatedByUserId,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string Token, string? PasscodeHash, Guid CompanyId, string? Name, string? Trade,
        DateTimeOffset ExpiresUtc, int Status, Guid? PersonId, Guid? EngagementId, Guid? CreatedByUserId, DateTimeOffset CreatedUtc);
}

/// <summary>Dapper <see cref="IImageStore"/> storing private binary assets (card photos, R9).</summary>
public sealed class ImageStore : RepositoryBase, IImageStore
{
    /// <summary>Creates the store over the connection factory.</summary>
    public ImageStore(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Stores bytes and returns the image reference (its id).</summary>
    public async Task<string> SaveAsync(byte[] bytes, string contentType, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO StoredImages (Id, ContentType, Bytes, CreatedUtc) VALUES (@Id, @ContentType, @Bytes, @CreatedUtc)",
            new { Id = id, ContentType = contentType, Bytes = bytes, CreatedUtc = DateTimeOffset.UtcNow }, cancellationToken);
        return id.ToString();
    }

    /// <summary>Returns a stored image by reference, or null.</summary>
    public async Task<StoredImage?> GetAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(reference, out var id))
        {
            return null;
        }

        var row = await QuerySingleOrDefaultAsync<Row>(
            "SELECT Id, ContentType, Bytes, CreatedUtc FROM StoredImages WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : new StoredImage { Id = row.Id, ContentType = row.ContentType, Bytes = row.Bytes, CreatedUtc = row.CreatedUtc };
    }

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(Guid Id, string ContentType, byte[] Bytes, DateTimeOffset CreatedUtc);
}
