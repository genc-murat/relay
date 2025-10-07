using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Relay;
using Relay.Core;
using Relay.Core.Caching;
using Relay.Core.Caching.Attributes;
using Relay.Core.Caching.Compression;
using Relay.Core.Caching.Invalidation;
using Relay.Core.Caching.Metrics;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Core;
using Relay.Core.Configuration.Core;
using Relay.Core.Contracts.Pipeline;

namespace Relay.Caching.Example
{
    // Example requests with enhanced caching attributes
    [EnhancedCache(
        AbsoluteExpirationSeconds = 300, 
        EnableCompression = true,
        EnableMetrics = true,
        Priority = CachePriority.High,
        Tags = new[] { "users", "frequently-accessed" })]
    public record GetUserRequest(int UserId) : IRequest<User>;
    
    [EnhancedCache(
        SlidingExpirationSeconds = 600,
        EnableCompression = false,
        EnableMetrics = true,
        Priority = CachePriority.Normal,
        Tags = new[] { "products" })]
    public record GetProductRequest(int ProductId) : IRequest<Product>;
    
    [EnhancedCache(
        AbsoluteExpirationSeconds = 60,
        EnableCompression = true,
        EnableMetrics = true,
        Priority = CachePriority.Low,
        Tags = new[] { "analytics", "reports" })]
    public record GetAnalyticsRequest(string ReportType, DateTime Date) : IRequest<AnalyticsReport>;
    
    // Request with cache dependencies
    [EnhancedCache(
        AbsoluteExpirationSeconds = 1800,
        EnableMetrics = true,
        Tags = new[] { "orders" })]
    [CacheDependency("user-orders", CacheDependencyType.InvalidateOnUpdate)]
    public record GetUserOrdersRequest(int UserId) : IRequest<List<Order>>;
    
    // Request without caching
    public record GetRealTimeStockPriceRequest(string Symbol) : IRequest<StockPrice>;
    
    // Example responses
    public record User(int Id, string Name, string Email, DateTime LastModified);
    public record Product(int Id, string Name, decimal Price, string Category, string Description);
    public record AnalyticsReport(string ReportType, DateTime Date, Dictionary<string, object> Data);
    public record Order(int Id, int UserId, decimal Total, DateTime OrderDate, string Status);
    public record StockPrice(string Symbol, decimal Price, DateTime Timestamp);
    
    // Enhanced data service with comprehensive caching examples
    public class EnhancedDataService
    {
        private static int _userCallCount = 0;
        private static int _productCallCount = 0;
        private static int _analyticsCallCount = 0;
        private static int _ordersCallCount = 0;
        private static int _stockPriceCallCount = 0;
        
        private readonly ILogger<EnhancedDataService> _logger;
        private readonly ICacheMetrics _cacheMetrics;
        
        public EnhancedDataService(ILogger<EnhancedDataService> logger, ICacheMetrics cacheMetrics)
        {
            _logger = logger;
            _cacheMetrics = cacheMetrics;
        }
        
        [Handle]
        public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _userCallCount);
            _logger.LogInformation("Fetching user {UserId} from database (call #{CallCount})", 
                request.UserId, _userCallCount);
            
            await Task.Delay(200, cancellationToken); // Simulate database work
            
            return new User(
                request.UserId, 
                $"User {request.UserId}", 
                $"user{request.UserId}@example.com",
                DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60))
            );
        }
        
        [Handle]
        public async ValueTask<Product> GetProduct(GetProductRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _productCallCount);
            _logger.LogInformation("Fetching product {ProductId} from database (call #{CallCount})", 
                request.ProductId, _productCallCount);
            
            await Task.Delay(150, cancellationToken); // Simulate database work
            
            var categories = new[] { "Electronics", "Books", "Clothing", "Home", "Sports" };
            return new Product(
                request.ProductId,
                $"Product {request.ProductId}",
                Math.Round(request.ProductId * 19.99m, 2),
                categories[Random.Shared.Next(categories.Length)],
                $"This is a detailed description for product {request.ProductId} with lots of text to demonstrate compression."
            );
        }
        
        [Handle]
        public async ValueTask<AnalyticsReport> GetAnalytics(GetAnalyticsRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _analyticsCallCount);
            _logger.LogInformation("Generating analytics report {ReportType} for {Date} (call #{CallCount})", 
                request.ReportType, request.Date.ToShortDateString(), _analyticsCallCount);
            
            await Task.Delay(1000, cancellationToken); // Simulate heavy analytics processing
            
            var data = new Dictionary<string, object>
            {
                ["totalUsers"] = Random.Shared.Next(1000, 10000),
                ["activeUsers"] = Random.Shared.Next(500, 5000),
                ["totalRevenue"] = Random.Shared.Next(10000, 100000),
                ["conversionRate"] = Math.Round(Random.Shared.NextDouble() * 0.1, 4),
                ["avgSessionDuration"] = TimeSpan.FromMinutes(Random.Shared.Next(5, 30)),
                ["reportGeneratedAt"] = DateTime.UtcNow
            };
            
            return new AnalyticsReport(request.ReportType, request.Date, data);
        }
        
        [Handle]
        public async ValueTask<List<Order>> GetUserOrders(GetUserOrdersRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _ordersCallCount);
            _logger.LogInformation("Fetching orders for user {UserId} from database (call #{CallCount})", 
                request.UserId, _ordersCallCount);
            
            await Task.Delay(300, cancellationToken); // Simulate database work
            
            var orders = new List<Order>();
            var orderCount = Random.Shared.Next(1, 10);
            
            for (int i = 0; i < orderCount; i++)
            {
                orders.Add(new Order(
                    i + 1,
                    request.UserId,
                    (decimal)Math.Round(Random.Shared.NextDouble() * 1000, 2),
                    DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    Random.Shared.Next(0, 2) == 0 ? "Completed" : "Processing"
                ));
            }
            
            return orders;
        }
        
        [Handle]
        public async ValueTask<StockPrice> GetRealTimeStockPrice(GetRealTimeStockPriceRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _stockPriceCallCount);
            _logger.LogInformation("Fetching real-time stock price for {Symbol} (call #{CallCount})", 
                request.Symbol, _stockPriceCallCount);
            
            await Task.Delay(50, cancellationToken); // Simulate API call
            
            return new StockPrice(
                request.Symbol,
                    (decimal)Math.Round(Random.Shared.NextDouble() * 1000, 2),
                DateTime.UtcNow
            );
        }
        
        // Method to demonstrate cache invalidation
        public void InvalidateUserOrdersCache(int userId)
        {
            _logger.LogInformation("Manually invalidating cache for user {UserId} orders", userId);
            // In a real application, you would use ICacheInvalidator here
        }
        
        // Method to display cache statistics
        public void DisplayCacheStatistics()
        {
            var stats = _cacheMetrics.GetStatistics();
            _logger.LogInformation("""
                === Cache Statistics ===
                Total Hits: {TotalHits}
                Total Misses: {TotalMisses}
                Total Sets: {TotalSets}
                Hit Ratio: {HitRatio:P2}
                Total Data Size: {TotalDataSize:N0} bytes
                Total Evictions: {TotalEvictions}
                ========================
                """, 
                stats.Hits,
                stats.Misses,
                stats.Sets,
                stats.HitRatio,
                stats.TotalDataSize,
                stats.Evictions
            );
        }
        
        public static int UserCallCount => _userCallCount;
        public static int ProductCallCount => _productCallCount;
        public static int AnalyticsCallCount => _analyticsCallCount;
        public static int OrdersCallCount => _ordersCallCount;
        public static int StockPriceCallCount => _stockPriceCallCount;
    }
    
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Relay Enhanced Caching Sample");
            Console.WriteLine("================================");
            Console.WriteLine();
            
            // Setup host with enhanced caching services
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Add memory cache
                services.AddMemoryCache();
                
                // Add distributed cache (using memory cache for demo)
                services.AddDistributedMemoryCache();
                
                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                
                // Add enhanced caching services
                services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
                services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
                services.AddSingleton<ICacheCompressor, GzipCacheCompressor>();
                services.AddSingleton<ICacheMetrics, DefaultCacheMetrics>();
                services.AddSingleton<ICacheInvalidator, DefaultCacheInvalidator>();
                services.AddSingleton<ICacheKeyTracker, SimpleCacheKeyTracker>();
                
                // Add Relay with enhanced caching
                services.AddMemoryCache();
                services.AddRelayCaching();
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Relay.Core.Caching.Behaviors.EnhancedCachingPipelineBehavior<,>));
                
                // Register our service
                services.AddSingleton<EnhancedDataService>();
            });
            
            var host = builder.Build();
            var serviceProvider = host.Services;
            var dataService = serviceProvider.GetRequiredService<EnhancedDataService>();
            
            Console.WriteLine("Demonstrating Enhanced Caching Features:");
            Console.WriteLine();
            
            // Demo 1: Basic caching with compression
            await DemonstrateBasicCaching(dataService);
            
            // Demo 2: Cache hits and performance
            await DemonstrateCacheHits(dataService);
            
            // Demo 3: Different cache priorities and expiration
            await DemonstrateCacheExpiration(dataService);
            
            // Demo 4: Non-cached requests
            await DemonstrateNonCachedRequests(dataService);
            
            // Demo 5: Cache statistics
            dataService.DisplayCacheStatistics();
            
            Console.WriteLine();
            Console.WriteLine("✅ Enhanced caching demonstration completed!");
            Console.WriteLine();
            Console.WriteLine("Key Features Demonstrated:");
            Console.WriteLine("  ✓ Enhanced cache attributes with compression");
            Console.WriteLine("  ✓ Cache metrics and monitoring");
            Console.WriteLine("  ✓ Different cache priorities");
            Console.WriteLine("  ✓ Absolute and sliding expiration");
            Console.WriteLine("  ✓ Cache tags for organization");
            Console.WriteLine("  ✓ Cache dependencies");
            Console.WriteLine("  ✓ Non-cached real-time data");
        }
        
        static async Task DemonstrateBasicCaching(EnhancedDataService dataService)
        {
            Console.WriteLine("📦 Demo 1: Basic Caching with Compression");
            Console.WriteLine("-----------------------------------------");
            
            var user = await dataService.GetUser(new GetUserRequest(1), CancellationToken.None);
            Console.WriteLine($"User: {user.Name} ({user.Email})");
            
            var product = await dataService.GetProduct(new GetProductRequest(101), CancellationToken.None);
            Console.WriteLine($"Product: {product.Name} - ${product.Price} ({product.Category})");
            
            Console.WriteLine();
        }
        
        static async Task DemonstrateCacheHits(EnhancedDataService dataService)
        {
            Console.WriteLine("⚡ Demo 2: Cache Hits and Performance");
            Console.WriteLine("--------------------------------------");
            
            Console.WriteLine("Making multiple requests for the same data...");
            
            // First call - should be a cache miss
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await dataService.GetUser(new GetUserRequest(2), CancellationToken.None);
            stopwatch.Stop();
            Console.WriteLine($"First call (cache miss): {stopwatch.ElapsedMilliseconds}ms");
            
            // Second call - should be a cache hit
            stopwatch.Restart();
            await dataService.GetUser(new GetUserRequest(2), CancellationToken.None);
            stopwatch.Stop();
            Console.WriteLine($"Second call (cache hit): {stopwatch.ElapsedMilliseconds}ms");
            
            // Third call - should also be a cache hit
            stopwatch.Restart();
            await dataService.GetUser(new GetUserRequest(2), CancellationToken.None);
            stopwatch.Stop();
            Console.WriteLine($"Third call (cache hit): {stopwatch.ElapsedMilliseconds}ms");
            
            Console.WriteLine($"Total user service calls: {EnhancedDataService.UserCallCount}");
            Console.WriteLine();
        }
        
        static async Task DemonstrateCacheExpiration(EnhancedDataService dataService)
        {
            Console.WriteLine("⏰ Demo 3: Cache Expiration and Priorities");
            Console.WriteLine("-------------------------------------------");
            
            // Analytics with short expiration (60 seconds)
            Console.WriteLine("Generating analytics report (short expiration)...");
            var analytics = await dataService.GetAnalytics(new GetAnalyticsRequest("daily", DateTime.Today), CancellationToken.None);
            Console.WriteLine($"Analytics: {analytics.ReportType} - {analytics.Data.Count} metrics");
            
            // User orders with longer expiration (30 minutes)
            Console.WriteLine("Fetching user orders (longer expiration)...");
            var orders = await dataService.GetUserOrders(new GetUserOrdersRequest(3), CancellationToken.None);
            Console.WriteLine($"Orders: {orders.Count} orders found");
            
            Console.WriteLine();
        }
        
        static async Task DemonstrateNonCachedRequests(EnhancedDataService dataService)
        {
            Console.WriteLine("🔄 Demo 4: Real-time Non-Cached Requests");
            Console.WriteLine("------------------------------------------");
            
            Console.WriteLine("Fetching real-time stock prices (no caching)...");
            
            for (int i = 0; i < 3; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var stockPrice = await dataService.GetRealTimeStockPrice(new GetRealTimeStockPriceRequest("AAPL"), CancellationToken.None);
                stopwatch.Stop();
                
                Console.WriteLine($"Stock Price: ${stockPrice.Price} (took {stopwatch.ElapsedMilliseconds}ms)");
                await Task.Delay(100); // Small delay between calls
            }
            
            Console.WriteLine($"Total stock price service calls: {EnhancedDataService.StockPriceCallCount}");
            Console.WriteLine();
        }
    }
}