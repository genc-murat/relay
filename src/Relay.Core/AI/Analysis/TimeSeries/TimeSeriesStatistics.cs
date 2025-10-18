using System;
using System.Linq;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Static utility class for time-series statistical calculations.
    /// </summary>
    internal static class TimeSeriesStatistics
    {
        /// <summary>
        /// Calculates the standard deviation of a set of values.
        /// </summary>
        public static double CalculateStdDev(float[] values)
        {
            var avg = values.Average();
            var sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumOfSquares / values.Length);
        }

        /// <summary>
        /// Calculates the median of a set of values.
        /// </summary>
        public static float CalculateMedian(float[] values)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            var mid = sorted.Length / 2;
            return sorted.Length % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
        }

        /// <summary>
        /// Calculates the percentile of a set of values.
        /// </summary>
        public static float CalculatePercentile(float[] values, double percentile)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
            return sorted[Math.Max(0, Math.Min(index, sorted.Length - 1))];
        }
    }
}