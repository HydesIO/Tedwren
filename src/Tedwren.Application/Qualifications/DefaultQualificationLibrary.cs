using Tedwren.Domain.Entities;

namespace Tedwren.Application.Qualifications;

/// <summary>
/// The canonical default qualification-type library and default trade requirements that ship with the
/// product (SF-12, SF-11), so a new customer is productive quickly. Single source of truth used by both
/// the in-memory store (mock mode) and the database seeder (database mode). Type ids are fixed so cards
/// and requirements reference stable identifiers across engines and restarts. The set is illustrative
/// and the customer's to adjust (PRD Q21); it is not invented compliance rules.
/// </summary>
public static class DefaultQualificationLibrary
{
    // Fixed identifiers for the default types.
    private static readonly Guid CscsCard = new("11111111-1111-4111-8111-000000000001");
    private static readonly Guid Sssts = new("11111111-1111-4111-8111-000000000002");
    private static readonly Guid Smsts = new("11111111-1111-4111-8111-000000000003");
    private static readonly Guid FirstAid = new("11111111-1111-4111-8111-000000000004");
    private static readonly Guid Asbestos = new("11111111-1111-4111-8111-000000000005");
    private static readonly Guid WorkingAtHeight = new("11111111-1111-4111-8111-000000000006");
    private static readonly Guid ManualHandling = new("11111111-1111-4111-8111-000000000007");

    /// <summary>The default qualification types (SF-12). Mirrors the sample library so mock/db agree.</summary>
    public static IReadOnlyList<QualificationType> Types { get; } = new List<QualificationType>
    {
        new() { Id = CscsCard, Name = "CSCS Card", Category = "Card", Issuer = "CITB", DefaultValidityMonths = 60, IsCscsVerifiable = true },
        new() { Id = Sssts, Name = "SSSTS", Category = "Supervision", Issuer = "CITB", DefaultValidityMonths = 60 },
        new() { Id = Smsts, Name = "SMSTS", Category = "Management", Issuer = "CITB", DefaultValidityMonths = 60 },
        new() { Id = FirstAid, Name = "First Aid at Work", Category = "Health & Safety", Issuer = "HSE", DefaultValidityMonths = 36 },
        new() { Id = Asbestos, Name = "Asbestos Awareness", Category = "Health & Safety", Issuer = "UKATA", DefaultValidityMonths = 12 },
        new() { Id = WorkingAtHeight, Name = "Working at Height", Category = "Health & Safety", Issuer = "IPAF", DefaultValidityMonths = 60 },
        new() { Id = ManualHandling, Name = "Manual Handling", Category = "Health & Safety", Issuer = "RoSPA", DefaultValidityMonths = 36 },
    };

    /// <summary>
    /// Illustrative default trade requirements (SF-11): a CSCS card underpins every listed trade, with a
    /// couple of trade-specific additions. The customer adjusts these (Q21); they are a starting point.
    /// </summary>
    public static IReadOnlyList<TradeQualificationRequirement> TradeRequirements { get; } = new List<TradeQualificationRequirement>
    {
        new() { Trade = "Groundworks", QualificationTypeId = CscsCard },
        new() { Trade = "Groundworks", QualificationTypeId = ManualHandling },
        new() { Trade = "Bricklayer", QualificationTypeId = CscsCard },
        new() { Trade = "Electrician", QualificationTypeId = CscsCard },
        new() { Trade = "Scaffolder", QualificationTypeId = CscsCard },
        new() { Trade = "Scaffolder", QualificationTypeId = WorkingAtHeight },
        new() { Trade = "Site Supervisor", QualificationTypeId = CscsCard },
        new() { Trade = "Site Supervisor", QualificationTypeId = Sssts },
        new() { Trade = "Labourer", QualificationTypeId = CscsCard },
    };
}
