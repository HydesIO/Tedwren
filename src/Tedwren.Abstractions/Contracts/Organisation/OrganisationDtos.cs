using Tedwren.Abstractions.Common;

namespace Tedwren.Abstractions.Contracts.Organisation;

/// <summary>A company row for the Organisation list. Field names mirror the existing UI record.</summary>
public sealed record CompanySummary(
    Guid Id,
    string Slug,
    string Name,
    string? Type,
    string? Trade,
    int Operatives,
    double? CompliancePercent,
    ComplianceState State,
    string StatusLabel,
    OrgType? OrgType = null);

/// <summary>Full company record for the detail page.</summary>
public sealed record CompanyDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Type,
    string? Trade,
    ComplianceState State,
    string StatusLabel,
    double? CompliancePercent,
    string? RegistrationNumber,
    string? Address,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    IReadOnlyList<CompanyDocumentDto> Documents,
    IReadOnlyList<CompanyOperativeDto> Operatives,
    OrgType? OrgType = null);

/// <summary>A company-held document (insurance, accreditation, policy) shown on the detail page.</summary>
public sealed record CompanyDocumentDto(
    string Name,
    string Type,
    ComplianceState State,
    string StatusLabel,
    DateOnly? ExpiresOn);

/// <summary>Request to add a company-held document — insurance, accreditation or policy (SUB-4).</summary>
public sealed record CreateCompanyDocumentRequest(
    Guid CompanyId,
    string Name,
    string Type,
    DateOnly? ExpiresOn,
    string? Reference);

/// <summary>An operative engaged by a company, shown on the company detail page.</summary>
public sealed record CompanyOperativeDto(
    Guid EngagementId,
    Guid PersonId,
    string Slug,
    string Name,
    string? Trade,
    ComplianceState State,
    string StatusLabel);

/// <summary>Request to create a company (Organisation → Add company). <paramref name="OrgType"/> selects the
/// product and its default module bundle (SF-22); null leaves the company product-less until edited.</summary>
public sealed record CreateCompanyRequest(
    string Name,
    string? Type,
    string? Trade,
    string? RegistrationNumber,
    string? Address,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    OrgType? OrgType = null);

/// <summary>Request to update an existing company's editable fields.</summary>
public sealed record UpdateCompanyRequest(
    string Name,
    string? Type,
    string? Trade,
    string? RegistrationNumber,
    string? Address,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone);

/// <summary>
/// Request to add an operative to a company. The person is identified by mobile number (SF-1); the
/// name is recorded per company (SF-2).
/// </summary>
public sealed record AddOperativeRequest(
    Guid CompanyId,
    string Name,
    string MobileNumber,
    string? Trade,
    string? InternalReference);

/// <summary>Request to update an operative's per-company engagement details (name + trade, SF-2).</summary>
public sealed record UpdateEngagementRequest(string Name, string? Trade);

/// <summary>
/// Outcome of <c>AddOperativeAsync</c>. On refusal (SF-2: already engaged in this company)
/// <see cref="Succeeded"/> is false and <see cref="Error"/> names the existing record.
/// </summary>
public sealed record AddOperativeResult(
    bool Succeeded,
    Guid? EngagementId,
    Guid? PersonId,
    string? Error);
