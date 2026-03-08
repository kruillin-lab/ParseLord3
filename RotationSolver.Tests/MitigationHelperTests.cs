using RotationSolver.Basic.Helpers;
using Xunit;
using static RotationSolver.Basic.Helpers.MitigationHelper;

namespace RotationSolver.Tests;

public class MitigationHelperTests
{
    #region ShouldUseBigCooldown Tests

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

    #endregion

    #region CanUseMitigationByRecast Tests

    [Theory]
    [InlineData(25f, true)]   // Short cooldown (e.g., Heart of Stone)
    [InlineData(60f, true)]   // Medium cooldown
    [InlineData(89f, true)]   // Just under threshold
    [InlineData(90f, false)]  // At threshold - requires no long cooldown active check
    [InlineData(120f, false)] // Long cooldown (e.g., Sentinel)
    [InlineData(240f, false)] // Very long cooldown (e.g., invulns)
    public void CanUseMitigationByRecast_ShortCooldowns_AlwaysTrue(float recastTime, bool expectedIfNoActiveMit)
    {
        // Note: This test assumes no long cooldown mitigation is currently active
        // The actual result depends on HasLongCooldownMitigationActive() which requires game state
        // For recast < 90, it should always return true
        if (recastTime < 90f)
        {
            var result = MitigationHelper.CanUseMitigationByRecast(recastTime);
            Assert.True(result);
        }
        // For recast >= 90, it depends on game state - can't test without mocking
    }

    #endregion

    #region CanUseLongCooldownMitigation Tests

    [Fact]
    public void CanUseLongCooldownMitigation_WhenNotLongCooldown_ReturnsTrue()
    {
        // When isLongCooldown is false, should always return true
        var result = MitigationHelper.CanUseLongCooldownMitigation(false);
        Assert.True(result);
    }

    #endregion

    #region DamageLevel Enum Tests

    [Fact]
    public void DamageLevel_HasExpectedValues()
    {
        // Verify the enum has the expected progression
        Assert.True((int)DamageLevel.None < (int)DamageLevel.Light);
        Assert.True((int)DamageLevel.Light < (int)DamageLevel.Moderate);
        Assert.True((int)DamageLevel.Moderate < (int)DamageLevel.Heavy);
        Assert.True((int)DamageLevel.Heavy < (int)DamageLevel.Emergency);
    }

    [Fact]
    public void DamageLevel_EnumContainsAllExpectedValues()
    {
        var values = Enum.GetValues<DamageLevel>();
        Assert.Equal(5, values.Length);
        Assert.Contains(DamageLevel.None, values);
        Assert.Contains(DamageLevel.Light, values);
        Assert.Contains(DamageLevel.Moderate, values);
        Assert.Contains(DamageLevel.Heavy, values);
        Assert.Contains(DamageLevel.Emergency, values);
    }

    #endregion

    #region DamageType Enum Tests

    [Fact]
    public void DamageType_EnumContainsAllExpectedValues()
    {
        var values = Enum.GetValues<DamageType>();
        Assert.Equal(4, values.Length);
        Assert.Contains(DamageType.Unknown, values);
        Assert.Contains(DamageType.Physical, values);
        Assert.Contains(DamageType.Magical, values);
        Assert.Contains(DamageType.Mixed, values);
    }

    #endregion
}
