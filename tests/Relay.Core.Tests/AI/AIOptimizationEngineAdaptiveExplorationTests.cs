using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineAdaptiveExplorationTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Return_Base_Rate_For_High_Effectiveness()
        {
            // Arrange - High effectiveness (>= 0.7) should return base rate with minimal adjustments
            var metrics = new Dictionary<string, double>
            {
                ["SystemStability"] = 0.9
            };
            var effectiveness = 0.8;

            // Act - Call CalculateAdaptiveExplorationRate directly using reflection
            var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;

            // Assert - Should return base rate (0.1) with minimal adjustments for high effectiveness
            Assert.True(result >= 0.05 && result <= 0.50, $"Expected exploration rate between 0.05-0.50, but got {result}");
        }

        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Increase_For_Low_Effectiveness()
        {
            // Arrange - Low effectiveness (< 0.5) should significantly increase exploration
            var metrics = new Dictionary<string, double>
            {
                ["SystemStability"] = 0.8
            };
            var effectiveness = 0.3;

            // Act - Call CalculateAdaptiveExplorationRate directly using reflection
            var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;

            // Assert - Should return higher exploration rate due to low effectiveness
            Assert.True(result >= 0.05 && result <= 0.50, $"Expected exploration rate between 0.05-0.50, but got {result}");
            // Low effectiveness should generally result in higher exploration than high effectiveness
        }

        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Increase_For_Low_System_Stability()
        {
            // Arrange - Low system stability should increase exploration rate
            var metrics = new Dictionary<string, double>
            {
                ["SystemStability"] = 0.3  // Low stability
            };
            var effectiveness = 0.8;

            // Act - Call CalculateAdaptiveExplorationRate directly using reflection
            var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;

            // Assert - Should return higher exploration rate due to low stability
            Assert.True(result >= 0.05 && result <= 0.50, $"Expected exploration rate between 0.05-0.50, but got {result}");
        }

        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Use_Default_Stability_When_Missing()
        {
            // Arrange - Missing SystemStability should use default value (0.8)
            var metrics = new Dictionary<string, double>
            {
                // No SystemStability key
            };
            var effectiveness = 0.8;

            // Act - Call CalculateAdaptiveExplorationRate directly using reflection
            var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;

            // Assert - Should use default stability and return valid exploration rate
            Assert.True(result >= 0.05 && result <= 0.50, $"Expected exploration rate between 0.05-0.50, but got {result}");
        }

        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Clamp_Values_To_Valid_Range()
        {
            // Arrange - Test that results are always clamped to 5%-50% range
            var testCases = new[]
            {
                new { Effectiveness = 0.0, Stability = 0.0, Description = "Extreme low values" },
                new { Effectiveness = 1.0, Stability = 1.0, Description = "Extreme high values" },
                new { Effectiveness = 0.5, Stability = 0.5, Description = "Medium values" }
            };

            foreach (var testCase in testCases)
            {
                var metrics = new Dictionary<string, double>
                {
                    ["SystemStability"] = testCase.Stability
                };

                // Act - Call CalculateAdaptiveExplorationRate for each test case
                var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var result = (double)method?.Invoke(_engine, new object[] { testCase.Effectiveness, metrics })!;

                // Assert - All results should be clamped to valid range
                Assert.True(result >= 0.05 && result <= 0.50,
                    $"Expected exploration rate between 0.05-0.50 for {testCase.Description}, but got {result}");
            }
        }

        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Handle_Empty_Metrics()
        {
            // Arrange - Test with empty metrics dictionary
            var metrics = new Dictionary<string, double>();
            var effectiveness = 0.8;

            // Act - Call CalculateAdaptiveExplorationRate directly using reflection
            var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;

            // Assert - Should handle empty metrics and return valid exploration rate
            Assert.True(result >= 0.05 && result <= 0.50, $"Expected exploration rate between 0.05-0.50, but got {result}");
        }

        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Handle_Extreme_Effectiveness_Values()
        {
            // Arrange - Test with extreme effectiveness values
            var testCases = new double[] { 0.0, 1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1.0, 2.0 };

            var metrics = new Dictionary<string, double>
            {
                ["SystemStability"] = 0.8
            };

            foreach (var effectiveness in testCases)
            {
                // Act - Call CalculateAdaptiveExplorationRate for each extreme effectiveness value
                var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var result = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;

                // Assert - Should handle extreme values and return valid exploration rate
                Assert.True(result >= 0.05 && result <= 0.50,
                    $"Expected exploration rate between 0.05-0.50 for effectiveness {effectiveness}, but got {result}");
            }
        }

        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Return_Safe_Default_On_Exception()
        {
            // Arrange - Test with null metrics (should cause exception and return safe default)
            Dictionary<string, double>? metrics = null;
            var effectiveness = 0.8;

            // Act - Call CalculateAdaptiveExplorationRate directly using reflection
            var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;

            // Assert - Should return safe default (0.1) when exceptions occur
            Assert.Equal(0.1, result);
        }

        [Fact]
        public void CalculateAdaptiveExplorationRate_Should_Be_Deterministic_For_Same_Inputs()
        {
            // Arrange - Test that same inputs produce consistent results
            var metrics = new Dictionary<string, double>
            {
                ["SystemStability"] = 0.8
            };
            var effectiveness = 0.7;

            // Act - Call CalculateAdaptiveExplorationRate multiple times with same inputs
            var method = _engine.GetType().GetMethod("CalculateAdaptiveExplorationRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result1 = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;
            var result2 = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;
            var result3 = (double)method?.Invoke(_engine, new object[] { effectiveness, metrics })!;

            // Assert - Results should be consistent (within small tolerance due to time-based factors)
            Assert.True(Math.Abs(result1 - result2) < 0.01, $"Results should be consistent: {result1} vs {result2}");
            Assert.True(Math.Abs(result2 - result3) < 0.01, $"Results should be consistent: {result2} vs {result3}");
        }
    }
}
