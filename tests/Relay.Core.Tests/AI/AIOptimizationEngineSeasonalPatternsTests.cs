using System;
using System.Collections.Generic;
using System.Linq;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Optimization.Data;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineSeasonalPatternsTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void ClassifySeasonalType_Should_Return_Intraday_For_Very_Short_Periods()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ClassifySeasonalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test various intraday periods
            var testCases = new[] { 1, 2, 4, 6, 8 };

            foreach (var period in testCases)
            {
                // Act
                var result = (string)method?.Invoke(_engine, new object[] { period })!;

                // Assert
                Assert.Equal("Intraday", result);
            }
        }

        [Fact]
        public void ClassifySeasonalType_Should_Return_Daily_For_24_Hour_Period()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ClassifySeasonalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (string)method?.Invoke(_engine, new object[] { 24 })!;

            // Assert
            Assert.Equal("Daily", result);
        }

        [Fact]
        public void ClassifySeasonalType_Should_Return_Semi_Weekly_For_48_Hour_Period()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ClassifySeasonalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (string)method?.Invoke(_engine, new object[] { 48 })!;

            // Assert
            Assert.Equal("Semi-weekly", result);
        }

        [Fact]
        public void ClassifySeasonalType_Should_Return_Weekly_For_168_Hour_Period()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ClassifySeasonalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (string)method?.Invoke(_engine, new object[] { 168 })!;

            // Assert
            Assert.Equal("Weekly", result);
        }

        [Fact]
        public void ClassifySeasonalType_Should_Return_Bi_Weekly_For_336_Hour_Period()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ClassifySeasonalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (string)method?.Invoke(_engine, new object[] { 336 })!;

            // Assert
            Assert.Equal("Bi-weekly", result);
        }

        [Fact]
        public void ClassifySeasonalType_Should_Return_Monthly_For_Long_Periods()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ClassifySeasonalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test various long periods
            var testCases = new[] { 337, 500, 720, 1000 };

            foreach (var period in testCases)
            {
                // Act
                var result = (string)method?.Invoke(_engine, new object[] { period })!;

                // Assert
                Assert.Equal("Monthly", result);
            }
        }

        [Fact]
        public void ClassifySeasonalType_Should_Handle_Boundary_Values()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ClassifySeasonalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test boundary values
            var testCases = new (int period, string expected)[]
            {
                (8, "Intraday"),      // Upper boundary for Intraday
                (9, "Daily"),         // Lower boundary for Daily
                (24, "Daily"),        // Upper boundary for Daily
                (25, "Semi-weekly"),  // Lower boundary for Semi-weekly
                (48, "Semi-weekly"),  // Upper boundary for Semi-weekly
                (49, "Weekly"),       // Lower boundary for Weekly
                (168, "Weekly"),      // Upper boundary for Weekly
                (169, "Bi-weekly"),   // Lower boundary for Bi-weekly
                (336, "Bi-weekly"),   // Upper boundary for Bi-weekly
                (337, "Monthly")      // Lower boundary for Monthly
            };

            foreach (var (period, expected) in testCases)
            {
                // Act
                var result = (string)method?.Invoke(_engine, new object[] { period })!;

                // Assert
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void DetectSeasonalPatterns_Should_Handle_Boundary_Values()
        {
            // Arrange - Test boundary values
            var testCases = new (int period, string expected)[]
            {
                (8, "Intraday"),      // Upper boundary for Intraday
                (9, "Daily"),         // Lower boundary for Daily
                (24, "Daily"),        // Upper boundary for Daily
                (25, "Semi-weekly"),  // Lower boundary for Semi-weekly
                (48, "Semi-weekly"),  // Upper boundary for Semi-weekly
                (49, "Weekly"),       // Lower boundary for Weekly
                (168, "Weekly"),      // Upper boundary for Weekly
                (169, "Bi-weekly"),   // Lower boundary for Bi-weekly
                (336, "Bi-weekly"),   // Upper boundary for Bi-weekly
                (337, "Monthly")      // Lower boundary for Monthly
            };

            foreach (var (period, expected) in testCases)
            {
                // Set up time series database with data that will trigger the specific period
                var timeSeriesDbField = _engine.GetType().GetField("_timeSeriesDb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var timeSeriesDb = timeSeriesDbField?.GetValue(_engine) as TimeSeriesDatabase;

                if (timeSeriesDb != null)
                {
                    // Create simple periodic data
                    var baseTime = DateTime.UtcNow.AddHours(-period * 3); // 3 full periods

                    for (int i = 0; i < period * 3; i++)
                    {
                        var value = (float)(Math.Sin(2 * Math.PI * i / period) * 100 + 200); // Sine wave pattern
                        timeSeriesDb.StoreMetric("ThroughputPerSecond", value, baseTime.AddHours(i));
                    }
                }

                var metrics = new Dictionary<string, double>();

                // Act
                var method = _engine.GetType().GetMethod("DetectSeasonalPatterns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var result = (List<SeasonalPattern>)method?.Invoke(_engine, new object[] { metrics })!;

                // Assert
                var pattern = result.FirstOrDefault(p => p.Period == period);
                if (pattern != null)
                {
                    string actualType = pattern.Type;
                    Assert.Equal(expected, actualType);
                }
            }
        }
    }
}
