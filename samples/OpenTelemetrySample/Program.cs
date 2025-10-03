using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Relay.Core;
using OpenTelemetrySample.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "OpenTelemetry Sample API", 
        Version = "v1",
        Description = "Demonstrates OpenTelemetry integration with Relay for distributed tracing and observability"
    });
});

// Add HTTP Client Factory
builder.Services.AddHttpClient("PaymentGateway");

// Add Relay
builder.Services.AddRelay(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
});

// Add Message Broker with telemetry
builder.Services.AddRelayMessageBroker(options =>
{
    options.UseInMemory();
    options.EnableTelemetry = true;
});

// Add business services
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<EmailService>();

// Configure OpenTelemetry
var serviceName = "OpenTelemetry.Sample";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService(
            serviceName: serviceName,
            serviceVersion: serviceVersion);
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Relay.*") // Relay activity sources
            .AddSource("Relay.MessageBroker") // Message Broker telemetry
            .AddSource("OpenTelemetry.Sample") // App activity sources
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                    activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    activity.SetTag("http.request.method", request.Method.ToString());
                };
            })
            .AddConsoleExporter() // Export to console
            //.AddJaegerExporter(options => // Uncomment to export to Jaeger
            //{
            //    options.AgentHost = "localhost";
            //    options.AgentPort = 6831;
            //})
            ;
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Relay.*")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter(); // Prometheus metrics endpoint
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

app.UseHttpsRedirection();
app.MapControllers();

app.Logger.LogInformation("OpenTelemetry Sample API started");
app.Logger.LogInformation("Service: {ServiceName} v{ServiceVersion}", serviceName, serviceVersion);
app.Logger.LogInformation("Swagger UI: /swagger");
app.Logger.LogInformation("Prometheus Metrics: /metrics");
app.Logger.LogInformation("");
app.Logger.LogInformation("Try these endpoints:");
app.Logger.LogInformation("  POST /api/orders - Create order with full tracing");
app.Logger.LogInformation("  GET /api/orders/slow - Slow operation demo");
app.Logger.LogInformation("  GET /api/orders/error - Error tracking demo");
app.Logger.LogInformation("  POST /api/orders/complex - Complex operation with multiple spans");

app.Run();

