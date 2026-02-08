using System;
using Dalamud.Game.ClientState.Keys;
using RotationSolver.Basic.Helpers;
using Xunit;

namespace RotationSolver.Tests;

public class ConfigurationHelperTests
{
    [Theory]
    [InlineData(ConsoleModifiers.Alt, VirtualKey.MENU)]
    [InlineData(ConsoleModifiers.Shift, VirtualKey.SHIFT)]
    [InlineData(ConsoleModifiers.Control, VirtualKey.CONTROL)]
    public void ToVirtual_ReturnsCorrectVirtualKey(ConsoleModifiers modifier, VirtualKey expected)
    {
        // Act
        var result = modifier.ToVirtual();

        // Assert
        Assert.Equal(expected, result);
    }
}
