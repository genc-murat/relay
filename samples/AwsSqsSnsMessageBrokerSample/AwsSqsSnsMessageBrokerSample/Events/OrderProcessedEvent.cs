namespace AwsSqsSnsMessageBrokerSample.Events;

public class OrderProcessedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
