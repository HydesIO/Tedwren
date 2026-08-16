using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IPaymentRepository"/> (test-only double), singleton so payments persist per host.</summary>
public sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    /// <summary>Returns every payment, most recent first.</summary>
    public Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Payment> rows = _payments.Values.OrderByDescending(p => p.CreatedUtc).ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a payment by local id, or null.</summary>
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_payments.GetValueOrDefault(id));

    /// <summary>Returns a company's payments, most recent first.</summary>
    public Task<IReadOnlyList<Payment>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Payment> rows = _payments.Values
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns the payment carrying the given GoCardless payment id, or null.</summary>
    public Task<Payment?> GetByGoCardlessPaymentIdAsync(string goCardlessPaymentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_payments.Values
            .FirstOrDefault(p => p.GoCardlessPaymentId == goCardlessPaymentId));

    /// <summary>Adds a payment.</summary>
    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _payments[payment.Id] = payment;
        return Task.CompletedTask;
    }

    /// <summary>Removes a payment from the store (demo-data teardown).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _payments.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>Updates a payment.</summary>
    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _payments[payment.Id] = payment;
        return Task.CompletedTask;
    }
}
