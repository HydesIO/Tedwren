using System.Collections.Concurrent;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IOperativeDeviceRepository"/> (M2).</summary>
public sealed class InMemoryOperativeDeviceRepository : IOperativeDeviceRepository
{
    private readonly ConcurrentDictionary<Guid, OperativeDevice> _devices = new();

    public Task<OperativeDevice?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_devices.Values.FirstOrDefault(d => d.DeviceId == deviceId));

    public Task<OperativeDevice?> GetActiveByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_devices.Values.FirstOrDefault(d => d.PersonId == personId && d.Status == OperativeDeviceStatus.Active));

    public Task AddAsync(OperativeDevice device, CancellationToken cancellationToken = default)
    {
        _devices[device.Id] = device;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OperativeDevice device, CancellationToken cancellationToken = default)
    {
        _devices[device.Id] = device;
        return Task.CompletedTask;
    }
}
