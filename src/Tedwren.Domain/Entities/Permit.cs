using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A permit to work raised for a company: the permitted activity, where and who is responsible, its valid
/// period and the controls in place. Scoped to the issuing company (R15).
/// </summary>
public sealed class Permit
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that raised the permit.</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The permit type (e.g. Hot Works) — from the reference list.</summary>
    public required string PermitType { get; set; }

    /// <summary>The site the permit applies to.</summary>
    public string? SiteName { get; set; }

    /// <summary>The person responsible for the permitted work.</summary>
    public string? ResponsiblePerson { get; set; }

    /// <summary>First day the permit is valid.</summary>
    public DateOnly? ValidFrom { get; set; }

    /// <summary>Last day the permit is valid.</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>Free-text description of the works.</summary>
    public string? Description { get; set; }

    /// <summary>Whether the permit covers a high-risk activity requiring extra sign-off.</summary>
    public bool HighRisk { get; set; }

    /// <summary>Whether a risk assessment / method statement is attached.</summary>
    public bool RamsAttached { get; set; }

    /// <summary>The permit's lifecycle state.</summary>
    public PermitStatus Status { get; set; } = PermitStatus.Draft;

    /// <summary>When the permit was created (UTC; displayed in UK local time, R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
