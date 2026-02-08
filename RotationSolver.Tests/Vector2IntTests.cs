using RotationSolver.Basic.Data;
using Xunit;

namespace RotationSolver.Tests;

public class Vector2IntTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange & Act
        var vector = new Vector2Int(10, 20);

        // Assert
        Assert.Equal(10, vector.X);
        Assert.Equal(20, vector.Y);
    }

    [Fact]
    public void WithX_ReturnsNewInstanceWithModifiedX()
    {
        // Arrange
        var vector = new Vector2Int(10, 20);

        // Act
        var newVector = vector.WithX(30);

        // Assert
        Assert.Equal(30, newVector.X);
        Assert.Equal(20, newVector.Y);
        Assert.Equal(10, vector.X); // Original unchanged
    }

    [Fact]
    public void WithY_ReturnsNewInstanceWithModifiedY()
    {
        // Arrange
        var vector = new Vector2Int(10, 20);

        // Act
        var newVector = vector.WithY(40);

        // Assert
        Assert.Equal(10, newVector.X);
        Assert.Equal(40, newVector.Y);
        Assert.Equal(20, vector.Y); // Original unchanged
    }
}
