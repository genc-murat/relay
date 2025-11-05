using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Models;

public class ForecastResultTests
{
    [Fact]
    public void ForecastResult_Should_Initialize_With_Default_Values()
    {
        // Act
        var forecastResult = new ForecastResult();

        // Assert
        Assert.Equal(0.0, forecastResult.Current);
        Assert.Equal(0.0, forecastResult.Forecast5Min);
        Assert.Equal(0.0, forecastResult.Forecast15Min);
        Assert.Equal(0.0, forecastResult.Forecast60Min);
        Assert.Equal(0.0, forecastResult.Confidence);
    }

    [Fact]
    public void ForecastResult_Should_Allow_Setting_Properties()
    {
        // Arrange
        var forecastResult = new ForecastResult();

        // Act
        forecastResult.Current = 0.5;
        forecastResult.Forecast5Min = 0.6;
        forecastResult.Forecast15Min = 0.7;
        forecastResult.Forecast60Min = 0.8;
        forecastResult.Confidence = 0.9;

        // Assert
        Assert.Equal(0.5, forecastResult.Current);
        Assert.Equal(0.6, forecastResult.Forecast5Min);
        Assert.Equal(0.7, forecastResult.Forecast15Min);
        Assert.Equal(0.8, forecastResult.Forecast60Min);
        Assert.Equal(0.9, forecastResult.Confidence);
    }

    [Fact]
    public void ForecastResult_Should_Support_Object_Initialization()
    {
        // Act
        var forecastResult = new ForecastResult
        {
            Current = 0.3,
            Forecast5Min = 0.35,
            Forecast15Min = 0.4,
            Forecast60Min = 0.45,
            Confidence = 0.85
        };

        // Assert
        Assert.Equal(0.3, forecastResult.Current);
        Assert.Equal(0.35, forecastResult.Forecast5Min);
        Assert.Equal(0.4, forecastResult.Forecast15Min);
        Assert.Equal(0.45, forecastResult.Forecast60Min);
        Assert.Equal(0.85, forecastResult.Confidence);
    }
}