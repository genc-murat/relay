using AwsSqsSnsMessageBrokerSample.Events;
using AwsSqsSnsMessageBrokerSample.Handlers;
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Relay MessageBroker with AWS SQS/SNS
builder.Services.AddMessageBroker(mb =>
{
    var config = builder.Configuration.GetSection("MessageBroker:AwsSqsSns");
    var useLocalStack = config.GetValue<bool>("UseLocalStack");

    mb.UseAwsSqsSns(options =>
    {
        options.Region = config["Region"] ?? "us-east-1";
        options.QueueUrl = config["QueueUrl"] ?? "";
        options.TopicArn = config["TopicArn"] ?? "";

        if (useLocalStack)
        {
            options.ServiceUrl = config["LocalStackUrl"] ?? "http://localhost:4566";
            options.AccessKey = "test";
            options.SecretKey = "test";
        }
    });
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
