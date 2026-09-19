using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IOperativeDeviceRepository"/> for operative device bindings (M2).</summary>
public sealed class OperativeDeviceRepository : RepositoryBase, IOperativeDeviceRepository
{
    private const string Columns =
        "Id, PersonId, CompanyId, DeviceId, DeviceName, Status, RefreshTokenHash, RefreshTokenExpiresUtc, EnrolledUtc, LastSeenUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public OperativeDeviceRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<OperativeDevice?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM OperativeDevices WHERE DeviceId = @DeviceId", new { DeviceId = deviceId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    public async Task<OperativeDevice?> GetActiveByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM OperativeDevices WHERE PersonId = @PersonId AND Status = @Active",
            new { PersonId = personId, Active = (int)OperativeDeviceStatus.Active }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : ToEntity(row);
    }

    public Task AddAsync(OperativeDevice device, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO OperativeDevices (Id, PersonId, CompanyId, DeviceId, DeviceName, Status, RefreshTokenHash, " +
            "RefreshTokenExpiresUtc, EnrolledUtc, LastSeenUtc) VALUES " +
            "(@Id, @PersonId, @CompanyId, @DeviceId, @DeviceName, @Status, @RefreshTokenHash, " +
            "@RefreshTokenExpiresUtc, @EnrolledUtc, @LastSeenUtc)",
            ToParameters(device), cancellationToken);

    public Task UpdateAsync(OperativeDevice device, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE OperativeDevices SET PersonId = @PersonId, CompanyId = @CompanyId, DeviceName = @DeviceName, " +
            "Status = @Status, RefreshTokenHash = @RefreshTokenHash, RefreshTokenExpiresUtc = @RefreshTokenExpiresUtc, " +
            "LastSeenUtc = @LastSeenUtc WHERE Id = @Id",
            ToParameters(device), cancellationToken);

    /// <summary>Flattens a device to Dapper parameters (enum as int).</summary>
    private static object ToParameters(OperativeDevice d) => new
    {
        d.Id,
        d.PersonId,
        d.CompanyId,
        d.DeviceId,
        d.DeviceName,
        Status = (int)d.Status,
        d.RefreshTokenHash,
        d.RefreshTokenExpiresUtc,
        d.EnrolledUtc,
        d.LastSeenUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static OperativeDevice ToEntity(Row r) => new()
    {
        Id = r.Id,
        PersonId = r.PersonId,
        CompanyId = r.CompanyId,
        DeviceId = r.DeviceId,
        DeviceName = r.DeviceName,
        Status = (OperativeDeviceStatus)r.Status,
        RefreshTokenHash = r.RefreshTokenHash,
        RefreshTokenExpiresUtc = r.RefreshTokenExpiresUtc,
        EnrolledUtc = r.EnrolledUtc,
        LastSeenUtc = r.LastSeenUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid PersonId, Guid CompanyId, string DeviceId, string? DeviceName, int Status,
        string? RefreshTokenHash, DateTimeOffset? RefreshTokenExpiresUtc, DateTimeOffset EnrolledUtc, DateTimeOffset LastSeenUtc);
}
