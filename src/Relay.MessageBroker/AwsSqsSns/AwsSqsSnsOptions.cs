namespace Relay.MessageBroker;

/// <summary>
/// AWS SQS/SNS-specific options.
/// </summary>
public sealed class AwsSqsSnsOptions
{
    /// <summary>
    /// Gets or sets the AWS region.
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Gets or sets the AWS access key ID.
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// Gets or sets the AWS secret access key.
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the default SQS queue URL.
    /// </summary>
    public string? DefaultQueueUrl { get; set; }

    /// <summary>
    /// Gets or sets the default SNS topic ARN.
    /// </summary>
    public string? DefaultTopicArn { get; set; }

    /// <summary>
    /// Gets or sets the SQS visibility timeout.
    /// </summary>
    public TimeSpan VisibilityTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of messages to receive.
    /// </summary>
    public int MaxNumberOfMessages { get; set; } = 10;

    /// <summary>
    /// Gets or sets the wait time for long polling.
    /// </summary>
    public TimeSpan WaitTimeSeconds { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Gets or sets whether to use FIFO queues.
    /// </summary>
    public bool UseFifo { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use FIFO queue.
    /// </summary>
    public bool UseFifoQueue { get; set; } = false;

    /// <summary>
    /// Gets or sets the message group ID for FIFO queues.
    /// </summary>
    public string? MessageGroupId { get; set; }

    /// <summary>
    /// Gets or sets the message deduplication ID for FIFO queues.
    /// </summary>
    public string? MessageDeduplicationId { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-delete messages after processing.
    /// </summary>
    public bool AutoDeleteMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the message retention period.
    /// </summary>
    public TimeSpan MessageRetentionPeriod { get; set; } = TimeSpan.FromDays(4);
}