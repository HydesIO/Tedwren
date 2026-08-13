using Tedwren.Abstractions.Contracts.Forms;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The Forms Library assignment service (PRD-Phase 2). Assigns forms to sites, operators, the organisation or an
/// induction (requirement 5), and manages the failed-check alert address. Tenant-scoped — a caller only ever
/// sees and manages its own company's assignments (R15).
/// </summary>
public interface IFormAssignmentService
{
    /// <summary>Lists the caller's form assignments, newest first.</summary>
    Task<IReadOnlyList<FormAssignmentDto>> GetAssignmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates an assignment and returns its id. Throws <see cref="System.ArgumentException"/> when the form/scope is invalid.</summary>
    Task<Guid> CreateAsync(CreateFormAssignmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes an assignment. Returns false when not found / not the caller's.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
