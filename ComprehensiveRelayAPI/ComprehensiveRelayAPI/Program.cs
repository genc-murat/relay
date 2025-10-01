using ComprehensiveRelayAPI.Configuration;
using ComprehensiveRelayAPI.Handlers;
using ComprehensiveRelayAPI.Models;
using ComprehensiveRelayAPI.Requests;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Relay.Core;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

// Create activity source for OpenTelemetry
using var activitySource = new ActivitySource("ComprehensiveRelayAPI");

var builder = WebApplication.CreateBuilder(args);

// Configure comprehensive logging with Serilog
builder.Services.AddComprehensiveLogging(builder.Configuration);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add comprehensive Relay services
builder.Services.AddComprehensiveRelay(builder.Configuration);

// Add API documentation
builder.Services.AddComprehensiveApiDocumentation();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Comprehensive Relay API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

// Add custom middleware
app.UseMiddleware<ApiMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add health checks endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new 
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                data = x.Value.Data,
                duration = x.Value.Duration.ToString()
            }),
            timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
    }
});

// ==================== RELAY MINIMAL API ENDPOINTS ====================

var relay = app.Services.GetRequiredService<IRelay>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Root endpoint
app.MapGet("/", () => new 
{
    message = "🚀 Comprehensive Relay API",
    version = "1.0.0",
    features = new[]
    {
        "✅ Request/Response handling with Relay",
        "✅ Streaming support",
        "✅ Notification publishing",
        "✅ Pipeline behaviors (Validation, Logging, Caching, Exception Handling)",
        "✅ Performance monitoring",
        "✅ Health checks",
        "✅ OpenTelemetry tracing",
        "✅ FluentValidation integration",
        "✅ Memory caching",
        "✅ Comprehensive logging with Serilog"
    },
    endpoints = new
    {
        users = "/api/users",
        products = "/api/products", 
        orders = "/api/orders",
        health = "/health",
        swagger = "/swagger"
    }
})
.WithName("GetApiInfo")
.WithTags("Info");

// ==================== USER ENDPOINTS ====================

app.MapGet("/api/users/{userId:int}", async (int userId, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("GetUser");
    activity?.SetTag("user.id", userId);
    
    var query = new GetUserQuery(userId);
    var user = await relay.SendAsync(query, ct);
    
    return user != null ? Results.Ok(new ApiResponse<User> 
    { 
        Success = true, 
        Data = user, 
        Message = "User retrieved successfully" 
    }) : Results.NotFound(new ApiResponse<User> 
    { 
        Success = false, 
        Message = "User not found" 
    });
})
.WithName("GetUser")
.WithTags("Users");

app.MapGet("/api/users", async (int pageNumber = 1, int pageSize = 10, string? searchTerm = null, bool? isActive = null, CancellationToken ct = default) =>
{
    using var activity = activitySource.StartActivity("GetUsers");
    activity?.SetTag("page.number", pageNumber);
    activity?.SetTag("page.size", pageSize);
    
    var query = new GetUsersQuery(pageNumber, pageSize, searchTerm, isActive);
    var users = await relay.SendAsync(query, ct);
    
    return Results.Ok(new ApiResponse<PagedResponse<User>> 
    { 
        Success = true, 
        Data = users, 
        Message = "Users retrieved successfully" 
    });
})
.WithName("GetUsers")
.WithTags("Users");

app.MapPost("/api/users", async (CreateUserCommand command, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("CreateUser");
    activity?.SetTag("user.name", command.Name);
    
    try
    {
        var user = await relay.SendAsync(command, ct);
        
        // Publish notification
        var notification = new UserCreatedNotification(user.Id, user.Name, user.Email);
        await relay.PublishAsync(notification, ct);
        
        return Results.Created($"/api/users/{user.Id}", new ApiResponse<User> 
        { 
            Success = true, 
            Data = user, 
            Message = "User created successfully" 
        });
    }
    catch (FluentValidation.ValidationException ex)
    {
        return Results.BadRequest(new ApiResponse<User> 
        { 
            Success = false, 
            Message = "Validation failed", 
            Errors = ex.Errors.Select(e => e.ErrorMessage).ToList() 
        });
    }
})
.WithName("CreateUser")
.WithTags("Users");

app.MapPut("/api/users/{userId:int}", async (int userId, UpdateUserCommand command, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("UpdateUser");
    activity?.SetTag("user.id", userId);
    
    var updateCommand = command with { UserId = userId };
    var user = await relay.SendAsync(updateCommand, ct);
    
    return user != null ? Results.Ok(new ApiResponse<User> 
    { 
        Success = true, 
        Data = user, 
        Message = "User updated successfully" 
    }) : Results.NotFound(new ApiResponse<User> 
    { 
        Success = false, 
        Message = "User not found" 
    });
})
.WithName("UpdateUser")
.WithTags("Users");

app.MapDelete("/api/users/{userId:int}", async (int userId, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("DeleteUser");
    activity?.SetTag("user.id", userId);
    
    var command = new DeleteUserCommand(userId);
    var result = await relay.SendAsync(command, ct);
    
    return result ? Results.Ok(new ApiResponse<bool> 
    { 
        Success = true, 
        Data = true, 
        Message = "User deleted successfully" 
    }) : Results.NotFound(new ApiResponse<bool> 
    { 
        Success = false, 
        Message = "User not found" 
    });
})
.WithName("DeleteUser")
.WithTags("Users");

// ==================== PRODUCT ENDPOINTS ====================

app.MapGet("/api/products/{productId:int}", async (int productId, CancellationToken ct) =>
{
    var query = new GetProductQuery(productId);
    var product = await relay.SendAsync(query, ct);
    
    return product != null ? Results.Ok(new ApiResponse<Product> 
    { 
        Success = true, 
        Data = product, 
        Message = "Product retrieved successfully" 
    }) : Results.NotFound(new ApiResponse<Product> 
    { 
        Success = false, 
        Message = "Product not found" 
    });
})
.WithName("GetProduct")
.WithTags("Products");

app.MapGet("/api/products", async (int pageNumber = 1, int pageSize = 10, string? category = null, decimal? minPrice = null, decimal? maxPrice = null, bool? isActive = null, CancellationToken ct = default) =>
{
    var query = new GetProductsQuery(pageNumber, pageSize, category, minPrice, maxPrice, isActive);
    var products = await relay.SendAsync(query, ct);
    
    return Results.Ok(new ApiResponse<PagedResponse<Product>> 
    { 
        Success = true, 
        Data = products, 
        Message = "Products retrieved successfully" 
    });
})
.WithName("GetProducts")
.WithTags("Products");

app.MapPost("/api/products", async (CreateProductCommand command, CancellationToken ct) =>
{
    try
    {
        var product = await relay.SendAsync(command, ct);
        
        return Results.Created($"/api/products/{product.Id}", new ApiResponse<Product> 
        { 
            Success = true, 
            Data = product, 
            Message = "Product created successfully" 
        });
    }
    catch (FluentValidation.ValidationException ex)
    {
        return Results.BadRequest(new ApiResponse<Product> 
        { 
            Success = false, 
            Message = "Validation failed", 
            Errors = ex.Errors.Select(e => e.ErrorMessage).ToList() 
        });
    }
})
.WithName("CreateProduct")
.WithTags("Products");

// ==================== ORDER ENDPOINTS ====================

app.MapGet("/api/orders/{orderId:int}", async (int orderId, CancellationToken ct) =>
{
    var query = new GetOrderQuery(orderId);
    var order = await relay.SendAsync(query, ct);
    
    return order != null ? Results.Ok(new ApiResponse<Order> 
    { 
        Success = true, 
        Data = order, 
        Message = "Order retrieved successfully" 
    }) : Results.NotFound(new ApiResponse<Order> 
    { 
        Success = false, 
        Message = "Order not found" 
    });
})
.WithName("GetOrder")
.WithTags("Orders");

app.MapPost("/api/orders", async (CreateOrderCommand command, CancellationToken ct) =>
{
    try
    {
        var order = await relay.SendAsync(command, ct);
        
        // Publish order created notification
        var notification = new OrderCreatedNotification(order.Id, order.UserId, order.TotalAmount);
        await relay.PublishAsync(notification, ct);
        
        return Results.Created($"/api/orders/{order.Id}", new ApiResponse<Order> 
        { 
            Success = true, 
            Data = order, 
            Message = "Order created successfully" 
        });
    }
    catch (FluentValidation.ValidationException ex)
    {
        return Results.BadRequest(new ApiResponse<Order> 
        { 
            Success = false, 
            Message = "Validation failed", 
            Errors = ex.Errors.Select(e => e.ErrorMessage).ToList() 
        });
    }
})
.WithName("CreateOrder")
.WithTags("Orders");

// ==================== PERFORMANCE TEST ENDPOINT ====================

app.MapGet("/api/performance-test", async (int iterations = 1000, CancellationToken ct = default) =>
{
    logger.LogInformation("🚀 Starting performance test with {Iterations} iterations", iterations);
    
    var stopwatch = Stopwatch.StartNew();
    var results = new List<long>();
    
    for (int i = 0; i < iterations; i++)
    {
        var iterationStopwatch = Stopwatch.StartNew();
        
        var query = new GetUserQuery(1);
        await relay.SendAsync(query, ct);
        
        iterationStopwatch.Stop();
        results.Add(iterationStopwatch.ElapsedMilliseconds);
        
        if (ct.IsCancellationRequested)
            break;
    }
    
    stopwatch.Stop();
    
    var stats = new
    {
        TotalIterations = results.Count,
        TotalTimeMs = stopwatch.ElapsedMilliseconds,
        AverageTimeMs = results.Average(),
        MinTimeMs = results.Min(),
        MaxTimeMs = results.Max(),
        RequestsPerSecond = results.Count * 1000.0 / stopwatch.ElapsedMilliseconds,
        P95TimeMs = results.OrderBy(x => x).Skip((int)(results.Count * 0.95)).FirstOrDefault(),
        P99TimeMs = results.OrderBy(x => x).Skip((int)(results.Count * 0.99)).FirstOrDefault()
    };
    
    logger.LogInformation("✅ Performance test completed: {Stats}", JsonSerializer.Serialize(stats));
    
    return Results.Ok(new ApiResponse<object>
    {
        Success = true,
        Data = stats,
        Message = "Performance test completed successfully"
    });
})
.WithName("PerformanceTest")
.WithTags("Performance");

// ==================== START APPLICATION ====================

logger.LogInformation("🚀 Starting Comprehensive Relay API");
logger.LogInformation("📚 Swagger UI available at: {SwaggerUrl}", app.Environment.IsDevelopment() ? "https://localhost:7108" : "");
logger.LogInformation("💚 Health checks available at: /health");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
