using ComprehensiveRelayAPI.Models;
using ComprehensiveRelayAPI.Pipeline;
using ComprehensiveRelayAPI.Requests;
using ComprehensiveRelayAPI.Services;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Relay;
using Relay.Core;
using Serilog;
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace ComprehensiveRelayAPI.Configuration;

/// <summary>
/// Extension methods for configuring Relay services
/// </summary>
public static class RelayConfiguration
{
    /// <summary>
    /// Configure all Relay services with auto-generated handler registration
    /// </summary>
    public static IServiceCollection AddComprehensiveRelay(this IServiceCollection services, IConfiguration configuration)
    {
        // 🚀 SINGLE LINE SETUP - All handlers auto-registered by source generator!
        services.AddRelay();
        
        // Add memory cache for caching pipeline
        services.AddMemoryCache();
        
        // Add our application services
        services.AddSingleton<DataService>();
        
        // Add pipeline behaviors (these are not auto-discovered yet, need manual registration)
        services.AddScoped<ValidationPipeline>();
        services.AddScoped<LoggingPipeline>();
        services.AddScoped<CachingPipeline>();
        services.AddScoped<ExceptionHandlingPipeline>();
        services.AddScoped<PerformanceMonitoringPipeline>();
        
        // Add FluentValidation validators
        services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();
        
        // Configure health checks with enhanced Relay monitoring
        services.AddHealthChecks()
            .AddCheck<RelayHealthCheck>("relay")
            .AddCheck("memory", () => 
            {
                var memory = GC.GetTotalMemory(false);
                return memory < 100_000_000 ? 
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory usage: {memory:N0} bytes") :
                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High memory usage: {memory:N0} bytes");
            })
            .AddCheck("handlers", () =>
            {
                // Check if source generator worked correctly
                var serviceProvider = services.BuildServiceProvider();
                var userHandler = serviceProvider.GetService<IRequestHandler<GetUserQuery, User?>>();
                var productHandler = serviceProvider.GetService<IRequestHandler<GetProductsQuery, PagedResponse<Product>>>();
                
                if (userHandler == null || productHandler == null)
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                        "Source generator failed to register handlers");
                }
                
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "All handlers registered successfully by source generator");
            });
        
        // Configure OpenTelemetry for enhanced observability
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddSource("ComprehensiveRelayAPI")
                       .AddSource("Relay.Generated"); // Add generated dispatcher tracing
            });
        
        return services;
    }
    
    /// <summary>
    /// Configure comprehensive logging with Serilog
    /// </summary>
    public static IServiceCollection AddComprehensiveLogging(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "ComprehensiveRelayAPI")
            .Enrich.WithProperty("Version", typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/app-.log", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        
        services.AddSerilog();
        
        return services;
    }
    
    /// <summary>
    /// Configure comprehensive API documentation
    /// </summary>
    public static IServiceCollection AddComprehensiveApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Comprehensive Relay API",
                Version = "v1",
                Description = "A comprehensive demonstration of Relay framework features including caching, validation, notifications, streaming, and performance monitoring.",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Relay Team",
                    Email = "info@relay.dev",
                    Url = new Uri("https://github.com/genc-murat/relay")
                }
            });
            
            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
        
        return services;
    }
}

/// <summary>
/// Health check for Relay framework
/// </summary>
public class RelayHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IRelay _relay;
    private readonly ILogger<RelayHealthCheck> _logger;

    public RelayHealthCheck(IRelay relay, ILogger<RelayHealthCheck> logger)
    {
        _relay = relay;
        _logger = logger;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test a simple request to ensure Relay is working
            var testQuery = new GetUserQuery(1);
            var stopwatch = Stopwatch.StartNew();
            
            var result = await _relay.SendAsync(testQuery, cancellationToken);
            
            stopwatch.Stop();
            
            var data = new Dictionary<string, object>
            {
                ["ResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms",
                ["TestQuery"] = testQuery.GetType().Name,
                ["ResultFound"] = result != null
            };
            
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                    "Relay is responding slowly", data: data);
            }
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                "Relay is working correctly", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Relay health check failed", ex);
        }
    }
}

/// <summary>
/// Configuration options for the comprehensive API
/// </summary>
public class ComprehensiveApiOptions
{
    public const string SectionName = "ComprehensiveApi";
    
    /// <summary>
    /// Enable performance monitoring
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
    
    /// <summary>
    /// Enable response caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>
    /// Default cache duration in minutes
    /// </summary>
    public int DefaultCacheDurationMinutes { get; set; } = 5;
    
    /// <summary>
    /// Enable request validation
    /// </summary>
    public bool EnableValidation { get; set; } = true;
    
    /// <summary>
    /// Enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;
    
    /// <summary>
    /// Slow request threshold in milliseconds
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;
    
    /// <summary>
    /// Enable OpenTelemetry tracing
    /// </summary>
    public bool EnableTracing { get; set; } = true;
}

/// <summary>
/// Middleware for adding custom headers and CORS
/// </summary>
public class ApiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiMiddleware> _logger;

    public ApiMiddleware(RequestDelegate next, ILogger<ApiMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add custom headers
        context.Response.Headers["X-API-Version"] = "1.0";
        context.Response.Headers["X-Powered-By"] = "Relay Framework";
        
        // Log request
        _logger.LogDebug("Processing request: {Method} {Path}", 
            context.Request.Method, context.Request.Path);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            _logger.LogDebug("Completed request: {Method} {Path} in {ElapsedMs}ms with status {StatusCode}",
                context.Request.Method, 
                context.Request.Path, 
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode);
        }
    }
}