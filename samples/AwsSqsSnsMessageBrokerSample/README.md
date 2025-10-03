# AWS SQS/SNS Message Broker Sample

This sample demonstrates how to use Relay.MessageBroker with AWS SQS and SNS for event-driven architectures.

## Features Demonstrated

- Publishing events to AWS SNS topics
- Subscribing to events from AWS SQS queues
- Error handling and retry mechanisms
- Message context usage
- Correlation IDs
- Dead letter queues

## Prerequisites

### AWS Account Setup

1. Create an AWS account if you don't have one
2. Install AWS CLI: https://aws.amazon.com/cli/
3. Configure AWS credentials:

```bash
aws configure
```

Enter your:
- AWS Access Key ID
- AWS Secret Access Key
- Default region (e.g., us-east-1)
- Output format (e.g., json)

### LocalStack (for local development)

Alternatively, use LocalStack for local AWS emulation:

```bash
docker run -d -p 4566:4566 -p 4571:4571 localstack/localstack
```

## Configuration

Update `appsettings.json` with your AWS settings:

```json
{
  "MessageBroker": {
    "Provider": "AwsSqsSns",
    "AwsSqsSns": {
      "Region": "us-east-1",
      "QueueUrl": "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue",
      "TopicArn": "arn:aws:sns:us-east-1:123456789012:my-topic",
      "UseLocalStack": false,
      "LocalStackUrl": "http://localhost:4566"
    }
  }
}
```

## Creating AWS Resources

### Create SNS Topic

```bash
aws sns create-topic --name order-events
```

### Create SQS Queue

```bash
aws sqs create-queue --queue-name order-processing-queue
```

### Subscribe Queue to Topic

```bash
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:123456789012:order-events \
  --protocol sqs \
  --notification-endpoint arn:aws:sqs:us-east-1:123456789012:order-processing-queue
```

### Set Queue Policy

The SQS queue needs permission to receive messages from SNS. Create a policy file `queue-policy.json`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": "*",
      "Action": "sqs:SendMessage",
      "Resource": "arn:aws:sqs:us-east-1:123456789012:order-processing-queue",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "arn:aws:sns:us-east-1:123456789012:order-events"
        }
      }
    }
  ]
}
```

Apply the policy:

```bash
aws sqs set-queue-attributes \
  --queue-url https://sqs.us-east-1.amazonaws.com/123456789012/order-processing-queue \
  --attributes file://queue-policy.json
```

## Running the Sample

```bash
dotnet run
```

## Testing the API

### Create an Order

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "CUST-001", "amount": 99.99, "items": ["Item1", "Item2"]}'
```

### Check Order Status

```bash
curl http://localhost:5000/api/orders/{orderId}
```

## Sample Output

```
[INFO] AWS SQS/SNS Message Broker initialized
[INFO] Subscribed to OrderCreatedEvent
[INFO] Publishing order created event for order ORD-12345
[INFO] Event published to SNS topic: arn:aws:sns:us-east-1:123456789012:order-events
[INFO] Received OrderCreatedEvent for order ORD-12345
[INFO] Processing order ORD-12345...
[INFO] Order processed successfully
```

## Code Structure

- `Program.cs` - Application setup and AWS configuration
- `Events/` - Event definitions (OrderCreatedEvent, OrderProcessedEvent)
- `Handlers/` - Event handlers (OrderEventHandler)
- `Controllers/` - API controllers (OrdersController)
- `Services/` - Business services (OrderService)

## Key Concepts

### 1. Event Definition

```csharp
public class OrderCreatedEvent
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public List<string> Items { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2. Publishing to SNS

```csharp
await _messageBroker.PublishAsync(
    new OrderCreatedEvent 
    { 
        OrderId = orderId,
        CustomerId = customerId,
        Amount = amount,
        Items = items
    });
```

### 3. Subscribing from SQS

```csharp
services.AddMessageBroker(builder =>
{
    builder.UseAwsSqsSns(options =>
    {
        options.Region = "us-east-1";
        options.QueueUrl = configuration["MessageBroker:AwsSqsSns:QueueUrl"];
        options.TopicArn = configuration["MessageBroker:AwsSqsSns:TopicArn"];
    });
});
```

## LocalStack Configuration

For local development with LocalStack:

```json
{
  "MessageBroker": {
    "Provider": "AwsSqsSns",
    "AwsSqsSns": {
      "Region": "us-east-1",
      "QueueUrl": "http://localhost:4566/000000000000/order-processing-queue",
      "TopicArn": "arn:aws:sns:us-east-1:000000000000:order-events",
      "UseLocalStack": true,
      "LocalStackUrl": "http://localhost:4566"
    }
  }
}
```

Create resources in LocalStack:

```bash
aws --endpoint-url=http://localhost:4566 sns create-topic --name order-events
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name order-processing-queue
aws --endpoint-url=http://localhost:4566 sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:000000000000:order-events \
  --protocol sqs \
  --notification-endpoint arn:aws:sqs:us-east-1:000000000000:order-processing-queue
```

## Troubleshooting

### AWS Credentials Issues

1. Verify credentials: `aws sts get-caller-identity`
2. Check region: `aws configure get region`
3. Test access: `aws sns list-topics`

### Connection Issues

1. Check internet connectivity
2. Verify firewall settings
3. Confirm AWS service status: https://status.aws.amazon.com/

### LocalStack Issues

1. Ensure LocalStack is running: `docker ps`
2. Check logs: `docker logs localstack`
3. Test endpoint: `curl http://localhost:4566/_localstack/health`

## Cost Considerations

- AWS SQS: $0.40 per million requests (first million free)
- AWS SNS: $0.50 per million requests (first million free)
- Data transfer costs may apply

For testing, use LocalStack to avoid AWS charges.

## Best Practices

1. **Use Dead Letter Queues**: Configure DLQs for handling failed messages
2. **Set Message Retention**: Configure appropriate retention periods
3. **Monitor CloudWatch**: Set up alarms for queue depth and errors
4. **Use IAM Roles**: In production, use IAM roles instead of access keys
5. **Enable Encryption**: Use SSE-SQS or SSE-KMS for sensitive data
6. **Tag Resources**: Use tags for cost tracking and organization

## Additional Resources

- [AWS SQS Documentation](https://docs.aws.amazon.com/sqs/)
- [AWS SNS Documentation](https://docs.aws.amazon.com/sns/)
- [LocalStack Documentation](https://docs.localstack.cloud/)
- [Relay.MessageBroker Documentation](../../docs/MessageBroker.md)
