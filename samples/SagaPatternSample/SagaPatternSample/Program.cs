using Microsoft.EntityFrameworkCore;
using Relay.Core;
using Relay.MessageBroker.Saga;
using SagaPatternSample.Data;
using SagaPatternSample.Services;
using SagaPatternSample.Sagas;
using SagaPatternSample.Sagas.Steps;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Saga Pattern Sample API", 
        Version = "v1",
        Description = "Demonstrates Relay Saga Pattern for distributed transactions with compensation"
    });
});

// Configure database - Choose one:
// Option 1: In-Memory Database (for demo)
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("SagaDemo"));

// Option 2: SQLite Database (for persistence)
// builder.Services.AddDbContext<OrderDbContext>(options =>
//     options.UseSqlite("Data Source=saga.db"));

// Add Relay services
builder.Services.AddRelay(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
});

// Add Saga services
builder.Services.AddScoped<OrderSaga>();
builder.Services.AddScoped<ReserveInventoryStep>();
builder.Services.AddScoped<ProcessPaymentStep>();
builder.Services.AddScoped<CreateShipmentStep>();

// Add saga persistence (in-memory)
builder.Services.AddSingleton<ISagaPersistence<OrderSagaData>, InMemorySagaPersistence<OrderSagaData>>();

// Add business services
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ShippingService>();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Logger.LogInformation("Saga Pattern Sample API started");
app.Logger.LogInformation("Visit /swagger for API documentation");
app.Logger.LogInformation("Try POST /api/orders/create for success scenario");
app.Logger.LogInformation("Try POST /api/orders/create-inventory-fail for compensation demo");
app.Logger.LogInformation("Try POST /api/orders/create-payment-fail for compensation demo");

app.Run();

