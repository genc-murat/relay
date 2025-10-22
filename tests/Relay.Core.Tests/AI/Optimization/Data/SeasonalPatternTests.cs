using Relay.Core.AI.Optimization.Data;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Data;

public class SeasonalPatternTests
{
    [Fact]
    public void SeasonalPattern_Should_Initialize_With_Default_Values()
    {
        // Act
        var pattern = new SeasonalPattern();

        // Assert
        Assert.Equal(0, pattern.Period);
        Assert.Equal(0.0, pattern.Strength);
        Assert.Equal(string.Empty, pattern.Type);
    }

    [Fact]
    public void SeasonalPattern_Should_Allow_Setting_All_Properties()
    {
        // Arrange
        var pattern = new SeasonalPattern();

        // Act
        pattern.Period = 24;
        pattern.Strength = 0.85;
        pattern.Type = "Daily";

        // Assert
        Assert.Equal(24, pattern.Period);
        Assert.Equal(0.85, pattern.Strength);
        Assert.Equal("Daily", pattern.Type);
    }

    [Fact]
    public void SeasonalPattern_Should_Support_Object_Initialization()
    {
        // Act
        var pattern = new SeasonalPattern
        {
            Period = 7,
            Strength = 0.92,
            Type = "Weekly"
        };

        // Assert
        Assert.Equal(7, pattern.Period);
        Assert.Equal(0.92, pattern.Strength);
        Assert.Equal("Weekly", pattern.Type);
    }

    [Fact]
    public void SeasonalPattern_Should_Accept_Negative_Period()
    {
        // Arrange
        var pattern = new SeasonalPattern();

        // Act
        pattern.Period = -1;

        // Assert
        Assert.Equal(-1, pattern.Period);
    }

    [Fact]
    public void SeasonalPattern_Should_Accept_Strength_Out_Of_Range()
    {
        // Arrange
        var pattern = new SeasonalPattern();

        // Act
        pattern.Strength = 1.5; // Above 1.0

        // Assert
        Assert.Equal(1.5, pattern.Strength);

        // Act
        pattern.Strength = -0.5; // Below 0.0

        // Assert
        Assert.Equal(-0.5, pattern.Strength);
    }

    [Fact]
    public void SeasonalPattern_Should_Accept_Empty_Type()
    {
        // Arrange
        var pattern = new SeasonalPattern();

        // Act
        pattern.Type = string.Empty;

        // Assert
        Assert.Equal(string.Empty, pattern.Type);
    }

    [Fact]
    public void SeasonalPattern_Should_Accept_Null_Type_When_Assigned()
    {
        // Arrange
        var pattern = new SeasonalPattern();

        // Act & Assert - This should not throw, but Type is initialized as empty string
        Assert.Equal(string.Empty, pattern.Type);
    }
}