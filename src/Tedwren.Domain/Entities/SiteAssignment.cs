namespace Tedwren.Domain.Entities;

/// <summary>
/// Assigns a console user to a site they are responsible for (MC-21). A site manager sees only the sites they
/// are assigned to; administrators, compliance managers and auditors see every site in the company. Without
/// this link a site manager would see the whole company's sites, which does not scale as the tester noted (UAT-011).
/// </summary>
public sealed class SiteAssignment
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The console user the site is assigned to.</summary>
    public required Guid UserId { get; init; }

    /// <summary>The site the user is responsible for.</summary>
    public required Guid SiteId { get; init; }

    /// <summary>When the assignment was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
