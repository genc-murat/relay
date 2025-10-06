namespace Relay.Core.AI.Optimization.Data
{
    /// <summary>
    /// Metrics for threshold optimization using ROC curve analysis
    /// </summary>
    internal class ThresholdMetrics
    {
        public double Threshold { get; set; }
        public int TruePositives { get; set; }
        public int FalsePositives { get; set; }
        public int TrueNegatives { get; set; }
        public int FalseNegatives { get; set; }
        public double Sensitivity { get; set; } // TPR / Recall
        public double Specificity { get; set; } // TNR
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double Accuracy { get; set; }
    }
}
