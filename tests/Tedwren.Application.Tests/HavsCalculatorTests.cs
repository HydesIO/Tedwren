using Tedwren.Abstractions.Contracts.Havs;
using Tedwren.Application.Havs;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for the HAVs exposure calculator (PRD §8.2) against the HSE methodology: the Exposure Action Value
/// (2.5 m/s² A(8) = 100 points) and Exposure Limit Value (5.0 m/s² A(8) = 400 points), and the root-sum-of-squares
/// combination of multiple tools.
/// </summary>
public sealed class HavsCalculatorTests
{
    private static HavsToolUsageDto Tool(double magnitude, int minutes) => new("Tool", magnitude, minutes);

    [Fact]
    public void NoUsages_IsZero_BelowActionValue()
    {
        var result = HavsCalculator.Compute(Array.Empty<HavsToolUsageDto>());

        Assert.Equal(0, result.DailyExposureA8);
        Assert.Equal(0, result.ExposurePoints);
        Assert.Equal(HavsExposureBand.BelowActionValue, result.Band);
    }

    [Fact] // 2.5 m/s² for a full 8h day is exactly the EAV: A(8) = 2.5, 100 points
    public void AtActionValue_100Points_AboveActionValue()
    {
        var result = HavsCalculator.Compute(new[] { Tool(2.5, 480) });

        Assert.Equal(2.5, result.DailyExposureA8);
        Assert.Equal(100, result.ExposurePoints);
        Assert.Equal(HavsExposureBand.AboveActionValue, result.Band);
    }

    [Fact] // 5.0 m/s² for a full 8h day is exactly the ELV: A(8) = 5.0, 400 points
    public void AtLimitValue_400Points_AboveLimitValue()
    {
        var result = HavsCalculator.Compute(new[] { Tool(5.0, 480) });

        Assert.Equal(5.0, result.DailyExposureA8);
        Assert.Equal(400, result.ExposurePoints);
        Assert.Equal(HavsExposureBand.AboveLimitValue, result.Band);
    }

    [Fact] // 2.0 m/s² for 8h is below the EAV: A(8) = 2.0, 64 points
    public void BelowActionValue_64Points()
    {
        var result = HavsCalculator.Compute(new[] { Tool(2.0, 480) });

        Assert.Equal(2.0, result.DailyExposureA8);
        Assert.Equal(64, result.ExposurePoints);
        Assert.Equal(HavsExposureBand.BelowActionValue, result.Band);
    }

    [Fact] // 5.0 m/s² for only 2h: A(8) = 5·√(2/8) = 2.5 → back at the EAV
    public void ShorterTriggerTime_ReducesExposure()
    {
        var result = HavsCalculator.Compute(new[] { Tool(5.0, 120) });

        Assert.Equal(2.5, result.DailyExposureA8);
        Assert.Equal(100, result.ExposurePoints);
    }

    [Fact] // Two tools each at the EAV combine by root-sum-of-squares: points add (100 + 100 = 200)
    public void MultipleTools_RootSumOfSquares_PointsAdd()
    {
        var result = HavsCalculator.Compute(new[] { Tool(2.5, 480), Tool(2.5, 480) });

        Assert.Equal(200, result.ExposurePoints);
        Assert.Equal(Math.Round(Math.Sqrt(12.5), 2), result.DailyExposureA8);   // ≈ 3.54 m/s²
        Assert.Equal(HavsExposureBand.AboveActionValue, result.Band);
    }
}
