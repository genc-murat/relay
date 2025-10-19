namespace Relay.Core
{
    /// <summary>
    /// Represents the type of message queue provider.
    /// </summary>
    public enum MessageQueueProvider
    {
        /// <summary>
        /// Generic message queue provider.
        /// </summary>
        Generic,

        /// <summary>
        /// RabbitMQ message queue provider.
        /// </summary>
        RabbitMQ,

        /// <summary>
        /// Azure Service Bus message queue provider.
        /// </summary>
        AzureServiceBus,

        /// <summary>
        /// Amazon SQS message queue provider.
        /// </summary>
        AmazonSQS,

        /// <summary>
        /// Apache Kafka message queue provider.
        /// </summary>
        Kafka
    }
}