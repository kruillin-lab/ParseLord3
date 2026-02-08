using System.Numerics;
using RotationSolver.Basic.Data;
using Xunit;

namespace RotationSolver.Tests;

public class RandomDelayTests
{
    [Fact]
    public void Delay_ReturnsFalse_WhenOriginDataIsFalse()
    {
        // Arrange
        var delay = new RandomDelay(() => (1f, 2f));

        // Act
        var result = delay.Delay(false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Delay_ReturnsFalse_ImmediatelyWhenOriginDataTurnsTrue()
    {
        // Arrange
        var delay = new RandomDelay(() => (0.5f, 1f));

        // Act
        var result = delay.Delay(true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Delay_ReturnsFalse_IfMinOrMaxIsZeroOrLess()
    {
        // Arrange
        var delay = new RandomDelay(() => (0f, 1f));

        // Act
        var result = delay.Delay(true);

        // Assert
        Assert.True(result); // Logic says: if min <= 0 || max <= 0 { return originData; }
    }

    [Fact]
    public async Task Delay_EventuallyReturnsTrue_AfterDelay()
    {
        // Arrange
        var delay = new RandomDelay(() => (0.1f, 0.2f));

        // Act
        var initialResult = delay.Delay(true);
        await Task.Delay(300); // Wait longer than max delay
        var finalResult = delay.Delay(true);

        // Assert
        Assert.False(initialResult);
        Assert.True(finalResult);
    }

    [Fact]
    public void Delay_Resets_WhenOriginDataTurnsFalse()
    {
        // Arrange
        var delay = new RandomDelay(() => (0.1f, 0.2f));

        // Start delay
        delay.Delay(true);

        // Reset
        delay.Delay(false);

        // Start again
        var result = delay.Delay(true);

        // Assert
        Assert.False(result); // Should be false again as it just restarted
    }

    [Fact]
    public void DelayGeneric_ReturnsNull_Initially()
    {
        // Arrange
        var delay = new RandomDelay(() => (0.5f, 1f));
        var data = new object();

        // Act
        var result = delay.Delay(data);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DelayGeneric_ReturnsObject_AfterDelay()
    {
        // Arrange
        var delay = new RandomDelay(() => (0.1f, 0.2f));
        var data = new object();

        // Act
        delay.Delay(data);
        await Task.Delay(300);
        var result = delay.Delay(data);

        // Assert
        Assert.Same(data, result);
    }
}
