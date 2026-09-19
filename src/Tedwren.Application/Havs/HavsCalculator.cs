using Tedwren.Abstractions.Contracts.Havs;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Havs;

/// <summary>The derived result of a day's hand-arm vibration exposure calculation (PRD §8.2).</summary>
public readonly record struct HavsExposureResult(double DailyExposureA8, int ExposurePoints, HavsExposureBand Band);

/// <summary>
/// Computes daily hand-arm vibration exposure using the HSE methodology (Control of Vibration at Work Regulations
/// 2005). Each tool's partial exposure is A(8)ₚ = a·√(t/8) for magnitude <c>a</c> (m/s²) and trigger time <c>t</c>
/// (hours); the day's exposure is the root-sum-of-squares of the partials. Points are (A(8)/EAV)²·100 so that the
/// Exposure Action Value (2.5 m/s²) is 100 points and the Exposure Limit Value (5.0 m/s²) is 400 points.
/// </summary>
public static class HavsCalculator
{
    /// <summary>The HSE Exposure Action Value — daily A(8) in m/s² above which action is required.</summary>
    public const double ExposureActionValue = 2.5;

    /// <summary>The HSE Exposure Limit Value — daily A(8) in m/s² that must not be exceeded.</summary>
    public const double ExposureLimitValue = 5.0;

    private const double ReferenceHours = 8.0;

    /// <summary>Computes the day's A(8), exposure points and band from its tool usages.</summary>
    public static HavsExposureResult Compute(IEnumerable<HavsToolUsageDto> usages)
    {
        var sumOfSquares = 0.0;
        foreach (var usage in usages ?? Array.Empty<HavsToolUsageDto>())
        {
            var magnitude = Math.Max(0, usage.MagnitudeMs2);
            var triggerHours = Math.Max(0, usage.TriggerMinutes) / 60.0;
            // Partial A(8) squared = a² · (t / 8); summing the squares gives the day's A(8)² (root-sum-of-squares).
            sumOfSquares += magnitude * magnitude * (triggerHours / ReferenceHours);
        }

        var a8 = Math.Sqrt(sumOfSquares);
        var points = (int)Math.Round(a8 * a8 / (ExposureActionValue * ExposureActionValue) * 100, MidpointRounding.AwayFromZero);
        return new HavsExposureResult(Math.Round(a8, 2), points, BandFor(a8));
    }

    /// <summary>Maps a daily A(8) to its exposure band against the EAV/ELV thresholds.</summary>
    private static HavsExposureBand BandFor(double a8) => a8 switch
    {
        >= ExposureLimitValue => HavsExposureBand.AboveLimitValue,
        >= ExposureActionValue => HavsExposureBand.AboveActionValue,
        _ => HavsExposureBand.BelowActionValue,
    };
}
