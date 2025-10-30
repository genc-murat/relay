using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.MessageBroker;
using Relay.MessageBroker.Security;
using Relay.MessageBroker.RateLimit;
using Relay.MessageBroker.DistributedTracing;
using Relay.MessageBroker.Batch;
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.Deduplication;
using System;
using System.Threading.Tasks;

namespace Relay.MessageBroker.Examples;

/// <summary>
/// Comprehensive example demonstrating multiple MessageBroker enhancements working together:
/// - Message Encryption with Azure Key Vault
/// - Rate Limiting with per-tenant limits
/// - Distributed Tracing with Jaeger
/// - Batch Processing with compression
/// - Connection Pooling
/// - Message Deduplication
/// </summary>
public class ComprehensiveExample
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 1. Configure RabbitMQ
                services.AddRabbitMQ(options =>
                {
                    options.HostName = context.Configuration["RabbitMQ:HostName"] ?? "localhost";
                    options.Port = int.Parse(context.Configuration["RabbitMQ:Port"] ?? "5672");
                    options.UserName = context.Configuration["RabbitMQ:UserName"] ?? "guest";
                    options.Password = context.Configuration["RabbitMQ:Password"] ?? "guest";
                    options.PrefetchCount = 100;
                });

                // 2. Add Connection Pooling for better performance
                services.AddConnectionPooling<IConnection>(options =>
                {
                    options.MinPoolSize = 10;
                    options.MaxPoolSize = 100;
                    options.ConnectionTimeout = TimeSpan.FromSeconds(10);
                    options.ValidationInterval = TimeSpan.FromSeconds(30);
                    options.IdleTimeout = TimeSpan.FromMinutes(5);
                    options.EnableValidation = true;
                });

                // 3. Add Batch Processing with compression
                services.AddBatchProcessing(options =>
                {
                    options.Enabled = true;
                    options.MaxBatchSize = 200;
                    options.FlushInterval = TimeSpan.FromMilliseconds(100);
                    options.EnableCompression = true;
                    options.CompressionAlgorithm = CompressionAlgorithm.Brotli;
                    options.CompressionLevel = 6;
                    options.EnablePartialRetry = true;
                });

                // 4. Add Message Deduplication
                services.AddDeduplication(options =>
                {
                    options.Enabled = true;
                    options.Window = TimeSpan.FromMinutes(10);
                    options.MaxCacheSize = 200000;
                    options.Strategy = DeduplicationStrategy.ContentHash;
                });

                // 5. Add Message Encryption with Azure Key Vault
                services.AddMessageEncryption(options =>
                {
                    options.EnableEncryption = true;
                    options.EncryptionAlgorithm = "AES256";
                    options.KeyProvider = KeyProviderType.AzureKeyVault;
                    options.KeyVaultUrl = context.Configuration["Azure:KeyVault:Url"];
                    options.KeyName = "message-encryption-key";
                    options.EnableKeyRotation = true;
                    options.KeyRotationGracePeriod = TimeSpan.FromHours(24);
                });

                // 6. Add Authentication and Authorization
                services.AddMessageBrokerSecurity(options =>
                {
                    options.EnableAuthentication = true;
                    options.JwtIssuer = context.Configuration["Jwt:Issuer"];
                    options.JwtAudience = context.Configuration["Jwt:Audience"];
                    options.JwtSigningKey = context.Configuration["Jwt:SigningKey"];
                    options.TokenCacheTtl = TimeSpan.FromMinutes(10);

                    options.EnableAuthorization = true;
                    options.Roles = new Dictionary<string, string[]>
                    {
                        ["admin"] = new[] { "publish:*", "subscribe:*", "manage:*" },
                        ["publisher"] = new[] { "publish:orders.*", "publish:inventory.*" },
                        ["consumer"] = new[] { "subscribe:orders.*", "subscribe:inventory.*" }
                    };
                });

                // 7. Add Rate Limiting with per-tenant limits
                services.AddRateLimiting(options =>
                {
                    options.Enabled = true;
                    options.RequestsPerSecond = 5000;
                    options.Strategy = RateLimitStrategy.TokenBucket;
                    options.BurstSize = 500;

                    options.EnablePerTenantLimits = true;
                    options.TenantLimits = new Dictionary<string, int>
                    {
                        ["tenant-premium"] = 10000,
                        ["tenant-standard"] = 1000,
                        ["tenant-basic"] = 100
                    };
                    options.DefaultTenantLimit = 50;
                });

                // 8. Add Distributed Tracing with Jaeger
                services.AddDistributedTracing(options =>
                {
                    options.ServiceName = "OrderService";
                    options.ServiceVersion = "1.0.0";
                    options.EnableTracing = true;
                    options.SamplingRate = 0.1; // 10% sampling in production
                    options.CaptureMessagePayloads = false; // Don't capture sensitive data
                    options.CaptureMessageHeaders = true;
                    options.ExcludedHeaderKeys = new List<string>
                    {
                        "Authorization",
                        "X-API-Key",
                        "Password",
                        "CreditCard"
                    };
                    options.Exporters = new[]
                    {
                        TracingExporter.Jaeger,
                        TracingExporter.OTLP
                    };
                    options.JaegerAgentHost = "localhost";
                    options.JaegerAgentPort = 6831;
                    options.OtlpEndpoint = "http://otel-collector:4317";
                });

                // 9. Add Health Checks
                services.AddMessageBrokerHealthChecks(options =>
                {
                    options.CheckInterval = TimeSpan.FromSeconds(30);
                    options.Timeout = TimeSpan.FromSeconds(5);
                    options.IncludeDiagnostics = true;
                });

                // 10. Add Metrics with Prometheus
                services.AddMessageBrokerMetrics(options =>
                {
                    options.EnableMetrics = true;
                    options.EnablePrometheusExporter = true;
                    options.PrometheusEndpoint = "/metrics";
                    options.Labels = new Dictionary<string, string>
                    {
                        ["environment"] = context.HostingEnvironment.EnvironmentName,
                        ["service"] = "order-service",
                        ["version"] = "1.0.0"
                    };
                });

                // 11. Add application services
                services.AddScoped<SecureOrderService>();

                // 12. Add hosted service
                services.AddMessageBrokerHostedService();
            })
            .Build();

        // Configure web application
        var app = host as WebApplication;
        if (app != null)
        {
            // Add health check endpoints
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/ready");
            app.MapHealthChecks("/health/live");

            // Add Prometheus metrics endpoint
            app.MapPrometheusScrapingEndpoint("/metrics");
        }

        await host.StartAsync();

        // Example usage
        using (var scope = host.Services.CreateScope())
        {
            var orderService = scope.ServiceProvider.GetRequiredService<SecureOrderService>();

            // Create order with all enhancements active
            var order = new SecureOrder
            {
                OrderId = 123,
                CustomerId = 456,
                CustomerEmail = "customer@example.com",
                CreditCardNumber = "4111111111111111",
                TotalAmount = 99.99m,
                TenantId = "tenant-premium"
            };

            await orderService.CreateOrderAsync(order, "valid-jwt-token");
        }

        await host.WaitForShutdownAsync();
    }
}

/// <summary>
/// Secure order entity with sensitive data
/// </summary>
public class SecureOrder
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CreditCardNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Secure order created event (will be encrypted)
/// </summary>
public class SecureOrderCreatedEvent
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CreditCardNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Order service demonstrating all enhancements working together
/// </summary>
public class SecureOrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<SecureOrderService> _logger;

    public SecureOrderService(
        IMessageBroker messageBroker,
        ILogger<SecureOrderService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    /// <summary>
    /// Creates an order with all security and performance enhancements:
    /// - Message is encrypted (sensitive data protected)
    /// - Authentication token is validated
    /// - Authorization is checked
    /// - Rate limiting is applied per tenant
    /// - Message is deduplicated
    /// - Message is batched for performance
    /// - Distributed trace is created
    /// - Metrics are collected
    /// </summary>
    public async Task CreateOrderAsync(SecureOrder order, string authToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("CreateOrder");
        activity?.SetTag("order.id", order.OrderId);
        activity?.SetTag("tenant.id", order.TenantId);

        _logger.LogInformation(
            "Creating order {OrderId} for tenant {TenantId}",
            order.OrderId,
            order.TenantId);

        try
        {
            // Create event with sensitive data
            var orderEvent = new SecureOrderCreatedEvent
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerEmail = order.CustomerEmail,
                CreditCardNumber = order.CreditCardNumber, // Will be encrypted
                TotalAmount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow
            };

            // Publish with all enhancements
            await _messageBroker.PublishAsync(
                orderEvent,
                new PublishOptions
                {
                    RoutingKey = "orders.created",
                    Headers = new Dictionary<string, object>
                    {
                        // Authentication token (validated by security decorator)
                        ["Authorization"] = $"Bearer {authToken}",

                        // Tenant ID (used for rate limiting)
                        ["TenantId"] = order.TenantId,

                        // Correlation ID (for distributed tracing)
                        ["CorrelationId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString(),

                        // Idempotency key (for deduplication)
                        ["IdempotencyKey"] = $"order-{order.OrderId}",

                        // Message metadata
                        ["MessageVersion"] = "2.0",
                        ["Source"] = "OrderService"
                    }
                });

            _logger.LogInformation(
                "Order {OrderId} published successfully with all enhancements",
                order.OrderId);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning(
                "Rate limit exceeded for tenant {TenantId}. RetryAfter: {RetryAfter}",
                order.TenantId,
                ex.RetryAfter);

            activity?.SetStatus(ActivityStatusCode.Error, "Rate limit exceeded");
            throw;
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(
                ex,
                "Authentication failed for order {OrderId}",
                order.OrderId);

            activity?.SetStatus(ActivityStatusCode.Error, "Authentication failed");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create order {OrderId}",
                order.OrderId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Example of consuming messages with all enhancements
    /// </summary>
    public async Task SubscribeToOrdersAsync()
    {
        await _messageBroker.SubscribeAsync<SecureOrderCreatedEvent>(
            async (message, context, ct) =>
            {
                using var activity = Activity.Current?.Source.StartActivity("ProcessOrder");
                activity?.SetTag("order.id", message.OrderId);

                _logger.LogInformation(
                    "Processing order {OrderId}. Message was: encrypted={Encrypted}, authenticated={Authenticated}, deduplicated={Deduplicated}",
                    message.OrderId,
                    context.Headers?.ContainsKey("X-Encrypted") ?? false,
                    context.Headers?.ContainsKey("Authorization") ?? false,
                    context.Headers?.ContainsKey("X-Deduplicated") ?? false);

                try
                {
                    // Process order (message is already decrypted)
                    await ProcessOrderAsync(message);

                    // Acknowledge
                    if (context.Acknowledge != null)
                    {
                        await context.Acknowledge();
                    }

                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process order {OrderId}", message.OrderId);

                    // Reject for retry
                    if (context.Reject != null)
                    {
                        await context.Reject(requeue: true);
                    }

                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    throw;
                }
            },
            new SubscriptionOptions
            {
                QueueName = "order-processing-queue",
                RoutingKey = "orders.*",
                PrefetchCount = 100,
                AutoAck = false
            });
    }

    private async Task ProcessOrderAsync(SecureOrderCreatedEvent order)
    {
        // Simulate processing
        await Task.Delay(100);

        _logger.LogInformation(
            "Order {OrderId} processed. Customer: {Email}, Amount: {Amount}",
            order.OrderId,
            MaskEmail(order.CustomerEmail),
            order.TotalAmount);
    }

    private string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";

        var username = parts[0];
        var domain = parts[1];

        if (username.Length <= 2)
            return $"***@{domain}";

        return $"{username[0]}***{username[^1]}@{domain}";
    }
}

/// <summary>
/// Example of monitoring all enhancements
/// </summary>
public class EnhancementsMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnhancementsMonitoringService> _logger;

    public EnhancementsMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<EnhancementsMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                // Monitor connection pool
                var connectionPool = scope.ServiceProvider.GetService<IConnectionPool<IConnection>>();
                if (connectionPool != null)
                {
                    var poolMetrics = connectionPool.GetMetrics();
                    _logger.LogInformation(
                        "Connection Pool: Active={Active} Idle={Idle} WaitTime={WaitTime}ms",
                        poolMetrics.ActiveConnections,
                        poolMetrics.IdleConnections,
                        poolMetrics.AverageWaitTime.TotalMilliseconds);
                }

                // Monitor batch processor
                var batchProcessor = scope.ServiceProvider.GetService<IBatchProcessor<object>>();
                if (batchProcessor != null)
                {
                    var batchMetrics = batchProcessor.GetMetrics();
                    _logger.LogInformation(
                        "Batch Processing: AvgSize={Size} CompressionRatio={Ratio:P} Pending={Pending}",
                        batchMetrics.AverageBatchSize,
                        batchMetrics.CompressionRatio,
                        batchMetrics.PendingMessages);
                }

                // Monitor deduplication
                var deduplicationCache = scope.ServiceProvider.GetService<IDeduplicationCache>();
                if (deduplicationCache != null)
                {
                    var dedupMetrics = deduplicationCache.GetMetrics();
                    _logger.LogInformation(
                        "Deduplication: Total={Total} Duplicates={Duplicates} HitRate={HitRate:P}",
                        dedupMetrics.TotalMessages,
                        dedupMetrics.DuplicatesDetected,
                        dedupMetrics.HitRate);
                }

                // Monitor rate limiter
                var rateLimiter = scope.ServiceProvider.GetService<IRateLimiter>();
                if (rateLimiter != null)
                {
                    var rateLimitMetrics = rateLimiter.GetMetrics();
                    _logger.LogInformation(
                        "Rate Limiting: Allowed={Allowed} Rejected={Rejected} CurrentRate={Rate}/s",
                        rateLimitMetrics.AllowedRequests,
                        rateLimitMetrics.RejectedRequests,
                        rateLimitMetrics.CurrentRate);
                }

                // Check health
                var healthCheckService = scope.ServiceProvider.GetService<HealthCheckService>();
                if (healthCheckService != null)
                {
                    var healthReport = await healthCheckService.CheckHealthAsync(stoppingToken);
                    _logger.LogInformation(
                        "Health Status: {Status}",
                        healthReport.Status);

                    foreach (var entry in healthReport.Entries)
                    {
                        if (entry.Value.Status != HealthStatus.Healthy)
                        {
                            _logger.LogWarning(
                                "Unhealthy component: {Name} Status={Status} Description={Description}",
                                entry.Key,
                                entry.Value.Status,
                                entry.Value.Description);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring enhancements");
            }

            // Monitor every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
