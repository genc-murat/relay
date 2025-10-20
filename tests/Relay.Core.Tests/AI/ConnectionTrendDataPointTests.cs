using System;
using Xunit;
using Relay.Core.AI;

namespace Relay.Core.Tests.AI
{
    public class ConnectionTrendDataPointTests
    {
        [Fact]
        public void Constructor_Should_Initialize_With_Default_Values()
        {
            // Arrange & Act
            var dataPoint = new ConnectionTrendDataPoint();

            // Assert
            Assert.Equal(default(DateTime), dataPoint.Timestamp);
            Assert.Equal(0, dataPoint.ConnectionCount);
            Assert.Equal(0.0, dataPoint.MovingAverage5Min);
            Assert.Equal(0.0, dataPoint.MovingAverage15Min);
            Assert.Equal(0.0, dataPoint.MovingAverage1Hour);
            Assert.Equal("stable", dataPoint.TrendDirection);
            Assert.Equal(0.0, dataPoint.VolatilityScore);
        }

        [Fact]
        public void Properties_Should_Be_Set_And_Get_Correctly()
        {
            // Arrange
            var timestamp = new DateTime(2023, 10, 20, 14, 30, 0);
            var dataPoint = new ConnectionTrendDataPoint();

            // Act
            dataPoint.Timestamp = timestamp;
            dataPoint.ConnectionCount = 150;
            dataPoint.MovingAverage5Min = 145.5;
            dataPoint.MovingAverage15Min = 142.3;
            dataPoint.MovingAverage1Hour = 138.7;
            dataPoint.TrendDirection = "increasing";
            dataPoint.VolatilityScore = 12.5;

            // Assert
            Assert.Equal(timestamp, dataPoint.Timestamp);
            Assert.Equal(150, dataPoint.ConnectionCount);
            Assert.Equal(145.5, dataPoint.MovingAverage5Min);
            Assert.Equal(142.3, dataPoint.MovingAverage15Min);
            Assert.Equal(138.7, dataPoint.MovingAverage1Hour);
            Assert.Equal("increasing", dataPoint.TrendDirection);
            Assert.Equal(12.5, dataPoint.VolatilityScore);
        }

        [Fact]
        public void TrendDirection_Should_Default_To_Stable()
        {
            // Arrange & Act
            var dataPoint = new ConnectionTrendDataPoint();

            // Assert
            Assert.Equal("stable", dataPoint.TrendDirection);
        }

        [Fact]
        public void Numeric_Properties_Should_Default_To_Zero()
        {
            // Arrange & Act
            var dataPoint = new ConnectionTrendDataPoint();

            // Assert
            Assert.Equal(0, dataPoint.ConnectionCount);
            Assert.Equal(0.0, dataPoint.MovingAverage5Min);
            Assert.Equal(0.0, dataPoint.MovingAverage15Min);
            Assert.Equal(0.0, dataPoint.MovingAverage1Hour);
            Assert.Equal(0.0, dataPoint.VolatilityScore);
        }

        [Fact]
        public void Timestamp_Should_Default_To_DateTime_MinValue()
        {
            // Arrange & Act
            var dataPoint = new ConnectionTrendDataPoint();

            // Assert
            Assert.Equal(default(DateTime), dataPoint.Timestamp);
        }

        [Fact]
        public void Can_Set_TrendDirection_To_Different_Values()
        {
            // Arrange
            var dataPoint = new ConnectionTrendDataPoint();

            // Act & Assert
            dataPoint.TrendDirection = "increasing";
            Assert.Equal("increasing", dataPoint.TrendDirection);

            dataPoint.TrendDirection = "decreasing";
            Assert.Equal("decreasing", dataPoint.TrendDirection);

            dataPoint.TrendDirection = "stable";
            Assert.Equal("stable", dataPoint.TrendDirection);
        }

        [Fact]
        public void Can_Set_Negative_Values_For_ConnectionCount()
        {
            // Arrange
            var dataPoint = new ConnectionTrendDataPoint();

            // Act
            dataPoint.ConnectionCount = -5;

            // Assert
            Assert.Equal(-5, dataPoint.ConnectionCount);
        }

        [Fact]
        public void Can_Set_Negative_Values_For_MovingAverages()
        {
            // Arrange
            var dataPoint = new ConnectionTrendDataPoint();

            // Act
            dataPoint.MovingAverage5Min = -10.5;
            dataPoint.MovingAverage15Min = -15.2;
            dataPoint.MovingAverage1Hour = -20.8;

            // Assert
            Assert.Equal(-10.5, dataPoint.MovingAverage5Min);
            Assert.Equal(-15.2, dataPoint.MovingAverage15Min);
            Assert.Equal(-20.8, dataPoint.MovingAverage1Hour);
        }

        [Fact]
        public void Can_Set_Negative_VolatilityScore()
        {
            // Arrange
            var dataPoint = new ConnectionTrendDataPoint();

            // Act
            dataPoint.VolatilityScore = -5.5;

            // Assert
            Assert.Equal(-5.5, dataPoint.VolatilityScore);
        }

        [Fact]
        public void Can_Set_Future_Timestamp()
        {
            // Arrange
            var futureDate = new DateTime(2030, 1, 1);
            var dataPoint = new ConnectionTrendDataPoint();

            // Act
            dataPoint.Timestamp = futureDate;

            // Assert
            Assert.Equal(futureDate, dataPoint.Timestamp);
        }

        [Fact]
        public void Can_Set_Past_Timestamp()
        {
            // Arrange
            var pastDate = new DateTime(2000, 1, 1);
            var dataPoint = new ConnectionTrendDataPoint();

            // Act
            dataPoint.Timestamp = pastDate;

            // Assert
            Assert.Equal(pastDate, dataPoint.Timestamp);
        }
    }
}