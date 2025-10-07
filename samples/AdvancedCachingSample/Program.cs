using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Relay.AdvancedCaching.Example
{
    // Complex domain models for realistic caching scenarios
    public record Product(
        int Id,
        string Name,
        string Description,
        decimal Price,
        string Category,
        string SKU,
        int StockQuantity,
        DateTime LastUpdated,
        Dictionary<string, object> Metadata
    );

    public record Customer(
        int Id,
        string Name,
        string Email,
        string Tier,
        DateTime CreatedAt,
        List<string> Preferences,
        Dictionary<string, object> Attributes
    );

    public record Order(
        int Id,
        int CustomerId,
        List<OrderItem> Items,
        decimal Total,
        string Status,
        DateTime CreatedAt,
        DateTime? ShippedAt,
        ShippingAddress ShippingAddress
    );

    public record OrderItem(
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal
    );

    public record ShippingAddress(
        string Street,
        string City,
        string State,
        string PostalCode,
        string Country
    );

    public record CatalogSearchRequest(
        string Query,
        string Category,
        decimal? MinPrice,
        decimal? MaxPrice,
        string SortBy,
        int Page,
        int PageSize
    ) : IRequest<CatalogSearchResult>;

    public record CatalogSearchResult(
        List<Product> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );

    public record CustomerDashboardRequest(
        int CustomerId,
        DateTime? StartDate,
        DateTime? EndDate
    ) : IRequest<CustomerDashboard>;

    public record CustomerDashboard(
        Customer Customer,
        List<Order> RecentOrders,
        Dictionary<string, object> Analytics,
        List<Product> Recommendations
    );

    public record InventoryUpdateRequest(
        int ProductId,
        int QuantityChange,
        string Reason
    ) : IRequest<bool>;

    // Advanced caching attributes with different strategies
    [EnhancedCache(
        AbsoluteExpirationSeconds = 1800, // 30 minutes
        EnableCompression = true,
        EnableMetrics = true,
        Priority = CachePriority.High,
        Tags = new[] { "catalog", "search", "products" })]
    public record GetProductRequest(int ProductId) : IRequest<Product>;

    [EnhancedCache(
        SlidingExpirationSeconds = 900, // 15 minutes sliding
        EnableCompression = true,
        EnableMetrics = true,
        Priority = CachePriority.Normal,
        Tags = new[] { "catalog", "search" })]
    public record CatalogSearchRequest : IRequest<CatalogSearchResult>;

    [EnhancedCache(
        AbsoluteExpirationSeconds = 300, // 5 minutes
        EnableCompression = true,
        EnableMetrics = true,
        Priority = CachePriority.High,
        Tags = new[] { "customers", "dashboard" })]
    [CacheDependency("customer-data", DependencyType: CacheDependencyType.InvalidateOnAnyChange)]
    public record CustomerDashboardRequest : IRequest<CustomerDashboard>;

    // No caching for real-time inventory updates
    public record InventoryUpdateRequest : IRequest<bool>;

    // Short cache for real-time data that can be slightly stale
    [EnhancedCache(
        AbsoluteExpirationSeconds = 30, // 30 seconds
        EnableCompression = false,
        EnableMetrics = true,
        Priority = CachePriority.Low,
        Tags = new[] { "inventory", "realtime" })]
    public record GetInventoryRequest(int ProductId) : IRequest<int>;

    // Advanced e-commerce service with sophisticated caching
    public class AdvancedECommerceService
    {
        private readonly ILogger<AdvancedECommerceService> _logger;
        private readonly ICacheMetrics _cacheMetrics;
        private readonly ICacheInvalidator _cacheInvalidator;
        private static readonly Dictionary<int, Product> _productDatabase = new();
        private static readonly Dictionary<int, Customer> _customerDatabase = new();
        private static readonly List<Order> _orderDatabase = new();
        private static readonly Dictionary<int, int> _inventoryDatabase = new();

        public AdvancedECommerceService(
            ILogger<AdvancedECommerceService> logger,
            ICacheMetrics cacheMetrics,
            ICacheInvalidator cacheInvalidator)
        {
            _logger = logger;
            _cacheMetrics = cacheMetrics;
            _cacheInvalidator = cacheInvalidator;
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            // Initialize sample products
            for (int i = 1; i <= 100; i++)
            {
                _productDatabase[i] = new Product(
                    i,
                    $"Product {i}",
                    $"This is a detailed description for product {i} with lots of features and benefits that would make it quite long when serialized to demonstrate compression capabilities.",
                    i * 19.99m,
                    new[] { "Electronics", "Clothing", "Books", "Home", "Sports" }[i % 5],
                    $"SKU-{i:D6}",
                    Random.Shared.Next(0, 1000),
                    DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365)),
                    new Dictionary<string, object>
                    {
                        ["brand"] = $"Brand {(i % 10) + 1}",
                        ["rating"] = Math.Round(Random.Shared.NextDouble() * 5, 1),
                        ["reviews"] = Random.Shared.Next(0, 500),
                        ["weight"] = Random.Shared.NextDouble() * 10,
                        ["dimensions"] = $"{Random.Shared.Next(1, 50)}x{Random.Shared.Next(1, 50)}x{Random.Shared.Next(1, 50)}"
                    }
                );
                _inventoryDatabase[i] = Random.Shared.Next(0, 1000);
            }

            // Initialize sample customers
            for (int i = 1; i <= 50; i++)
            {
                _customerDatabase[i] = new Customer(
                    i,
                    $"Customer {i}",
                    $"customer{i}@example.com",
                    new[] { "Bronze", "Silver", "Gold", "Platinum" }[i % 4],
                    DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 1000)),
                    new List<string> { $"Category{i % 5}", $"Brand{i % 10}" },
                    new Dictionary<string, object>
                    {
                        ["totalOrders"] = Random.Shared.Next(1, 100),
                        ["totalSpent"] = Random.Shared.NextDouble() * 10000,
                        ["lastOrderDate"] = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365)),
                        ["loyaltyPoints"] = Random.Shared.Next(0, 5000)
                    }
                );
            }

            // Initialize sample orders
            for (int i = 1; i <= 200; i++)
            {
                var customerId = Random.Shared.Next(1, 51);
                var itemCount = Random.Shared.Next(1, 5);
                var items = new List<OrderItem>();
                var total = 0m;

                for (int j = 0; j < itemCount; j++)
                {
                    var productId = Random.Shared.Next(1, 101);
                    var product = _productDatabase[productId];
                    var quantity = Random.Shared.Next(1, 5);
                    var lineTotal = product.Price * quantity;
                    
                    items.Add(new OrderItem(
                        productId,
                        product.Name,
                        quantity,
                        product.Price,
                        lineTotal
                    ));
                    total += lineTotal;
                }

                _orderDatabase.Add(new Order(
                    i,
                    customerId,
                    items,
                    total,
                    new[] { "Pending", "Processing", "Shipped", "Delivered" }[Random.Shared.Next(0, 4)],
                    DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365)),
                    Random.Shared.Next(0, 2) == 0 ? DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)) : null,
                    new ShippingAddress(
                        $"{Random.Shared.Next(100, 999)} Main St",
                        "City",
                        "ST",
                        $"{Random.Shared.Next(10000, 99999)}",
                        "USA"
                    )
                ));
            }

            _logger.LogInformation("Initialized sample data: {Products} products, {Customers} customers, {Orders} orders",
                _productDatabase.Count, _customerDatabase.Count, _orderDatabase.Count);
        }

        [Handle]
        public async ValueTask<Product> GetProduct(GetProductRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching product {ProductId} from database", request.ProductId);
            await Task.Delay(50, cancellationToken); // Simulate database query

            if (_productDatabase.TryGetValue(request.ProductId, out var product))
            {
                return product;
            }

            throw new KeyNotFoundException($"Product {request.ProductId} not found");
        }

        [Handle]
        public async ValueTask<CatalogSearchResult> SearchCatalog(CatalogSearchRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Searching catalog with query: {Query}, category: {Category}", 
                request.Query, request.Category);
            
            await Task.Delay(200, cancellationToken); // Simulate complex search query

            var products = _productDatabase.Values.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                products = products.Where(p => 
                    p.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(request.Query, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                products = products.Where(p => 
                    p.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));
            }

            if (request.MinPrice.HasValue)
            {
                products = products.Where(p => p.Price >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= request.MaxPrice.Value);
            }

            // Apply sorting
            products = request.SortBy?.ToLowerInvariant() switch
            {
                "price" => products.OrderBy(p => p.Price),
                "name" => products.OrderBy(p => p.Name),
                "stock" => products.OrderByDescending(p => p.StockQuantity),
                _ => products.OrderBy(p => p.Id)
            };

            var totalCount = products.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var pagedProducts = products
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new CatalogSearchResult(pagedProducts, totalCount, request.Page, request.PageSize, totalPages);
        }

        [Handle]
        public async ValueTask<CustomerDashboard> GetCustomerDashboard(CustomerDashboardRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Generating dashboard for customer {CustomerId}", request.CustomerId);
            await Task.Delay(300, cancellationToken); // Simulate complex dashboard generation

            if (!_customerDatabase.TryGetValue(request.CustomerId, out var customer))
            {
                throw new KeyNotFoundException($"Customer {request.CustomerId} not found");
            }

            var customerOrders = _orderDatabase
                .Where(o => o.CustomerId == request.CustomerId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToList();

            var analytics = new Dictionary<string, object>
            {
                ["totalOrders"] = customerOrders.Count,
                ["totalSpent"] = customerOrders.Sum(o => o.Total),
                ["avgOrderValue"] = customerOrders.Any() ? customerOrders.Average(o => o.Total) : 0,
                ["orderStatusBreakdown"] = customerOrders
                    .GroupBy(o => o.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["monthlySpending"] = customerOrders
                    .Where(o => o.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                    .Sum(o => o.Total),
                ["favoriteCategory"] = customerOrders
                    .SelectMany(o => o.Items)
                    .GroupBy(i => _productDatabase[i.ProductId]?.Category)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "Unknown"
            };

            var recommendations = _productDatabase.Values
                .Where(p => p.Category == analytics["favoriteCategory"] as string)
                .OrderByDescending(p => p.StockQuantity)
                .Take(5)
                .ToList();

            return new CustomerDashboard(customer, customerOrders, analytics, recommendations);
        }

        [Handle]
        public async ValueTask<bool> UpdateInventory(InventoryUpdateRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating inventory for product {ProductId} by {QuantityChange}", 
                request.ProductId, request.QuantityChange);
            
            await Task.Delay(10, cancellationToken); // Simulate fast database update

            if (_inventoryDatabase.TryGetValue(request.ProductId, out var currentQuantity))
            {
                var newQuantity = currentQuantity + request.QuantityChange;
                if (newQuantity < 0)
                {
                    _logger.LogWarning("Inventory update would result in negative quantity for product {ProductId}", 
                        request.ProductId);
                    return false;
                }

                _inventoryDatabase[request.ProductId] = newQuantity;

                // Invalidate related caches
                await _cacheInvalidator.InvalidateByTagAsync("inventory");
                await _cacheInvalidator.InvalidateByTagAsync("products");

                _logger.LogInformation("Updated inventory for product {ProductId}: {OldQuantity} -> {NewQuantity}", 
                    request.ProductId, currentQuantity, newQuantity);
                
                return true;
            }

            return false;
        }

        [Handle]
        public async ValueTask<int> GetInventory(GetInventoryRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting inventory for product {ProductId}", request.ProductId);
            await Task.Delay(5, cancellationToken); // Simulate fast database lookup

            return _inventoryDatabase.TryGetValue(request.ProductId, out var quantity) ? quantity : 0;
        }

        public void DisplayCacheStatistics()
        {
            var stats = _cacheMetrics.GetStatistics();
            _logger.LogInformation("""
                === Advanced Cache Statistics ===
                Total Hits: {TotalHits:N0}
                Total Misses: {TotalMisses:N0}
                Total Sets: {TotalSets:N0}
                Hit Ratio: {HitRatio:P2}
                Total Data Size: {TotalDataSize:N0} bytes
                Total Evictions: {TotalEvictions:N0}
                
                Performance Metrics:
                - Average Hit Time: {AvgHitTime}ms
                - Average Miss Time: {AvgMissTime}ms
                - Compression Ratio: {CompressionRatio:P2}
                ===================================
                """, 
                stats.TotalHits,
                stats.TotalMisses,
                stats.TotalSets,
                stats.HitRatio,
                stats.TotalDataSize,
                stats.TotalEvictions,
                "N/A", // These would be available in a real implementation
                "N/A",
                "N/A"
            );
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🛒 Relay Advanced E-Commerce Caching Sample");
            Console.WriteLine("============================================");
            Console.WriteLine();

            // Setup host with advanced caching configuration
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Add caching infrastructure
                services.AddMemoryCache(options =>
                {
                    options.SizeLimit = 1000; // Limit memory cache size
                });

                // For demo purposes, use distributed memory cache
                // In production, you would use Redis:
                // services.AddStackExchangeRedisCache(options =>
                // {
                //     options.Configuration = "localhost:6379";
                //     options.InstanceName = "ecommerce:";
                // });
                services.AddDistributedMemoryCache();

                // Add enhanced caching services with custom configuration
                services.AddSingleton<ICacheKeyGenerator>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<DefaultCacheKeyGenerator>>();
                    return new DefaultCacheKeyGenerator(logger, "ecommerce-{requestType}-{hash}");
                });

                services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
                services.AddSingleton<ICacheCompressor>(provider =>
                {
                    return new GzipCacheCompressor(thresholdBytes: 512); // Lower threshold for demo
                });
                services.AddSingleton<ICacheMetrics, DefaultCacheMetrics>();
                services.AddSingleton<ICacheInvalidator, DefaultCacheInvalidator>();
                services.AddSingleton<ICacheKeyTracker, DefaultCacheKeyTracker>();

                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Add Relay with advanced caching
                services.AddRelay(builder =>
                {
                    builder.AddHandlersFromAssemblyContaining<AdvancedECommerceService>();
                    builder.AddCaching();
                    builder.AddEnhancedCaching();
                });

                // Register our service
                services.AddSingleton<AdvancedECommerceService>();
            });

            var host = builder.Build();
            var serviceProvider = host.Services;
            var ecommerceService = serviceProvider.GetRequiredService<AdvancedECommerceService>();

            Console.WriteLine("🚀 Advanced E-Commerce Caching Demonstrations");
            Console.WriteLine();

            // Demo 1: Product catalog with compression
            await DemonstrateProductCatalog(ecommerceService);

            // Demo 2: Search functionality with complex queries
            await DemonstrateSearchFunctionality(ecommerceService);

            // Demo 3: Customer dashboard with cache dependencies
            await DemonstrateCustomerDashboard(ecommerceService);

            // Demo 4: Real-time inventory with cache invalidation
            await DemonstrateInventoryManagement(ecommerceService);

            // Demo 5: Cache performance analysis
            ecommerceService.DisplayCacheStatistics();

            Console.WriteLine();
            Console.WriteLine("✅ Advanced e-commerce caching demonstration completed!");
            Console.WriteLine();
            Console.WriteLine("Advanced Features Demonstrated:");
            Console.WriteLine("  ✓ Complex domain models with rich data");
            Console.WriteLine("  ✓ Multi-layer caching with compression");
            Console.WriteLine("  ✓ Cache dependencies and invalidation");
            Console.WriteLine("  ✓ Search result caching");
            Console.WriteLine("  ✓ Real-time data with short cache windows");
            Console.WriteLine("  ✓ Performance optimization strategies");
            Console.WriteLine("  ✓ Cache metrics and monitoring");
        }

        static async Task DemonstrateProductCatalog(AdvancedECommerceService ecommerceService)
        {
            Console.WriteLine("📦 Demo 1: Product Catalog with Compression");
            Console.WriteLine("--------------------------------------------");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var product = await ecommerceService.GetProduct(new GetProductRequest(42));
            stopwatch.Stop();

            Console.WriteLine($"Product: {product.Name}");
            Console.WriteLine($"Category: {product.Category}");
            Console.WriteLine($"Price: ${product.Price}");
            Console.WriteLine($"Stock: {product.StockQuantity}");
            Console.WriteLine($"Description length: {product.Description.Length} chars");
            Console.WriteLine($"Metadata: {product.Metadata.Count} properties");
            Console.WriteLine($"First fetch time: {stopwatch.ElapsedMilliseconds}ms");

            // Second fetch should be from cache
            stopwatch.Restart();
            await ecommerceService.GetProduct(new GetProductRequest(42));
            stopwatch.Stop();
            Console.WriteLine($"Second fetch time (cached): {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine();
        }

        static async Task DemonstrateSearchFunctionality(AdvancedECommerceService ecommerceService)
        {
            Console.WriteLine("🔍 Demo 2: Search Functionality with Complex Queries");
            Console.WriteLine("----------------------------------------------------");

            var searchRequests = new[]
            {
                new CatalogSearchRequest("Product", "Electronics", null, null, "price", 1, 10),
                new CatalogSearchRequest("", "Clothing", 50m, 200m, "name", 1, 5),
                new CatalogSearchRequest("detailed", null, null, null, "stock", 1, 10)
            };

            foreach (var request in searchRequests)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await ecommerceService.SearchCatalog(request);
                stopwatch.Stop();

                Console.WriteLine($"Search: Query='{request.Query}', Category='{request.Category}'");
                Console.WriteLine($"Results: {result.Products.Count} of {result.TotalCount} (Page {result.Page}/{result.TotalPages})");
                Console.WriteLine($"Search time: {stopwatch.ElapsedMilliseconds}ms");

                if (result.Products.Any())
                {
                    var sample = result.Products.First();
                    Console.WriteLine($"Sample result: {sample.Name} - ${sample.Price}");
                }
                Console.WriteLine();
            }

            // Repeat the same search to demonstrate caching
            Console.WriteLine("Repeating first search to test cache...");
            var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
            await ecommerceService.SearchCatalog(searchRequests[0]);
            stopwatch2.Stop();
            Console.WriteLine($"Cached search time: {stopwatch2.ElapsedMilliseconds}ms");
            Console.WriteLine();
        }

        static async Task DemonstrateCustomerDashboard(AdvancedECommerceService ecommerceService)
        {
            Console.WriteLine("👤 Demo 3: Customer Dashboard with Cache Dependencies");
            Console.WriteLine("------------------------------------------------------");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var dashboard = await ecommerceService.GetCustomerDashboard(new CustomerDashboardRequest(15));
            stopwatch.Stop();

            Console.WriteLine($"Customer: {dashboard.Customer.Name} ({dashboard.Customer.Email})");
            Console.WriteLine($"Tier: {dashboard.Customer.Tier}");
            Console.WriteLine($"Recent Orders: {dashboard.RecentOrders.Count}");
            Console.WriteLine($"Total Spent: ${dashboard.Analytics["totalSpent"]}");
            Console.WriteLine($"Average Order Value: ${dashboard.Analytics["avgOrderValue"]}");
            Console.WriteLine($"Favorite Category: {dashboard.Analytics["favoriteCategory"]}");
            Console.WriteLine($"Recommendations: {dashboard.Recommendations.Count}");
            Console.WriteLine($"Dashboard generation time: {stopwatch.ElapsedMilliseconds}ms");

            // Second request should be cached
            stopwatch.Restart();
            await ecommerceService.GetCustomerDashboard(new CustomerDashboardRequest(15));
            stopwatch.Stop();
            Console.WriteLine($"Cached dashboard time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine();
        }

        static async Task DemonstrateInventoryManagement(AdvancedECommerceService ecommerceService)
        {
            Console.WriteLine("📊 Demo 4: Real-time Inventory with Cache Invalidation");
            Console.WriteLine("--------------------------------------------------------");

            var productId = 25;

            // Check initial inventory
            var initialInventory = await ecommerceService.GetInventory(new GetInventoryRequest(productId));
            Console.WriteLine($"Initial inventory for product {productId}: {initialInventory} units");

            // Update inventory (this will invalidate related caches)
            Console.WriteLine("Updating inventory...");
            var updateSuccess = await ecommerceService.UpdateInventory(
                new InventoryUpdateRequest(productId, -5, "Sale"));
            
            if (updateSuccess)
            {
                var newInventory = await ecommerceService.GetInventory(new GetInventoryRequest(productId));
                Console.WriteLine($"Updated inventory for product {productId}: {newInventory} units");

                // Fetch product details - should reflect updated inventory
                var product = await ecommerceService.GetProduct(new GetProductRequest(productId));
                Console.WriteLine($"Product stock after update: {product.StockQuantity}");
            }

            Console.WriteLine();
        }
    }
}