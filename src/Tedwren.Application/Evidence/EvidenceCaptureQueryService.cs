using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Evidence;

/// <summary>
/// Serves operatives' field evidence captures (M5) to managers for review (M7). Company-scoped (R15): a manager
/// only ever sees their own company's captures, and a capture id belonging to another company reads as not found.
/// The capturer's display name is the company's own recorded name for the person (from the engagement — names are
/// never reconciled across companies, PRD §5.1). The photo stays behind the authorised image route (R9).
/// </summary>
public sealed class EvidenceCaptureQueryService : IEvidenceCaptureQueryService
{
    private readonly IEvidenceItemRepository _items;
    private readonly IEngagementRepository _engagements;

    /// <summary>Creates the service over the evidence and engagement repositories.</summary>
    public EvidenceCaptureQueryService(IEvidenceItemRepository items, IEngagementRepository engagements)
    {
        _items = items;
        _engagements = engagements;
    }

    /// <summary>Returns a company's captures (newest first), optionally for a single operative.</summary>
    public async Task<IReadOnlyList<EvidenceCaptureDto>> GetForCompanyAsync(Guid companyId, Guid? personId = null, CancellationToken cancellationToken = default)
    {
        var items = await _items.GetByCompanyAsync(companyId, cancellationToken);
        var filtered = personId is { } pid ? items.Where(i => i.PersonId == pid) : items;

        // Resolve each capturer's company-recorded name once per person (R15 / PRD §5.1).
        var names = new Dictionary<Guid, string>();
        var result = new List<EvidenceCaptureDto>();
        foreach (var item in filtered)
        {
            result.Add(await ToDtoAsync(item, companyId, names, cancellationToken));
        }

        return result;
    }

    /// <summary>Returns one capture the company owns, or null when it is missing or belongs to another company (R15).</summary>
    public async Task<EvidenceCaptureDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _items.GetAsync(id, cancellationToken);
        if (item is null || item.CompanyId != companyId)
        {
            return null;
        }

        return await ToDtoAsync(item, companyId, new Dictionary<Guid, string>(), cancellationToken);
    }

    /// <summary>Projects a capture to its review DTO, resolving (and memoising) the capturer's engagement name.</summary>
    private async Task<EvidenceCaptureDto> ToDtoAsync(EvidenceItem item, Guid companyId, IDictionary<Guid, string> names, CancellationToken cancellationToken)
    {
        if (!names.TryGetValue(item.PersonId, out var name))
        {
            var engagement = await _engagements.GetByCompanyAndPersonAsync(companyId, item.PersonId, cancellationToken);
            name = string.IsNullOrWhiteSpace(engagement?.Name) ? "Unknown operative" : engagement!.Name;
            names[item.PersonId] = name;
        }

        return new EvidenceCaptureDto(
            item.Id, item.PersonId, name, item.Note, item.Latitude, item.Longitude, item.PhotoReference, item.CapturedUtc, item.CreatedUtc);
    }
}
