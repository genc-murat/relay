namespace Relay.Core.AI.Optimization.Data
{
    /// <summary>
    /// Represents a detected seasonal pattern in time series data
    /// </summary>
    public class SeasonalPattern
    {
        public int Period { get; set; }
        public double Strength { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
