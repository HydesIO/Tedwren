using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for operative device bindings (M2): one active device per operative (SF-1).</summary>
public interface IOperativeDeviceRepository
{
    /// <summary>Returns the device with this opaque install id, or null.</summary>
    Task<OperativeDevice?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the operative's currently-active device, or null when they have none.</summary>
    Task<OperativeDevice?> GetActiveByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new device binding.</summary>
    Task AddAsync(OperativeDevice device, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing device binding (status, refresh token, last-seen).</summary>
    Task UpdateAsync(OperativeDevice device, CancellationToken cancellationToken = default);
}
