namespace Relay.Core.AI
{
    public class ForecastResult
    {
        public double Current { get; set; }
        public double Forecast5Min { get; set; }
        public double Forecast15Min { get; set; }
        public double Forecast60Min { get; set; }
        public double Confidence { get; set; }
    }
}
