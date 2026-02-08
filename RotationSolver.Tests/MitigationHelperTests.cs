using RotationSolver.Basic.Helpers;
using Xunit;
using static RotationSolver.Basic.Helpers.MitigationHelper;

namespace RotationSolver.Tests;

public class MitigationHelperTests
{
    [Theory]
    [InlineData(DamageLevel.Emergency, true)]
    [InlineData(DamageLevel.Heavy, true)]
    [InlineData(DamageLevel.Moderate, false)]
    [InlineData(DamageLevel.Light, false)]
    [InlineData(DamageLevel.None, false)]
    public void ShouldUseBigCooldown_ReturnsExpectedValue(DamageLevel level, bool expected)
    {
        // Act
        var result = MitigationHelper.ShouldUseBigCooldown(level);

        // Assert
        Assert.Equal(expected, result);
    }
}
