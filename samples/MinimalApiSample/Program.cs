using Relay.MessageBroker;
using Relay.MessageBroker.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Message Broker with Development Profile
builder.Services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.Development,
    options =>
    {
        options.BrokerType = MessageBrokerType.RabbitMQ;
        options.RabbitMQ = new RabbitMQOptions
        {
            HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest",
            Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = "/"
        };
    });

// Add Message Broker Hosted Service
builder.Services.AddMessageBrokerHostedService();

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Health check endpoint
app.MapHealthChecks("/health");

// API Endpoints
var messages = app.MapGroup("/api/messages")
    .WithTags("Messages");

// Publish a message
messages.MapPost("/publish", async (PublishMessageRequest request, IMessageBroker broker) =>
{
    try
    {
        await broker.PublishAsync(
            request.Message,
            new PublishOptions
            {
                Exchange = request.Exchange,
                RoutingKey = request.RoutingKey
            });

        return Results.Ok(new { success = true, message = "Message published successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Failed to publish message");
    }
})
.WithName("PublishMessage")
.WithSummary("Publish a message to the broker");

// Subscribe to messages
messages.MapPost("/subscribe", async (SubscribeRequest request, IMessageBroker broker, ILogger<Program> logger) =>
{
    try
    {
        await broker.SubscribeAsync<string>(
            async (message, context, ct) =>
            {
                logger.LogInformation("Received message: {Message} from {Exchange}/{RoutingKey}", 
                    message, context.Exchange, context.RoutingKey);
                await Task.CompletedTask;
            },
            new SubscriptionOptions
            {
                Exchange = request.Exchange,
                RoutingKey = request.RoutingKey
            });

        return Results.Ok(new { success = true, message = "Subscribed successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Failed to subscribe");
    }
})
.WithName("Subscribe")
.WithSummary("Subscribe to messages from the broker");

// Get broker info
messages.MapGet("/info", () =>
{
    return Results.Ok(new
    {
        brokerType = "RabbitMQ",
        profile = "Development",
        features = new[]
        {
            "Connection Pooling",
            "Health Checks",
            "Metrics"
        }
    });
})
.WithName("GetBrokerInfo")
.WithSummary("Get message broker information");

app.Run();

// Request/Response models
record PublishMessageRequest(string Exchange, string RoutingKey, string Message);
record SubscribeRequest(string Exchange, string RoutingKey);
