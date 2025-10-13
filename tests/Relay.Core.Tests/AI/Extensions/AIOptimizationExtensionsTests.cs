using System;
using System.Collections.Generic;
using System.Linq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.AI.Extensions
{
    public class AIOptimizationExtensionsTests
    {
        [Fact]
        public void AddRange_AddsRangeToDictionary()
        {
            var dict = new Dictionary<string, int> { ["a"] = 1 };
            var other = new Dictionary<string, int> { ["b"] = 2, ["c"] = 3 };

            dict.AddRange(other);

            Assert.Equal(3, dict.Count);
            Assert.Equal(1, dict["a"]);
            Assert.Equal(2, dict["b"]);
            Assert.Equal(3, dict["c"]);
        }

        [Fact]
        public void AddRange_OverwritesExistingKeys()
        {
            var dict = new Dictionary<string, int> { ["a"] = 1 };
            var other = new Dictionary<string, int> { ["a"] = 2 };

            dict.AddRange(other);

            Assert.Single(dict);
            Assert.Equal(2, dict["a"]);
        }

        [Fact]
        public void AddRange_ThrowsArgumentNullException_WhenDictionaryIsNull()
        {
            Dictionary<string, int> dict = null;
            var other = new Dictionary<string, int>();

            Assert.Throws<ArgumentNullException>(() => dict.AddRange(other));
        }

        [Fact]
        public void AddRange_ThrowsArgumentNullException_WhenOtherIsNull()
        {
            var dict = new Dictionary<string, int>();
            Dictionary<string, int> other = null;

            Assert.Throws<ArgumentNullException>(() => dict.AddRange(other));
        }

        [Fact]
        public void GetValueOrDefault_ReturnsValue_WhenKeyExists()
        {
            var dict = new Dictionary<string, int> { ["a"] = 1 };

            var result = dict.GetValueOrDefault("a", 99);

            Assert.Equal(1, result);
        }

        [Fact]
        public void GetValueOrDefault_ReturnsDefaultValue_WhenKeyDoesNotExist()
        {
            var dict = new Dictionary<string, int> { ["a"] = 1 };

            var result = dict.GetValueOrDefault("b", 99);

            Assert.Equal(99, result);
        }

        [Fact]
        public void GetValueOrDefault_ThrowsArgumentNullException_WhenDictionaryIsNull()
        {
            Dictionary<string, int> dict = null;

            Assert.Throws<ArgumentNullException>(() => dict.GetValueOrDefault("a", 99));
        }

        [Fact]
        public void AverageOrDefault_ReturnsAverage_WhenCollectionIsNotEmpty()
        {
            var source = new[] { 1.0, 2.0, 3.0 };

            var result = source.AverageOrDefault();

            Assert.Equal(2.0, result);
        }

        [Fact]
        public void AverageOrDefault_ReturnsDefaultValue_WhenCollectionIsEmpty()
        {
            var source = Array.Empty<double>();

            var result = source.AverageOrDefault(5.0);

            Assert.Equal(5.0, result);
        }

        [Fact]
        public void AverageOrDefault_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.AverageOrDefault());
        }

        [Fact]
        public void Median_ReturnsMedian_OddCount()
        {
            var source = new[] { 1.0, 3.0, 2.0 };

            var result = source.Median();

            Assert.Equal(2.0, result);
        }

        [Fact]
        public void Median_ReturnsMedian_EvenCount()
        {
            var source = new[] { 1.0, 2.0, 3.0, 4.0 };

            var result = source.Median();

            Assert.Equal(2.5, result);
        }

        [Fact]
        public void Median_ReturnsZero_WhenEmpty()
        {
            var source = Array.Empty<double>();

            var result = source.Median();

            Assert.Equal(0.0, result);
        }

        [Fact]
        public void Median_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.Median());
        }

        [Fact]
        public void StandardDeviation_CalculatesCorrectly()
        {
            var source = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            var result = source.StandardDeviation();

            Assert.Equal(1.4142135623730951, result, 10);
        }

        [Fact]
        public void StandardDeviation_ReturnsZero_WhenEmpty()
        {
            var source = Array.Empty<double>();

            var result = source.StandardDeviation();

            Assert.Equal(0.0, result);
        }

        [Fact]
        public void StandardDeviation_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.StandardDeviation());
        }

        [Fact]
        public void Percentile_CalculatesCorrectly()
        {
            var source = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            var result = source.Percentile(50);

            Assert.Equal(3.0, result);
        }

        [Fact]
        public void Percentile_ReturnsFirst_WhenZero()
        {
            var source = new[] { 1.0, 2.0, 3.0 };

            var result = source.Percentile(0);

            Assert.Equal(1.0, result);
        }

        [Fact]
        public void Percentile_ReturnsLast_WhenHundred()
        {
            var source = new[] { 1.0, 2.0, 3.0 };

            var result = source.Percentile(100);

            Assert.Equal(3.0, result);
        }

        [Fact]
        public void Percentile_ReturnsZero_WhenEmpty()
        {
            var source = Array.Empty<double>();

            var result = source.Percentile(50);

            Assert.Equal(0.0, result);
        }

        [Fact]
        public void Percentile_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.Percentile(50));
        }

        [Fact]
        public void Percentile_ThrowsArgumentOutOfRangeException_WhenPercentileInvalid()
        {
            var source = new[] { 1.0, 2.0 };

            Assert.Throws<ArgumentOutOfRangeException>(() => source.Percentile(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Percentile(101));
        }

        [Fact]
        public void Clamp_ClampsValue()
        {
            Assert.Equal(5, 10.Clamp(0, 5));
            Assert.Equal(0, (-1).Clamp(0, 5));
            Assert.Equal(3, 3.Clamp(0, 5));
        }

        [Fact]
        public void Normalize_NormalizesValue()
        {
            var result = 5.0.Normalize(0, 10);

            Assert.Equal(0.5, result);
        }

        [Fact]
        public void Normalize_ReturnsHalf_WhenMinEqualsMax()
        {
            var result = 5.0.Normalize(5, 5);

            Assert.Equal(0.5, result);
        }

        [Fact]
        public void ExponentialMovingAverage_CalculatesCorrectly()
        {
            var source = new[] { 1.0, 2.0, 3.0 };

            var result = source.ExponentialMovingAverage(0.5);

            Assert.Equal(2.25, result, 1);
        }

        [Fact]
        public void ExponentialMovingAverage_ReturnsZero_WhenEmpty()
        {
            var source = Array.Empty<double>();

            var result = source.ExponentialMovingAverage();

            Assert.Equal(0.0, result);
        }

        [Fact]
        public void ExponentialMovingAverage_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.ExponentialMovingAverage());
        }

        [Fact]
        public void ExponentialMovingAverage_ThrowsArgumentOutOfRangeException_WhenAlphaInvalid()
        {
            var source = new[] { 1.0 };

            Assert.Throws<ArgumentOutOfRangeException>(() => source.ExponentialMovingAverage(-0.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.ExponentialMovingAverage(1.1));
        }

        [Fact]
        public void WeightedAverage_CalculatesCorrectly()
        {
            var source = new[] { new { Value = 10.0, Weight = 1.0 }, new { Value = 20.0, Weight = 2.0 } };

            var result = source.WeightedAverage(x => x.Value, x => x.Weight);

            Assert.Equal(16.666666666666668, result, 10);
        }

        [Fact]
        public void WeightedAverage_ReturnsZero_WhenEmpty()
        {
            var source = Array.Empty<object>();

            var result = source.WeightedAverage(_ => 1.0, _ => 1.0);

            Assert.Equal(0.0, result);
        }

        [Fact]
        public void WeightedAverage_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<object> source = null;

            Assert.Throws<ArgumentNullException>(() => source.WeightedAverage(_ => 1.0, _ => 1.0));
        }

        [Fact]
        public void WeightedAverage_ThrowsArgumentNullException_WhenSelectorsAreNull()
        {
            var source = new[] { 1 };

            Assert.Throws<ArgumentNullException>(() => source.WeightedAverage(null, _ => 1.0));
            Assert.Throws<ArgumentNullException>(() => source.WeightedAverage(_ => 1.0, null));
        }

        [Fact]
        public void CoefficientOfVariation_CalculatesCorrectly()
        {
            var source = new[] { 2.0, 4.0, 6.0 };

            var result = source.CoefficientOfVariation();

            Assert.Equal(0.408, result, 1);
        }

        [Fact]
        public void CoefficientOfVariation_ReturnsZero_WhenAverageIsZero()
        {
            var source = new[] { 0.0, 0.0 };

            var result = source.CoefficientOfVariation();

            Assert.Equal(0.0, result);
        }

        [Fact]
        public void CoefficientOfVariation_ReturnsZero_WhenEmpty()
        {
            var source = Array.Empty<double>();

            var result = source.CoefficientOfVariation();

            Assert.Equal(0.0, result);
        }

        [Fact]
        public void CoefficientOfVariation_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.CoefficientOfVariation());
        }

        [Fact]
        public void HasOutliers_ReturnsTrue_WhenOutliersPresent()
        {
            var source = new[] { 1.0, 2.0, 3.0, 4.0, 100.0 };

            var result = source.HasOutliers();

            Assert.True(result);
        }

        [Fact]
        public void HasOutliers_ReturnsFalse_WhenNoOutliers()
        {
            var source = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            var result = source.HasOutliers();

            Assert.False(result);
        }

        [Fact]
        public void HasOutliers_ReturnsFalse_WhenLessThanFourElements()
        {
            var source = new[] { 1.0, 2.0, 3.0 };

            var result = source.HasOutliers();

            Assert.False(result);
        }

        [Fact]
        public void HasOutliers_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.HasOutliers());
        }

        [Fact]
        public void RemoveOutliers_RemovesOutliers()
        {
            var source = new[] { 1.0, 2.0, 3.0, 4.0, 100.0 };

            var result = source.RemoveOutliers().ToList();

            Assert.Equal(4, result.Count);
            Assert.DoesNotContain(100.0, result);
        }

        [Fact]
        public void RemoveOutliers_ReturnsOriginal_WhenLessThanFourElements()
        {
            var source = new[] { 1.0, 2.0, 3.0 };

            var result = source.RemoveOutliers().ToList();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void RemoveOutliers_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.RemoveOutliers());
        }

        [Fact]
        public void SmoothTimeSeries_SmoothsSeries()
        {
            var source = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            var result = source.SmoothTimeSeries(3).ToList();

            Assert.Equal(5, result.Count);
            Assert.Equal(3.0, result[2], 1);
        }

        [Fact]
        public void SmoothTimeSeries_ReturnsOriginal_WhenSmallerThanWindow()
        {
            var source = new[] { 1.0, 2.0 };

            var result = source.SmoothTimeSeries(3).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void SmoothTimeSeries_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.SmoothTimeSeries());
        }

        [Fact]
        public void SmoothTimeSeries_ThrowsArgumentOutOfRangeException_WhenWindowSizeInvalid()
        {
            var source = new[] { 1.0 };

            Assert.Throws<ArgumentOutOfRangeException>(() => source.SmoothTimeSeries(0));
        }

        [Fact]
        public void RateOfChange_CalculatesCorrectly()
        {
            var source = new[] { 1.0, 2.0, 3.0 };

            var result = source.RateOfChange().ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(1.0, result[0]);
            Assert.Equal(0.5, result[1]);
        }

        [Fact]
        public void RateOfChange_ReturnsEmpty_WhenLessThanTwoElements()
        {
            var source = new[] { 1.0 };

            var result = source.RateOfChange().ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void RateOfChange_HandlesZeroPrevious()
        {
            var source = new[] { 0.0, 1.0 };

            var result = source.RateOfChange().ToList();

            Assert.Single(result);
            Assert.Equal(0.0, result[0]);
        }

        [Fact]
        public void RateOfChange_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.RateOfChange());
        }

        [Fact]
        public void ZScores_CalculatesCorrectly()
        {
            var source = new[] { 1.0, 2.0, 3.0 };

            var result = source.ZScores().ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(-1.224744871391589, result[0], 10);
            Assert.Equal(0.0, result[1], 10);
            Assert.Equal(1.224744871391589, result[2], 10);
        }

        [Fact]
        public void ZScores_ReturnsEmpty_WhenEmpty()
        {
            var source = Array.Empty<double>();

            var result = source.ZScores().ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void ZScores_ReturnsZeros_WhenZeroStdDev()
        {
            var source = new[] { 2.0, 2.0, 2.0 };

            var result = source.ZScores().ToList();

            Assert.All(result, x => Assert.Equal(0.0, x));
        }

        [Fact]
        public void ZScores_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<double> source = null;

            Assert.Throws<ArgumentNullException>(() => source.ZScores());
        }

        [Fact]
        public void Batch_BatchesCollection()
        {
            var source = new[] { 1, 2, 3, 4, 5 };

            var result = source.Batch(2).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { 1, 2 }, result[0]);
            Assert.Equal(new[] { 3, 4 }, result[1]);
            Assert.Equal(new[] { 5 }, result[2]);
        }

        [Fact]
        public void Batch_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            IEnumerable<int> source = null;

            Assert.Throws<ArgumentNullException>(() => source.Batch(2));
        }

        [Fact]
        public void Batch_ThrowsArgumentOutOfRangeException_WhenBatchSizeInvalid()
        {
            var source = new[] { 1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => source.Batch(0));
        }
    }
}