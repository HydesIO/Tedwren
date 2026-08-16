using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for direct-debit payments. Scoped by company (R15).</summary>
public interface IPaymentRepository
{
    /// <summary>Returns every payment, most recent first.</summary>
    Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a payment by its local id, or null.</summary>
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's payments, most recent first.</summary>
    Task<IReadOnlyList<Payment>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the payment carrying the given GoCardless payment id, or null.</summary>
    Task<Payment?> GetByGoCardlessPaymentIdAsync(string goCardlessPaymentId, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new payment.</summary>
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing payment.</summary>
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes a payment by id (used only by the demo-data teardown).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
