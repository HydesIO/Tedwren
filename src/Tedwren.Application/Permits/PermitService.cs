using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Contracts.Permits;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Permits;

/// <summary>
/// Store-agnostic permits-to-work service: raises permits, lists a company's permits, and drives the permit
/// lifecycle — approve and close — scoped to the caller's company (R15) with each transition recorded to the
/// audit trail (PRD §8.2). The audit/current-user dependencies are optional so direct-construction unit tests are
/// unaffected; the composition root supplies them in the running app.
/// </summary>
public sealed class PermitService : IPermitService
{
    private readonly IPermitRepository _permits;
    private readonly IAuditService? _audit;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>Creates the service over the permit repository, with optional audit + current-user support.</summary>
    public PermitService(
        IPermitRepository permits,
        IAuditService? audit = null,
        ICurrentUserService? currentUser = null)
    {
        _permits = permits;
        _audit = audit;
        _currentUser = currentUser;
    }

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

    /// <summary>Approves a draft/issued permit, scoped to the caller's company (R15), and records it (PRD §8.2).</summary>
    public async Task<bool> ApproveAsync(Guid permitId, CancellationToken cancellationToken = default)
    {
        var (permit, companyId) = await ResolveAsync(permitId, cancellationToken);
        if (permit is null)
        {
            return false;
        }

        if (permit.Status is not (PermitStatus.Draft or PermitStatus.Issued))
        {
            throw new InvalidOperationException($"A permit that is {permit.Status} cannot be approved.");
        }

        await _permits.UpdateStatusAsync(permit.Id, PermitStatus.Approved, cancellationToken);
        await AuditAsync(companyId, "Permit approved", permit.PermitType, permit.SiteName, cancellationToken);
        return true;
    }

    /// <summary>Closes an issued/approved permit with an optional reason, scoped to the caller's company (R15).</summary>
    public async Task<bool> CloseAsync(Guid permitId, string? reason, CancellationToken cancellationToken = default)
    {
        var (permit, companyId) = await ResolveAsync(permitId, cancellationToken);
        if (permit is null)
        {
            return false;
        }

        if (permit.Status is not (PermitStatus.Issued or PermitStatus.Approved))
        {
            throw new InvalidOperationException($"A permit that is {permit.Status} cannot be closed.");
        }

        await _permits.UpdateStatusAsync(permit.Id, PermitStatus.Closed, cancellationToken);
        var note = string.IsNullOrWhiteSpace(reason) ? permit.SiteName : $"{permit.SiteName} — {reason.Trim()}";
        await AuditAsync(companyId, "Permit closed", permit.PermitType, note, cancellationToken);
        return true;
    }

    /// <summary>
    /// Loads a permit together with the caller's company, returning a null permit when it is missing or belongs to
    /// another company — so a caller can never act on a permit outside their own tenant (R15).
    /// </summary>
    private async Task<(Permit? Permit, Guid CompanyId)> ResolveAsync(Guid permitId, CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            throw new InvalidOperationException("A current user is required to change a permit's state.");
        }

        var companyId = (await _currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
        var permit = await _permits.GetAsync(permitId, cancellationToken);
        return permit is not null && permit.CompanyId == companyId ? (permit, companyId) : (null, companyId);
    }

    /// <summary>Records a permit lifecycle transition to the audit trail (best-effort; a no-op without an audit sink).</summary>
    private async Task AuditAsync(Guid companyId, string action, string entity, string? reference, CancellationToken cancellationToken)
    {
        if (_audit is null)
        {
            return;
        }

        try
        {
            var actor = _currentUser is not null ? (await _currentUser.GetCurrentAsync(cancellationToken)).Name : "System";
            await _audit.RecordAsync(new RecordAuditRequest(companyId, actor, action, entity, reference, "Permits"), cancellationToken);
        }
        catch
        {
            // Audit is best-effort; never fail the operation because the audit write failed.
        }
    }

    /// <summary>Maps a permit entity to its DTO, deriving the Expired display state from the valid-to date (PRD §8.2).</summary>
    private static PermitDto ToDto(Permit p)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var inForce = p.Status is PermitStatus.Issued or PermitStatus.Approved;
        var effective = inForce && p.ValidTo is { } validTo && validTo < today
            ? PermitStatus.Expired
            : p.Status;
        return new PermitDto(
            p.Id, p.PermitType, p.SiteName, p.ResponsiblePerson, p.ValidFrom, p.ValidTo,
            p.Description, p.HighRisk, p.RamsAttached, effective.ToString(), p.CreatedUtc);
    }
}
