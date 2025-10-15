using System;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class TimeSeriesStatisticsTests
    {
        [Fact]
        public void CalculateStdDev_Should_Return_Zero_For_Single_Value()
        {
            // Arrange
            var values = new[] { 5.0f };

            // Act
            var result = TimeSeriesStatistics.CalculateStdDev(values);

            // Assert
            Assert.Equal(0.0, result, 5);
        }

        [Fact]
        public void CalculateStdDev_Should_Calculate_Correct_Standard_Deviation()
        {
            // Arrange - Values: 1, 2, 3, 4, 5 (mean = 3, variance = 2, stddev = sqrt(2) ≈ 1.414)
            var values = new[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };
            var expected = Math.Sqrt(2.0); // ≈ 1.414213562

            // Act
            var result = TimeSeriesStatistics.CalculateStdDev(values);

            // Assert
            Assert.Equal(expected, result, 5);
        }

        [Fact]
        public void CalculateStdDev_Should_Handle_Large_Values()
        {
            // Arrange
            var values = new[] { 1000.0f, 2000.0f, 3000.0f };

            // Act
            var result = TimeSeriesStatistics.CalculateStdDev(values);

            // Assert
            Assert.True(result > 0);
            // mean = 2000, deviations: -1000, 0, 1000
            // variance = (1000000 + 0 + 1000000) / 3 = 2000000 / 3 ≈ 666666.67
            // stddev = sqrt(666666.67) ≈ 816.50
            Assert.Equal(816.5, result, 1);
        }

        [Fact]
        public void CalculateStdDev_Should_Handle_Decimal_Values()
        {
            // Arrange
            var values = new[] { 1.5f, 2.5f, 3.5f };

            // Act
            var result = TimeSeriesStatistics.CalculateStdDev(values);

            // Assert
            // mean = 2.5, deviations: -1, 0, 1
            // variance = (1 + 0 + 1) / 3 = 2/3 ≈ 0.6667
            // stddev = sqrt(0.6667) ≈ 0.8165
            Assert.Equal(0.8165, result, 4);
        }

        [Fact]
        public void CalculateMedian_Should_Return_Value_For_Single_Element()
        {
            // Arrange
            var values = new[] { 42.0f };

            // Act
            var result = TimeSeriesStatistics.CalculateMedian(values);

            // Assert
            Assert.Equal(42.0f, result);
        }

        [Fact]
        public void CalculateMedian_Should_Return_Middle_Value_For_Odd_Count()
        {
            // Arrange
            var values = new[] { 1.0f, 3.0f, 2.0f }; // Will be sorted: 1, 2, 3

            // Act
            var result = TimeSeriesStatistics.CalculateMedian(values);

            // Assert
            Assert.Equal(2.0f, result);
        }

        [Fact]
        public void CalculateMedian_Should_Return_Average_For_Even_Count()
        {
            // Arrange
            var values = new[] { 1.0f, 2.0f, 4.0f, 3.0f }; // Will be sorted: 1, 2, 3, 4

            // Act
            var result = TimeSeriesStatistics.CalculateMedian(values);

            // Assert
            Assert.Equal(2.5f, result); // (2 + 3) / 2
        }

        [Fact]
        public void CalculateMedian_Should_Handle_Unsorted_Array()
        {
            // Arrange
            var values = new[] { 5.0f, 1.0f, 9.0f, 3.0f, 7.0f }; // Sorted: 1, 3, 5, 7, 9

            // Act
            var result = TimeSeriesStatistics.CalculateMedian(values);

            // Assert
            Assert.Equal(5.0f, result);
        }

        [Fact]
        public void CalculatePercentile_Should_Return_Value_For_Single_Element()
        {
            // Arrange
            var values = new[] { 42.0f };

            // Act
            var result = TimeSeriesStatistics.CalculatePercentile(values, 0.5);

            // Assert
            Assert.Equal(42.0f, result);
        }

        [Fact]
        public void CalculatePercentile_Should_Return_First_Value_For_Zero_Percentile()
        {
            // Arrange
            var values = new[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };

            // Act
            var result = TimeSeriesStatistics.CalculatePercentile(values, 0.0);

            // Assert
            Assert.Equal(1.0f, result);
        }

        [Fact]
        public void CalculatePercentile_Should_Return_Last_Value_For_100_Percentile()
        {
            // Arrange
            var values = new[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };

            // Act
            var result = TimeSeriesStatistics.CalculatePercentile(values, 1.0);

            // Assert
            Assert.Equal(5.0f, result);
        }

        [Fact]
        public void CalculatePercentile_Should_Calculate_Median_For_50_Percentile()
        {
            // Arrange
            var values = new[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };

            // Act
            var result = TimeSeriesStatistics.CalculatePercentile(values, 0.5);

            // Assert
            Assert.Equal(3.0f, result); // Middle value
        }

        [Fact]
        public void CalculatePercentile_Should_Calculate_25th_Percentile()
        {
            // Arrange
            var values = new[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f };

            // Act
            var result = TimeSeriesStatistics.CalculatePercentile(values, 0.25);

            // Assert
            Assert.Equal(2.0f, result); // 25th percentile should be at index 1 (0-based) in sorted array
        }

        [Fact]
        public void CalculatePercentile_Should_Calculate_75th_Percentile()
        {
            // Arrange
            var values = new[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f };

            // Act
            var result = TimeSeriesStatistics.CalculatePercentile(values, 0.75);

            // Assert
            Assert.Equal(6.0f, result); // 75th percentile should be at index 5 (0-based) in sorted array
        }

        [Fact]
        public void CalculatePercentile_Should_Handle_Percentile_Greater_Than_1()
        {
            // Arrange
            var values = new[] { 1.0f, 2.0f, 3.0f };

            // Act
            var result = TimeSeriesStatistics.CalculatePercentile(values, 1.5);

            // Assert
            Assert.Equal(3.0f, result); // Should clamp to last value
        }

        [Fact]
        public void CalculatePercentile_Should_Handle_Negative_Percentile()
        {
            // Arrange
            var values = new[] { 1.0f, 2.0f, 3.0f };

            // Act
            var result = TimeSeriesStatistics.CalculatePercentile(values, -0.1);

            // Assert
            Assert.Equal(1.0f, result); // Should clamp to first value
        }

        [Fact]
        public void CalculatePercentile_Should_Handle_Empty_Array()
        {
            // Arrange
            var values = Array.Empty<float>();

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => TimeSeriesStatistics.CalculatePercentile(values, 0.5));
        }

        [Fact]
        public void CalculateStdDev_Should_Handle_Empty_Array()
        {
            // Arrange
            var values = Array.Empty<float>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => TimeSeriesStatistics.CalculateStdDev(values));
        }

        [Fact]
        public void CalculateMedian_Should_Handle_Empty_Array()
        {
            // Arrange
            var values = Array.Empty<float>();

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => TimeSeriesStatistics.CalculateMedian(values));
        }
    }
}