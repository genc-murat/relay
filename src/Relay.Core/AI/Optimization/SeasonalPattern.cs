namespace Relay.Core.AI
{
    /// <summary>
    /// Represents a detected seasonal pattern in time series data
    /// </summary>
    internal class SeasonalPattern
    {
        public int Period { get; set; }
        public double Strength { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
