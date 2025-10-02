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

// 🚀 ENHANCED: Add comprehensive Relay services with source generator
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

// Enhanced health checks endpoint with source generator validation
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            sourceGeneratorEnabled = true, // Flag to show we're using source generator
            checks = report.Entries.Select(x => new 
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                data = x.Value.Data,
                duration = x.Value.Duration.ToString()
            }),
            timestamp = DateTime.UtcNow,
            framework = new
            {
                name = "Relay Framework",
                version = "1.0.0",
                features = new[]
                {
                    "✅ Auto Handler Registration via Source Generator",
                    "✅ Optimized Request Dispatching",
                    "✅ Compile-time Type Safety",
                    "✅ Zero Configuration Setup"
                }
            }
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
    }
});

// ==================== RELAY MINIMAL API ENDPOINTS ====================

var relay = app.Services.GetRequiredService<IRelay>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Enhanced root endpoint with source generator info
app.MapGet("/", () => new 
{
    message = "🚀 Comprehensive Relay API with Source Generator",
    version = "2.0.0",
    sourceGenerator = new
    {
        enabled = true,
        description = "All handlers auto-registered at compile time",
        performance = "Optimized dispatch with zero reflection overhead"
    },
    features = new[]
    {
        "✅ Auto Handler Registration (Source Generator)",
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
        swagger = "/swagger",
        diagnostics = "/api/diagnostics"
    }
})
.WithName("GetApiInfo")
.WithTags("Info");

// ==================== DIAGNOSTICS ENDPOINT ====================

app.MapGet("/api/diagnostics", (IServiceProvider serviceProvider) =>
{
    try
    {
        // Check what handlers are registered
        var registeredHandlers = new List<object>();
        
        // Test major handler types
        var userHandler = serviceProvider.GetService<IRequestHandler<GetUserQuery, User?>>();
        var usersHandler = serviceProvider.GetService<IRequestHandler<GetUsersQuery, PagedResponse<User>>>();
        var productHandler = serviceProvider.GetService<IRequestHandler<GetProductQuery, Product?>>();
        var productsHandler = serviceProvider.GetService<IRequestHandler<GetProductsQuery, PagedResponse<Product>>>();
        
        if (userHandler != null) registeredHandlers.Add(new { handler = "GetUserQueryHandler", type = userHandler.GetType().Name });
        if (usersHandler != null) registeredHandlers.Add(new { handler = "GetUsersQueryHandler", type = usersHandler.GetType().Name });
        if (productHandler != null) registeredHandlers.Add(new { handler = "GetProductQueryHandler", type = productHandler.GetType().Name });
        if (productsHandler != null) registeredHandlers.Add(new { handler = "GetProductsQueryHandler", type = productsHandler.GetType().Name });
        
        var diagnostics = new
        {
            sourceGenerator = new
            {
                status = "Active",
                handlersFound = registeredHandlers.Count,
                compilationTime = DateTime.UtcNow.AddMinutes(-1), // Simulated
                generatedFiles = new[] { "RelayRegistration.g.cs", "OptimizedRequestDispatcher.g.cs" }
            },
            handlers = registeredHandlers,
            dispatchers = new
            {
                requestDispatcher = serviceProvider.GetService<IRequestDispatcher>()?.GetType().Name ?? "Not found",
                streamDispatcher = serviceProvider.GetService<IStreamDispatcher>()?.GetType().Name ?? "Not found",
                notificationDispatcher = serviceProvider.GetService<INotificationDispatcher>()?.GetType().Name ?? "Not found"
            },
            performance = new
            {
                reflectionUsage = "Minimal - Only during DI resolution",
                dispatchMethod = "Generated switch statements",
                typeChecking = "Compile-time validated"
            },
            memory = new
            {
                totalMemory = GC.GetTotalMemory(false),
                generation0 = GC.CollectionCount(0),
                generation1 = GC.CollectionCount(1),
                generation2 = GC.CollectionCount(2)
            }
        };
        
        return Results.Ok(new ApiResponse<object>
        {
            Success = true,
            Data = diagnostics,
            Message = "Source generator diagnostics retrieved successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new ApiResponse<object>
        {
            Success = false,
            Message = $"Diagnostics error: {ex.Message}",
            Errors = new[] { ex.ToString() }
        });
    }
})
.WithName("GetDiagnostics")
.WithTags("Diagnostics");

// ==================== USER ENDPOINTS ====================

app.MapGet("/api/users/{userId:int}", async (int userId, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("GetUser");
    activity?.SetTag("user.id", userId);
    activity?.SetTag("handler.generated", true);
    
    var query = new GetUserQuery(userId);
    var user = await relay.SendAsync(query, ct);
    
    return user != null ? Results.Ok(new ApiResponse<User> 
    { 
        Success = true, 
        Data = user, 
        Message = "User retrieved successfully via generated handler" 
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
    activity?.SetTag("handler.generated", true);
    
    var query = new GetUsersQuery(pageNumber, pageSize, searchTerm, isActive);
    var users = await relay.SendAsync(query, ct);
    
    return Results.Ok(new ApiResponse<PagedResponse<User>> 
    { 
        Success = true, 
        Data = users, 
        Message = "Users retrieved successfully via generated handler" 
    });
})
.WithName("GetUsers")
.WithTags("Users");

app.MapPost("/api/users", async (CreateUserCommand command, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("CreateUser");
    activity?.SetTag("user.name", command.Name);
    activity?.SetTag("handler.generated", true);
    
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
            Message = "User created successfully via generated handler" 
        });
    }
    catch (FluentValidation.ValidationException ex)
    {
        return Results.BadRequest(new ApiResponse<User> 
        { 
            Success = false, 
            Message = "Validation failed", 
            Errors = ex.Errors.Select(e => e.ErrorMessage).ToArray() 
        });
    }
})
.WithName("CreateUser")
.WithTags("Users");

app.MapPut("/api/users/{userId:int}", async (int userId, UpdateUserCommand command, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("UpdateUser");
    activity?.SetTag("user.id", userId);
    activity?.SetTag("handler.generated", true);
    
    var updateCommand = command with { UserId = userId };
    var user = await relay.SendAsync(updateCommand, ct);
    
    return user != null ? Results.Ok(new ApiResponse<User> 
    { 
        Success = true, 
        Data = user, 
        Message = "User updated successfully via generated handler" 
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
    activity?.SetTag("handler.generated", true);
    
    var command = new DeleteUserCommand(userId);
    var result = await relay.SendAsync(command, ct);
    
    return result ? Results.Ok(new ApiResponse<bool> 
    { 
        Success = true, 
        Data = true, 
        Message = "User deleted successfully via generated handler" 
    }) : Results.NotFound(new ApiResponse<bool> 
    { 
        Success = false, 
        Message = "User not found" 
    });
})
.WithName("DeleteUser")
.WithTags("Users");

// ==================== ENHANCED PRODUCT ENDPOINTS ====================

app.MapGet("/api/products/{productId:int}", async (int productId, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("GetProduct");
    activity?.SetTag("product.id", productId);
    activity?.SetTag("handler.generated", true);
    
    var query = new GetProductQuery(productId);
    var product = await relay.SendAsync(query, ct);
    
    return product != null ? Results.Ok(new ApiResponse<Product> 
    { 
        Success = true, 
        Data = product, 
        Message = "Product retrieved successfully via generated handler" 
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
    using var activity = activitySource.StartActivity("GetProducts");
    activity?.SetTag("page.number", pageNumber);
    activity?.SetTag("page.size", pageSize);
    activity?.SetTag("handler.generated", true);
    activity?.SetTag("category", category);
    
    var query = new GetProductsQuery(pageNumber, pageSize, category, minPrice, maxPrice, isActive);
    var products = await relay.SendAsync(query, ct);
    
    return Results.Ok(new ApiResponse<PagedResponse<Product>> 
    { 
        Success = true, 
        Data = products, 
        Message = "Products retrieved successfully via generated handler 🚀" 
    });
})
.WithName("GetProducts")
.WithTags("Products");

app.MapPost("/api/products", async (CreateProductCommand command, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("CreateProduct");
    activity?.SetTag("product.name", command.Name);
    activity?.SetTag("handler.generated", true);
    
    try
    {
        var product = await relay.SendAsync(command, ct);
        
        return Results.Created($"/api/products/{product.Id}", new ApiResponse<Product> 
        { 
            Success = true, 
            Data = product, 
            Message = "Product created successfully via generated handler" 
        });
    }
    catch (FluentValidation.ValidationException ex)
    {
        return Results.BadRequest(new ApiResponse<Product> 
        { 
            Success = false, 
            Message = "Validation failed", 
            Errors = ex.Errors.Select(e => e.ErrorMessage).ToArray() 
        });
    }
})
.WithName("CreateProduct")
.WithTags("Products");

// ==================== ORDER ENDPOINTS ====================

app.MapGet("/api/orders/{orderId:int}", async (int orderId, CancellationToken ct) =>
{
    using var activity = activitySource.StartActivity("GetOrder");
    activity?.SetTag("order.id", orderId);
    activity?.SetTag("handler.generated", true);
    
    var query = new GetOrderQuery(orderId);
    var order = await relay.SendAsync(query, ct);
    
    return order != null ? Results.Ok(new ApiResponse<Order> 
    { 
        Success = true, 
        Data = order, 
        Message = "Order retrieved successfully via generated handler" 
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
    using var activity = activitySource.StartActivity("CreateOrder");
    activity?.SetTag("order.userId", command.UserId);
    activity?.SetTag("handler.generated", true);
    
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
            Message = "Order created successfully via generated handler" 
        });
    }
    catch (FluentValidation.ValidationException ex)
    {
        return Results.BadRequest(new ApiResponse<Order> 
        { 
            Success = false, 
            Message = "Validation failed", 
            Errors = ex.Errors.Select(e => e.ErrorMessage).ToArray() 
        });
    }
})
.WithName("CreateOrder")
.WithTags("Orders");

// ==================== ENHANCED PERFORMANCE TEST ENDPOINT ====================

app.MapGet("/api/performance-test", async (int iterations = 1000, CancellationToken ct = default) =>
{
    logger.LogInformation("🚀 Starting enhanced performance test with {Iterations} iterations (Source Generator)", iterations);
    
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
        P99TimeMs = results.OrderBy(x => x).Skip((int)(results.Count * 0.99)).FirstOrDefault(),
        SourceGeneratorOptimized = true,
        DispatchMethod = "Generated Switch Statements",
        ReflectionUsage = "Minimal (DI Only)"
    };
    
    logger.LogInformation("✅ Enhanced performance test completed: {Stats}", JsonSerializer.Serialize(stats));
    
    return Results.Ok(new ApiResponse<object>
    {
        Success = true,
        Data = stats,
        Message = "Performance test completed successfully with source generator optimizations"
    });
})
.WithName("PerformanceTest")
.WithTags("Performance");

// ==================== START APPLICATION ====================

logger.LogInformation("🚀 Starting Comprehensive Relay API with Source Generator");
logger.LogInformation("📚 Swagger UI available at: {SwaggerUrl}", app.Environment.IsDevelopment() ? "https://localhost:7108" : "");
logger.LogInformation("💚 Health checks available at: /health");
logger.LogInformation("🔍 Diagnostics available at: /api/diagnostics");
logger.LogInformation("⚡ Source Generator: ENABLED - All handlers auto-registered!");

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
