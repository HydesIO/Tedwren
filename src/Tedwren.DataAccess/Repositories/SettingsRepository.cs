using System.Text.Json;
using Tedwren.Abstractions.Contracts.Settings;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="ISettingsRepository"/> storing a company's general settings as one JSON row.</summary>
public sealed class SettingsRepository : RepositoryBase, ISettingsRepository
{
    /// <summary>Creates the repository over the connection factory.</summary>
    public SettingsRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns the company's saved settings, deserialised from JSON, or null.</summary>
    public async Task<GeneralSettingsDto?> GetAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var json = await QuerySingleOrDefaultAsync<string>(
            "SELECT SettingsJson FROM CompanySettings WHERE CompanyId = @CompanyId",
            new { CompanyId = companyId }, cancellationToken);
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<GeneralSettingsDto>(json);
    }

    /// <summary>Upserts the company's settings as JSON (update-then-insert; portable across engines).</summary>
    public async Task SetAsync(Guid companyId, GeneralSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(settings);
        var now = DateTimeOffset.UtcNow;
        var updated = await ExecuteAsync(
            "UPDATE CompanySettings SET SettingsJson = @SettingsJson, UpdatedUtc = @UpdatedUtc WHERE CompanyId = @CompanyId",
            new { CompanyId = companyId, SettingsJson = json, UpdatedUtc = now }, cancellationToken);

        if (updated == 0)
        {
            await ExecuteAsync(
                "INSERT INTO CompanySettings (CompanyId, SettingsJson, UpdatedUtc) VALUES (@CompanyId, @SettingsJson, @UpdatedUtc)",
                new { CompanyId = companyId, SettingsJson = json, UpdatedUtc = now }, cancellationToken);
        }
    }
}
