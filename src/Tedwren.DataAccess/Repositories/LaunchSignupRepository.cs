using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ILaunchSignupRepository"/> for launch-list signups (commercial database).</summary>
public sealed class LaunchSignupRepository : AdminRepositoryBase, ILaunchSignupRepository
{
    private const string Columns =
        "Id, Email, Source, UtmSource, UtmMedium, UtmCampaign, CreatedUtc, Notified, NotifiedUtc";

    /// <summary>Creates the repository over the commercial connection factory.</summary>
    public LaunchSignupRepository(IAdminDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Inserts a new signup.</summary>
    public Task AddAsync(LaunchSignup signup, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO LaunchSignups (Id, Email, Source, UtmSource, UtmMedium, UtmCampaign, CreatedUtc, Notified, NotifiedUtc) " +
            "VALUES (@Id, @Email, @Source, @UtmSource, @UtmMedium, @UtmCampaign, @CreatedUtc, @Notified, @NotifiedUtc)",
            ToParameters(signup), cancellationToken);

    /// <summary>Returns the signup for an email (case-insensitive), or null.</summary>
    public async Task<LaunchSignup?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM LaunchSignups WHERE LOWER(Email) = @Email",
            new { Email = (email ?? string.Empty).ToLowerInvariant() }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns every signup, newest first.</summary>
    public async Task<IReadOnlyList<LaunchSignup>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM LaunchSignups ORDER BY CreatedUtc DESC", null, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Updates a signup's mutable fields (notified flag/timestamp).</summary>
    public Task UpdateAsync(LaunchSignup signup, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE LaunchSignups SET Notified = @Notified, NotifiedUtc = @NotifiedUtc WHERE Id = @Id",
            ToParameters(signup), cancellationToken);

    /// <summary>Flattens a signup to Dapper parameters.</summary>
    private static object ToParameters(LaunchSignup s) => new
    {
        s.Id,
        s.Email,
        s.Source,
        s.UtmSource,
        s.UtmMedium,
        s.UtmCampaign,
        s.CreatedUtc,
        s.Notified,
        s.NotifiedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static LaunchSignup ToEntity(Row r) => new()
    {
        Id = r.Id,
        Email = r.Email,
        Source = r.Source,
        UtmSource = r.UtmSource,
        UtmMedium = r.UtmMedium,
        UtmCampaign = r.UtmCampaign,
        CreatedUtc = r.CreatedUtc,
        Notified = r.Notified,
        NotifiedUtc = r.NotifiedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string Email, string? Source, string? UtmSource, string? UtmMedium, string? UtmCampaign,
        DateTimeOffset CreatedUtc, bool Notified, DateTimeOffset? NotifiedUtc);
}
