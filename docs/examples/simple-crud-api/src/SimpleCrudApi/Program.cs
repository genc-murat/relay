using SimpleCrudApi.Data;
using SimpleCrudApi.Pipelines;
using SimpleCrudApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Relay - basic registration (this would normally be generated)
// For this example, we'll manually register the core services
builder.Services.AddSingleton<Relay.Core.IRelay, Relay.Core.RelayImplementation>();

// Register application services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserNotificationHandlers>();
builder.Services.AddScoped<ValidationPipeline>();

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