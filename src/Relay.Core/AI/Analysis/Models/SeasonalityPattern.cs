namespace Relay.Core.AI
{
    public class SeasonalityPattern
    {
        public string HourlyPattern { get; set; } = string.Empty;
        public string DailyPattern { get; set; } = string.Empty;
        public double ExpectedMultiplier { get; set; }
        public bool MatchesSeasonality { get; set; }
    }
}
