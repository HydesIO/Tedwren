using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Mobile;

/// <summary>
/// Records operatives' offline-captured field evidence (M5). Append-only (R4); idempotent on the device-generated
/// client id so a retried sync never duplicates (R4/R16); scoped to the operative's company + person taken from the
/// token (R15). Any photo is pre-uploaded via <c>/api/mobile/uploads</c> and referenced here (R9); the capture UTC
/// is preserved (R11).
/// </summary>
public sealed class MobileEvidenceService : IMobileEvidenceService
{
    private readonly IEvidenceItemRepository _items;

    /// <summary>Creates the service over the evidence repository.</summary>
    public MobileEvidenceService(IEvidenceItemRepository items) => _items = items;

    /// <summary>Records an evidence item, or returns the existing one when the client id was already synced.</summary>
    public async Task<EvidenceItemDto> ReportAsync(Guid companyId, Guid personId, MobileReportEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty || personId == Guid.Empty)
        {
            throw new ArgumentException("A company id and person id are required.");
        }

        // Idempotency (R4/R16): a retried sync of the same capture returns the existing item, never a duplicate.
        var existing = await _items.GetAsync(request.ClientId, cancellationToken);
        if (existing is not null)
        {
            if (existing.CompanyId != companyId)
            {
                throw new InvalidOperationException("This id already belongs to another company.");
            }

            return ToDto(existing);
        }

        var item = new EvidenceItem
        {
            Id = request.ClientId,
            CompanyId = companyId,
            PersonId = personId,
            Note = Clean(request.Note),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PhotoReference = Clean(request.PhotoReference),
            CapturedUtc = request.CapturedUtc,
        };
        await _items.AddAsync(item, cancellationToken);
        return ToDto(item);
    }

    /// <summary>Trims a value, mapping blank to null.</summary>
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Projects an evidence item to its DTO (photo exposed only as a flag, R9).</summary>
    private static EvidenceItemDto ToDto(EvidenceItem i) =>
        new(i.Id, i.Note, i.Latitude, i.Longitude, i.PhotoReference is not null, i.CapturedUtc, i.CreatedUtc);
}
