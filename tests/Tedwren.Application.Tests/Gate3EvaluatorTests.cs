using Tedwren.Application.Qualifications;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the pure <see cref="Gate3Evaluator"/> (Subcontractor Onboarding spec §2/§5, SF-11): only a legally-mandatory
/// accreditation that is missing or expired blocks the gate; advisory requirements are reported but never block; a
/// superseded card does not satisfy a requirement; and no requirements clears vacuously.
/// </summary>
public sealed class Gate3EvaluatorTests
{
    private static readonly Guid GasSafe = Guid.Parse("11111111-1111-4111-8111-000000000008");
    private static readonly Guid Cscs = Guid.Parse("11111111-1111-4111-8111-000000000001");
    private static readonly Guid Person = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 1);

    private static readonly IReadOnlyList<QualificationType> Types = new List<QualificationType>
    {
        new() { Id = GasSafe, Name = "Gas Safe" },
        new() { Id = Cscs, Name = "CSCS Card" },
    };

    // Gas Engineer: Gas Safe is legally mandatory (blocks Gate 3); CSCS is advisory.
    private static readonly IReadOnlyList<TradeQualificationRequirement> GasEngineerRequirements = new List<TradeQualificationRequirement>
    {
        new() { Trade = "Gas Engineer", QualificationTypeId = GasSafe, LegalMandatory = true },
        new() { Trade = "Gas Engineer", QualificationTypeId = Cscs, LegalMandatory = false },
    };

    private static QualificationCard Card(Guid typeId, DateOnly? expiresOn, bool superseded = false) => new()
    {
        PersonId = Person,
        QualificationTypeId = typeId,
        ExpiresOn = expiresOn,
        SupersededByCardId = superseded ? Guid.NewGuid() : null,
    };

    [Fact] // Every mandatory accreditation held on a current card → cleared.
    public void Cleared_WhenMandatoryHeldValid()
    {
        var cards = new[] { Card(GasSafe, Today.AddYears(1)), Card(Cscs, Today.AddYears(1)) };

        var result = Gate3Evaluator.Evaluate(GasEngineerRequirements, cards, Types, Today);

        Assert.True(result.Cleared);
        Assert.All(result.Requirements, r => Assert.True(r.Satisfied));
    }

    [Fact] // A missing mandatory accreditation blocks the gate and is reported "Not held".
    public void Blocked_WhenMandatoryMissing()
    {
        var cards = new[] { Card(Cscs, Today.AddYears(1)) };   // advisory only; Gas Safe absent

        var result = Gate3Evaluator.Evaluate(GasEngineerRequirements, cards, Types, Today);

        Assert.False(result.Cleared);
        var gas = result.Requirements.Single(r => r.Accreditation == "Gas Safe");
        Assert.False(gas.Satisfied);
        Assert.Equal("Not held", gas.Issue);
        Assert.True(result.Requirements.Single(r => r.Accreditation == "CSCS Card").Satisfied);
    }

    [Fact] // An expired mandatory accreditation blocks the gate and is reported "Expired".
    public void Blocked_WhenMandatoryExpired()
    {
        var cards = new[] { Card(GasSafe, Today.AddDays(-1)), Card(Cscs, Today.AddYears(1)) };

        var result = Gate3Evaluator.Evaluate(GasEngineerRequirements, cards, Types, Today);

        Assert.False(result.Cleared);
        var gas = result.Requirements.Single(r => r.Accreditation == "Gas Safe");
        Assert.False(gas.Satisfied);
        Assert.Equal("Expired", gas.Issue);
    }

    [Fact] // A missing advisory (non-mandatory) accreditation is reported but does not block.
    public void Advisory_DoesNotBlock()
    {
        var cards = new[] { Card(GasSafe, Today.AddYears(1)) };   // mandatory held; advisory CSCS absent

        var result = Gate3Evaluator.Evaluate(GasEngineerRequirements, cards, Types, Today);

        Assert.True(result.Cleared);
        Assert.False(result.Requirements.Single(r => r.Accreditation == "CSCS Card").Satisfied);
    }

    [Fact] // A superseded card does not satisfy a requirement (SF-10) — the operative must hold a current one.
    public void Superseded_CardIgnored()
    {
        var cards = new[] { Card(GasSafe, Today.AddYears(1), superseded: true) };

        var result = Gate3Evaluator.Evaluate(GasEngineerRequirements, cards, Types, Today);

        Assert.False(result.Cleared);
        Assert.Equal("Not held", result.Requirements.Single(r => r.Accreditation == "Gas Safe").Issue);
    }

    [Fact] // A trade with no mapped requirements clears vacuously.
    public void NoRequirements_ClearsVacuously()
    {
        var result = Gate3Evaluator.Evaluate(Array.Empty<TradeQualificationRequirement>(), Array.Empty<QualificationCard>(), Types, Today);

        Assert.True(result.Cleared);
        Assert.Empty(result.Requirements);
    }
}
