using AwsSqsSnsMessageBrokerSample.Events;
using AwsSqsSnsMessageBrokerSample.Handlers;
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Relay MessageBroker with AWS SQS/SNS
builder.Services.AddAwsSqsSns(options =>
{
    var config = builder.Configuration.GetSection("MessageBroker:AwsSqsSns");
    var useLocalStack = config.GetValue<bool>("UseLocalStack");

    options.Region = config["Region"] ?? "us-east-1";
    options.DefaultQueueUrl = config["QueueUrl"] ?? "";
    options.DefaultTopicArn = config["TopicArn"] ?? "";
    options.MaxNumberOfMessages = config.GetValue<int>("MaxNumberOfMessages", 10);
    options.WaitTimeSeconds = TimeSpan.FromSeconds(config.GetValue<int>("WaitTimeSeconds", 20));
    options.AutoDeleteMessages = config.GetValue<bool>("AutoDeleteMessages", true);
    options.VisibilityTimeout = TimeSpan.FromSeconds(config.GetValue<int>("VisibilityTimeout", 30));

    // FIFO queue support
    options.UseFifoQueue = config.GetValue<bool>("UseFifoQueue", false);
    options.MessageGroupId = config["MessageGroupId"];
    options.MessageDeduplicationId = config["MessageDeduplicationId"];

    if (useLocalStack)
    {
        options.AccessKeyId = "test";
        options.SecretAccessKey = "test";
    }
});

// Add event handlers
builder.Services.AddSingleton<OrderEventHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Subscribe to events
var messageBroker = app.Services.GetRequiredService<IMessageBroker>();
var orderHandler = app.Services.GetRequiredService<OrderEventHandler>();

await messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) => await orderHandler.HandleOrderCreatedAsync(message, ct));

await messageBroker.SubscribeAsync<OrderProcessedEvent>(
    async (message, context, ct) => await orderHandler.HandleOrderProcessedAsync(message, ct));

app.Logger.LogInformation("AWS SQS/SNS Message Broker Sample started");
app.Logger.LogInformation("Subscribed to OrderCreatedEvent and OrderProcessedEvent");

app.Run();