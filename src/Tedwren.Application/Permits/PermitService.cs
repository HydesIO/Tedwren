using Tedwren.Abstractions.Contracts.Permits;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Permits;

/// <summary>Store-agnostic permits-to-work service: raises permits and lists a company's permits.</summary>
public sealed class PermitService : IPermitService
{
    private readonly IPermitRepository _permits;

    /// <summary>Creates the service over the permit repository.</summary>
    public PermitService(IPermitRepository permits) => _permits = permits;

    /// <summary>Raises a permit (draft or issued) and returns its new identifier.</summary>
    public async Task<Guid> CreateAsync(CreatePermitRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CompanyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PermitType))
        {
            throw new ArgumentException("A permit type is required.", nameof(request));
        }

        var permit = new Permit
        {
            CompanyId = request.CompanyId,
            PermitType = request.PermitType.Trim(),
            SiteName = string.IsNullOrWhiteSpace(request.SiteName) ? null : request.SiteName.Trim(),
            ResponsiblePerson = string.IsNullOrWhiteSpace(request.ResponsiblePerson) ? null : request.ResponsiblePerson.Trim(),
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            HighRisk = request.HighRisk,
            RamsAttached = request.RamsAttached,
            Status = request.Issue ? PermitStatus.Issued : PermitStatus.Draft,
        };

        await _permits.AddAsync(permit, cancellationToken);
        return permit.Id;
    }

    /// <summary>Returns a company's permits, newest first.</summary>
    public async Task<IReadOnlyList<PermitDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var permits = await _permits.GetByCompanyAsync(companyId, cancellationToken);
        return permits.Select(ToDto).ToList();
    }

    /// <summary>Maps a permit entity to its DTO.</summary>
    private static PermitDto ToDto(Permit p) => new(
        p.Id, p.PermitType, p.SiteName, p.ResponsiblePerson, p.ValidFrom, p.ValidTo,
        p.Description, p.HighRisk, p.RamsAttached, p.Status.ToString(), p.CreatedUtc);
}
