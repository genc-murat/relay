using Relay;
using SimpleCrudApi.Data;
using SimpleCrudApi.Pipelines;
using SimpleCrudApi.Services;
using SimpleCrudApi.MediatR.Handlers;

// Check if we should run benchmarks
if (args.Length > 0 && args[0] == "--benchmark")
{
    SimpleCrudApi.BenchmarkRunner.RunBenchmarks(args.Skip(1).ToArray());
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Relay - basic registration (this would normally be generated)
// For this example, we'll manually register the core services
builder.Services.AddRelay();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatRUserHandlers).Assembly));

// Register application services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserNotificationHandlers>();
builder.Services.AddScoped<ValidationPipeline>();

// Register Relay handlers manually
builder.Services.AddRelayHandlers();

// Register MediatR handlers
builder.Services.AddScoped<MediatRUserHandlers>();
builder.Services.AddScoped<MediatRUserNotificationHandlers>();

// Register repository
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();