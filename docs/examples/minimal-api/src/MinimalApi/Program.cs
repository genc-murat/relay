using MinimalApi.Endpoints;
using MinimalApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Relay
builder.Services.AddSingleton<Relay.Core.IRelay, Relay.Core.RelayImplementation>();

// Register application services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserNotificationHandlers>();

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

// Map endpoints
app.MapUserEndpoints();

app.Run();