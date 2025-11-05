using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI
{
    /// <summary>
    /// Extension methods for AI optimization engine.
    /// </summary>
    public static class AIOptimizationExtensions
    {
        /// <summary>
        /// Adds a range of key-value pairs to a dictionary.
        /// </summary>
        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Dictionary<TKey, TValue> other) where TKey : notnull
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            if (other == null) throw new ArgumentNullException(nameof(other));

            foreach (var kvp in other)
            {
                dictionary[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Gets a value from the dictionary or returns the default value if the key doesn't exist.
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Calculates the average of a collection of double values, returning 0 if empty.
        /// </summary>
        public static double AverageOrDefault(this IEnumerable<double> source, double defaultValue = 0.0)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            
            var list = source.ToList();
            return list.Any() ? list.Average() : defaultValue;
        }

        /// <summary>
        /// Calculates the median of a collection of double values.
        /// </summary>
        public static double Median(this IEnumerable<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            
            var sorted = source.OrderBy(x => x).ToList();
            if (!sorted.Any())
                return 0.0;

            int count = sorted.Count;
            int midIndex = count / 2;

            if (count % 2 == 0)
            {
                return (sorted[midIndex - 1] + sorted[midIndex]) / 2.0;
            }
            else
            {
                return sorted[midIndex];
            }
        }

        /// <summary>
        /// Calculates the standard deviation of a collection of double values.
        /// </summary>
        public static double StandardDeviation(this IEnumerable<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            
            var list = source.ToList();
            if (!list.Any())
                return 0.0;

            double average = list.Average();
            double sumOfSquares = list.Sum(x => Math.Pow(x - average, 2));
            return Math.Sqrt(sumOfSquares / list.Count);
        }

        /// <summary>
        /// Calculates the percentile of a collection of double values.
        /// </summary>
        /// <param name="source">The collection of values.</param>
        /// <param name="percentile">The percentile to calculate (0-100).</param>
        public static double Percentile(this IEnumerable<double> source, double percentile)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (percentile < 0 || percentile > 100)
                throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0 and 100");
            
            var sorted = source.OrderBy(x => x).ToList();
            if (!sorted.Any())
                return 0.0;

            if (percentile == 0)
                return sorted.First();
            if (percentile == 100)
                return sorted.Last();

            double index = (percentile / 100.0) * (sorted.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);

            if (lowerIndex == upperIndex)
                return sorted[lowerIndex];

            double weight = index - lowerIndex;
            return sorted[lowerIndex] * (1 - weight) + sorted[upperIndex] * weight;
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum value.
        /// </summary>
        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }

        /// <summary>
        /// Normalizes a value to a 0-1 range.
        /// </summary>
        public static double Normalize(this double value, double min, double max)
        {
            if (max == min) return 0.5;
            return (value - min) / (max - min);
        }

        /// <summary>
        /// Calculates the exponential moving average.
        /// </summary>
        public static double ExponentialMovingAverage(this IEnumerable<double> source, double alpha = 0.3)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (alpha < 0 || alpha > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1");
            
            var list = source.ToList();
            if (!list.Any())
                return 0.0;

            double ema = list.First();
            foreach (var value in list.Skip(1))
            {
                ema = alpha * value + (1 - alpha) * ema;
            }

            return ema;
        }

        /// <summary>
        /// Calculates the weighted average of a collection.
        /// </summary>
        public static double WeightedAverage<T>(this IEnumerable<T> source, Func<T, double> valueSelector, Func<T, double> weightSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
            if (weightSelector == null) throw new ArgumentNullException(nameof(weightSelector));
            
            var list = source.ToList();
            if (!list.Any())
                return 0.0;

            double sumWeightedValues = 0;
            double sumWeights = 0;

            foreach (var item in list)
            {
                double weight = weightSelector(item);
                sumWeightedValues += valueSelector(item) * weight;
                sumWeights += weight;
            }

            return sumWeights > 0 ? sumWeightedValues / sumWeights : 0.0;
        }

        /// <summary>
        /// Calculates the coefficient of variation (relative standard deviation).
        /// </summary>
        public static double CoefficientOfVariation(this IEnumerable<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            
            var list = source.ToList();
            if (!list.Any())
                return 0.0;

            double average = list.Average();
            if (Math.Abs(average) < double.Epsilon)
                return 0.0;

            double stdDev = list.StandardDeviation();
            return stdDev / Math.Abs(average);
        }

        /// <summary>
        /// Determines if a collection has outliers using the IQR method.
        /// </summary>
        public static bool HasOutliers(this IEnumerable<double> source, double iqrMultiplier = 1.5)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            
            var list = source.ToList();
            if (list.Count < 4)
                return false;

            double q1 = list.Percentile(25);
            double q3 = list.Percentile(75);
            double iqr = q3 - q1;

            double lowerBound = q1 - iqrMultiplier * iqr;
            double upperBound = q3 + iqrMultiplier * iqr;

            return list.Any(x => x < lowerBound || x > upperBound);
        }

        /// <summary>
        /// Removes outliers from a collection using the IQR method.
        /// </summary>
        public static IEnumerable<double> RemoveOutliers(this IEnumerable<double> source, double iqrMultiplier = 1.5)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            
            var list = source.ToList();
            if (list.Count < 4)
                return list;

            double q1 = list.Percentile(25);
            double q3 = list.Percentile(75);
            double iqr = q3 - q1;

            double lowerBound = q1 - iqrMultiplier * iqr;
            double upperBound = q3 + iqrMultiplier * iqr;

            return list.Where(x => x >= lowerBound && x <= upperBound);
        }

        /// <summary>
        /// Smooths a time series using a simple moving average.
        /// </summary>
        public static IEnumerable<double> SmoothTimeSeries(this IEnumerable<double> source, int windowSize = 3)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (windowSize < 1)
                throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be at least 1");
            
            var list = source.ToList();
            if (list.Count < windowSize)
                return list;

            var result = new List<double>();
            for (int i = 0; i < list.Count; i++)
            {
                int start = Math.Max(0, i - windowSize / 2);
                int end = Math.Min(list.Count - 1, i + windowSize / 2);
                double average = list.Skip(start).Take(end - start + 1).Average();
                result.Add(average);
            }

            return result;
        }

        /// <summary>
        /// Calculates the rate of change between consecutive values.
        /// </summary>
        public static IEnumerable<double> RateOfChange(this IEnumerable<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            
            var list = source.ToList();
            if (list.Count < 2)
                return Enumerable.Empty<double>();

            var result = new List<double>();
            for (int i = 1; i < list.Count; i++)
            {
                double previous = list[i - 1];
                if (Math.Abs(previous) < double.Epsilon)
                {
                    result.Add(0.0);
                }
                else
                {
                    result.Add((list[i] - previous) / Math.Abs(previous));
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates the z-score for each value in the collection.
        /// </summary>
        public static IEnumerable<double> ZScores(this IEnumerable<double> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            
            var list = source.ToList();
            if (!list.Any())
                return Enumerable.Empty<double>();

            double mean = list.Average();
            double stdDev = list.StandardDeviation();

            if (stdDev < double.Epsilon)
                return list.Select(_ => 0.0);

            return list.Select(x => (x - mean) / stdDev);
        }

        /// <summary>
        /// Batches a collection into chunks of a specified size.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (batchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be at least 1");

            return BatchImpl(source, batchSize);
        }

        private static IEnumerable<IEnumerable<T>> BatchImpl<T>(IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Any())
                yield return batch;
        }
    }
}
