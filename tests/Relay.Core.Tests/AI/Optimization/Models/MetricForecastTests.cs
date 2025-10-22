using System;
using Relay.Core.AI.Optimization.Models;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Models
{
    public class MetricForecastTests
    {
        [Fact]
        public void Constructor_InitializesEmptyArrays()
        {
            // Act
            var forecast = new MetricForecast();

            // Assert
            Assert.NotNull(forecast.ForecastedValues);
            Assert.NotNull(forecast.LowerBound);
            Assert.NotNull(forecast.UpperBound);
            Assert.Empty(forecast.ForecastedValues);
            Assert.Empty(forecast.LowerBound);
            Assert.Empty(forecast.UpperBound);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var forecast = new MetricForecast();
            var forecastedValues = new float[] { 1.0f, 2.0f, 3.0f };
            var lowerBound = new float[] { 0.8f, 1.8f, 2.8f };
            var upperBound = new float[] { 1.2f, 2.2f, 3.2f };

            // Act
            forecast.ForecastedValues = forecastedValues;
            forecast.LowerBound = lowerBound;
            forecast.UpperBound = upperBound;

            // Assert
            Assert.Equal(forecastedValues, forecast.ForecastedValues);
            Assert.Equal(lowerBound, forecast.LowerBound);
            Assert.Equal(upperBound, forecast.UpperBound);
        }

        [Fact]
        public void Arrays_CanHandleDifferentLengths()
        {
            // Arrange
            var forecast = new MetricForecast();

            // Act - Set arrays of different lengths (though in practice they should be consistent)
            forecast.ForecastedValues = new float[] { 1.0f, 2.0f };
            forecast.LowerBound = new float[] { 0.8f, 1.8f, 2.8f };
            forecast.UpperBound = new float[] { 1.2f };

            // Assert
            Assert.Equal(2, forecast.ForecastedValues.Length);
            Assert.Equal(3, forecast.LowerBound.Length);
            Assert.Single(forecast.UpperBound);
        }

        [Fact]
        public void Arrays_CanBeSetToNull()
        {
            // Arrange
            var forecast = new MetricForecast();

            // Act
            forecast.ForecastedValues = null!;
            forecast.LowerBound = null!;
            forecast.UpperBound = null!;

            // Assert
            Assert.Null(forecast.ForecastedValues);
            Assert.Null(forecast.LowerBound);
            Assert.Null(forecast.UpperBound);
        }

        [Fact]
        public void Arrays_CanContainSpecialValues()
        {
            // Arrange
            var forecast = new MetricForecast();

            // Act - Test with special float values
            forecast.ForecastedValues = new float[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f };
            forecast.LowerBound = new float[] { float.MinValue, float.MaxValue };
            forecast.UpperBound = new float[] { float.Epsilon };

            // Assert
            Assert.Equal(4, forecast.ForecastedValues.Length);
            Assert.Equal(2, forecast.LowerBound.Length);
            Assert.Single(forecast.UpperBound);
            Assert.True(float.IsNaN(forecast.ForecastedValues[0]));
            Assert.True(float.IsPositiveInfinity(forecast.ForecastedValues[1]));
            Assert.True(float.IsNegativeInfinity(forecast.ForecastedValues[2]));
        }

        [Fact]
        public void Arrays_ShareReferencesWhenAssigned()
        {
            // Arrange
            var forecast1 = new MetricForecast();
            var forecast2 = new MetricForecast();
            var sharedArray = new float[] { 1.0f, 2.0f, 3.0f };

            // Act
            forecast1.ForecastedValues = sharedArray;
            forecast2.ForecastedValues = sharedArray;

            // Modify one array - this affects both since they reference the same array
            forecast1.ForecastedValues[0] = 999f;

            // Assert - Both references point to the same array (reference sharing behavior)
            Assert.Equal(999f, forecast1.ForecastedValues[0]);
            Assert.Equal(999f, forecast2.ForecastedValues[0]); // Also affected due to shared reference
        }
    }
}