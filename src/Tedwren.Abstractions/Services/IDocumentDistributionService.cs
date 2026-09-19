using Tedwren.Abstractions.Contracts.Documents;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Document distribution &amp; acknowledgement (PRD §8.2): send a document to a set of recipients in one action,
/// record each recipient's signed receipt, and show the completion matrix (who has / has not signed). Scoped to
/// the distributing company (R15); part of the paid HSE module.
/// </summary>
public interface IDocumentDistributionService
{
    /// <summary>Distributes a document to the request's recipients and returns it with its completion counts.</summary>
    Task<DocumentDistributionDto> CreateAsync(Guid companyId, string sentBy, CreateDistributionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's distributions, newest first, each with its signed/total counts.</summary>
    Task<IReadOnlyList<DocumentDistributionDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a distribution with its completion matrix, or null when missing/cross-tenant (R15).</summary>
    Task<DocumentDistributionDetailDto?> GetAsync(Guid companyId, Guid distributionId, CancellationToken cancellationToken = default);

    /// <summary>Records a recipient's acknowledgement (idempotent). Returns false when missing/cross-tenant (R15).</summary>
    Task<bool> AcknowledgeAsync(Guid companyId, Guid acknowledgementId, CancellationToken cancellationToken = default);
}
