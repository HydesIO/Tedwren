using Tedwren.Application.Auth;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Inductions;

/// <summary>
/// The canonical default induction template that ships with the product (MC-3), so a new customer can run an
/// induction immediately. Single source of truth used by both the in-memory store (mock mode) and the database
/// seeder (database mode). Ids are fixed so sessions reference stable identifiers across engines and restarts.
/// The quiz answers live here (server-side) and are never sent to the device (R5); the set is illustrative and
/// the customer's to adjust (MC-3). Induction is a main-contractor feature (§6.1, SUB-11), so the demo template
/// belongs to the main-contractor demo tenant (Meridian).
/// </summary>
public static class DefaultInductionTemplate
{
    /// <summary>The demo tenant the default template belongs to — the main contractor Meridian (MC-3).</summary>
    public static readonly Guid DemoCompanyId = AdminUserSeeder.SeedCompanyId;

    /// <summary>The fixed default template id.</summary>
    public static readonly Guid TemplateId = Guid.Parse("66666666-6666-4666-8666-000000000010");

    /// <summary>The default induction template (MC-3).</summary>
    public static InductionTemplate Template { get; } = new()
    {
        Id = TemplateId,
        CompanyId = DemoCompanyId,
        Name = "General Site Induction",
        ValidityDays = 365,
        PassMark = 3,
        Steps = new List<InductionStep>
        {
            new("identity", InductionStepKind.Identity, "Confirm your identity", Required: true),
            new("emergency", InductionStepKind.EmergencyContact, "Add an emergency contact", Required: true),
            new("declaration", InductionStepKind.Declaration, "Read and accept the site rules", Required: true),
            new("video", InductionStepKind.Media, "Watch the safety induction video", Required: true),
        },
        Questions = new List<InductionQuizQuestion>
        {
            new("q1", "What must you wear on site at all times?",
                new[] { "Hi-vis, hard hat and boots", "Only a hard hat", "Nothing special" }, CorrectOptionIndex: 0),
            new("q2", "What do you do if you hear the evacuation alarm?",
                new[] { "Finish your task first", "Go to the muster point", "Hide on site" }, CorrectOptionIndex: 1),
            new("q3", "Who do you report a near-miss to?",
                new[] { "No one", "A friend", "Your site supervisor" }, CorrectOptionIndex: 2),
        },
    };
}
