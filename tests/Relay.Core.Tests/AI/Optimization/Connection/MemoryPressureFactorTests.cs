using Relay.Core.AI.Optimization.Connection;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;

public class MemoryPressureFactorTests
{
    [Fact]
    public void Constructor_Should_Initialize_Default_Values()
    {
        // Act
        var factor = new MemoryPressureFactor();

        // Assert
        Assert.Equal(string.Empty, factor.Name);
        Assert.Equal(0.0, factor.Value);
        Assert.Equal(0.0, factor.Weight);
    }

    [Fact]
    public void Properties_Should_Be_Settable()
    {
        // Arrange
        var factor = new MemoryPressureFactor();

        // Act
        factor.Name = "TestFactor";
        factor.Value = 0.75;
        factor.Weight = 0.5;

        // Assert
        Assert.Equal("TestFactor", factor.Name);
        Assert.Equal(0.75, factor.Value);
        Assert.Equal(0.5, factor.Weight);
    }

    [Fact]
    public void Should_Support_Negative_Values()
    {
        // Arrange
        var factor = new MemoryPressureFactor
        {
            Name = "NegativeFactor",
            Value = -0.2,
            Weight = 0.1
        };

        // Assert
        Assert.Equal("NegativeFactor", factor.Name);
        Assert.Equal(-0.2, factor.Value);
        Assert.Equal(0.1, factor.Weight);
    }

    [Fact]
    public void Should_Support_High_Values()
    {
        // Arrange
        var factor = new MemoryPressureFactor
        {
            Name = "HighFactor",
            Value = 1.5,
            Weight = 1.0
        };

        // Assert
        Assert.Equal("HighFactor", factor.Name);
        Assert.Equal(1.5, factor.Value);
        Assert.Equal(1.0, factor.Weight);
    }
}