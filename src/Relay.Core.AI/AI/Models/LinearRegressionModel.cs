using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Models
{
    /// <summary>
    /// Simple linear regression model for throughput prediction
    /// </summary>
    internal class LinearRegressionModel
    {
        private double _slope;
        private double _intercept;
        private bool _trained;

        public void Train(IEnumerable<(double x, double y)> data)
        {
            var dataList = data.ToList();
            if (dataList.Count < 2)
                throw new ArgumentException("Need at least 2 data points for training");

            var n = dataList.Count;
            var sumX = dataList.Sum(p => p.x);
            var sumY = dataList.Sum(p => p.y);
            var sumXY = dataList.Sum(p => p.x * p.y);
            var sumX2 = dataList.Sum(p => p.x * p.x);

            _slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            _intercept = (sumY - _slope * sumX) / n;
            _trained = true;
        }

        public double Predict(double x)
        {
            if (!_trained)
                throw new InvalidOperationException("Model must be trained before prediction");

            return _slope * x + _intercept;
        }

        public bool IsTrained => _trained;
    }
}