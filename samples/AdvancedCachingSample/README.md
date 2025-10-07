# Relay Advanced E-Commerce Caching Sample

This advanced sample demonstrates sophisticated caching strategies in a realistic e-commerce scenario, showcasing complex domain models, cache dependencies, and performance optimization techniques.

## 🚀 Advanced Features Demonstrated

### Complex Domain Models
- **Rich Product Catalog**: Products with detailed metadata, descriptions, and inventory
- **Customer Management**: Customer data with preferences, tiers, and analytics
- **Order Processing**: Complex order structures with items and shipping information
- **Search Functionality**: Advanced catalog search with filtering and pagination

### Multi-Layer Caching Strategies
- **Product Catalog**: Long-term caching with compression (30 minutes)
- **Search Results**: Sliding expiration for frequently accessed searches (15 minutes)
- **Customer Dashboards**: Medium-term caching with dependencies (5 minutes)
- **Real-time Inventory**: Short cache windows for frequently changing data (30 seconds)

### Cache Dependencies and Invalidation
```csharp
[CacheDependency("customer-data", DependencyType: CacheDependencyType.InvalidateOnAnyChange)]
public record CustomerDashboardRequest : IRequest<CustomerDashboard>;
```

### Performance Optimization
- **Data Compression**: Automatic GZIP compression for large payloads
- **Cache Priorities**: High, Normal, and Low priority cache entries
- **Smart Key Generation**: Custom cache key patterns for different data types
- **Metrics Collection**: Comprehensive cache performance monitoring

## 📊 Sample Scenarios

### 1. Product Catalog with Compression
Demonstrates caching of complex product data with automatic compression:
- Large product descriptions and metadata
- Compression reduces memory usage significantly
- Fast retrieval for frequently accessed products

### 2. Advanced Search Functionality
Shows caching of complex search results:
- Multi-parameter search queries
- Pagination support
- Sorting and filtering
- Cache invalidation on inventory changes

### 3. Customer Dashboard with Dependencies
Illustrates cache dependencies in action:
- Aggregated customer data
- Order history and analytics
- Personalized recommendations
- Automatic invalidation on data changes

### 4. Real-time Inventory Management
Demonstrates short cache windows for dynamic data:
- Fast inventory updates
- Cache invalidation propagation
- Real-time data consistency

## 🛠️ Configuration

### Enhanced Cache Attributes
```csharp
[EnhancedCache(
    AbsoluteExpirationSeconds = 1800,
    EnableCompression = true,
    EnableMetrics = true,
    Priority = CachePriority.High,
    Tags = new[] { "catalog", "search", "products" })]
```

### Custom Cache Configuration
```csharp
services.AddSingleton<ICacheKeyGenerator>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<DefaultCacheKeyGenerator>>();
    return new DefaultCacheKeyGenerator(logger, "ecommerce-{requestType}-{hash}");
});

services.AddSingleton<ICacheCompressor>(provider =>
{
    return new GzipCacheCompressor(thresholdBytes: 512);
});
```

### Memory Cache Limits
```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Limit memory cache size
});
```

## 🏃‍♂️ Running the Sample

1. **Navigate to the sample directory:**
   ```bash
   cd samples/AdvancedCachingSample
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

## 📈 Expected Output

The sample demonstrates four main scenarios:

### Demo 1: Product Catalog
```
📦 Demo 1: Product Catalog with Compression
--------------------------------------------
Product: Product 42
Category: Electronics
Price: $839.58
Stock: 723 units
Description length: 154 chars
Metadata: 5 properties
First fetch time: 52ms
Second fetch time (cached): 1ms
```

### Demo 2: Search Functionality
```
🔍 Demo 2: Search Functionality with Complex Queries
----------------------------------------------------
Search: Query='Product', Category='Electronics'
Results: 20 of 20 (Page 1/2)
Search time: 203ms
Sample result: Product 2 - $39.98

Repeating first search to test cache...
Cached search time: 2ms
```

### Demo 3: Customer Dashboard
```
👤 Demo 3: Customer Dashboard with Cache Dependencies
------------------------------------------------------
Customer: Customer 15 (customer15@example.com)
Tier: Gold
Recent Orders: 4
Total Spent: $3,847.23
Average Order Value: $961.81
Favorite Category: Electronics
Recommendations: 5
Dashboard generation time: 305ms
Cached dashboard time: 1ms
```

### Demo 4: Inventory Management
```
📊 Demo 4: Real-time Inventory with Cache Invalidation
--------------------------------------------------------
Initial inventory for product 25: 456 units
Updating inventory...
Updated inventory for product 25: 451 units
Product stock after update: 451
```

### Cache Statistics
```
=== Advanced Cache Statistics ===
Total Hits: 8
Total Misses: 12
Total Sets: 12
Hit Ratio: 40.00%
Total Data Size: 15,234 bytes
Total Evictions: 0
===================================
```

## 🔧 Production Considerations

### Distributed Cache Setup
For production environments, replace the in-memory distributed cache with Redis:

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "ecommerce:";
});
```

### Performance Tuning
- **Compression Threshold**: Adjust based on your data patterns
- **Cache Sizes**: Set appropriate limits for memory and distributed cache
- **Expiration Times**: Tune based on data volatility and access patterns
- **Monitoring**: Set up alerts for cache hit ratios and eviction rates

### Cache Warming Strategies
```csharp
// Warm up critical caches on application startup
public class CacheWarmupService
{
    public async Task WarmupCriticalDataAsync()
    {
        // Pre-load popular products
        // Cache frequent search queries
        // Warm up customer dashboards for active users
    }
}
```

## 📚 Advanced Concepts

### Cache Invalidation Patterns
1. **Time-based Invalidation**: Automatic expiration
2. **Event-driven Invalidation**: Based on data changes
3. **Manual Invalidation**: Administrative actions
4. **Dependency-based Invalidation**: Related data changes

### Performance Monitoring
- **Hit Ratio**: Target > 80% for frequently accessed data
- **Memory Usage**: Monitor cache size and eviction rates
- **Compression Ratio**: Track effectiveness of compression
- **Response Times**: Compare cached vs. uncached performance

### Cache Key Strategies
- **Hierarchical Keys**: `ecommerce:product:123`
- **Versioned Keys**: `ecommerce:product:123:v2`
- **Tagged Keys**: `ecommerce:product:123[electronics,featured]`
- **Hashed Keys**: For complex request parameters

## 🎯 Best Practices Demonstrated

1. **Appropriate Cache Durations**: Match cache times to data volatility
2. **Compression for Large Data**: Reduce memory footprint
3. **Cache Dependencies**: Maintain data consistency
4. **Performance Monitoring**: Track cache effectiveness
5. **Granular Cache Control**: Different strategies for different data types
6. **Real-time Data Handling**: Short cache windows for dynamic data

## 🔄 Related Samples

- [CachingSample](../CachingSample/) - Basic caching features
- [ComprehensiveRelayAPI](../ComprehensiveRelayAPI/) - Full API implementation
- [ObservabilitySample](../ObservabilitySample/) - Metrics and monitoring

## 📖 Learn More

- [Relay Caching Guide](../../../docs/caching-guide.md)
- [Performance Optimization](../../../docs/performance-guide.md)
- [Cache Invalidation Strategies](../../../docs/cache-invalidation-guide.md)
- [Distributed Caching](../../../docs/distributed-caching-guide.md)