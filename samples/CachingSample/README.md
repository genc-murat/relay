# Relay Enhanced Caching Sample

This sample demonstrates the comprehensive enhanced caching capabilities of the Relay framework, including advanced features like compression, metrics, cache invalidation, and distributed caching.

## Features Demonstrated

### 🚀 Enhanced Cache Attributes

The sample showcases the new `[EnhancedCache]` attribute with advanced configuration options:

```csharp
[EnhancedCache(
    AbsoluteExpirationSeconds = 300,     // Cache for 5 minutes
    EnableCompression = true,            // Compress cached data
    EnableMetrics = true,                // Track cache performance
    Priority = CachePriority.High,       // High priority cache entries
    Tags = new[] { "users", "frequently-accessed" })]
public record GetUserRequest(int UserId) : IRequest<User>;
```

### 📊 Cache Metrics and Monitoring

Real-time cache statistics including:
- Cache hit/miss ratios
- Total data size cached
- Eviction counts
- Performance metrics

### 🗜️ Data Compression

Automatic compression of cached data using GZIP to reduce memory usage:
- Enabled per-request type
- Threshold-based compression (only compresses data larger than 1KB)
- Transparent compression/decompression

### ⏰ Flexible Expiration Strategies

- **Absolute Expiration**: Cache expires after a fixed time
- **Sliding Expiration**: Cache expires after inactivity period
- **Priority-based eviction**: High, Normal, Low, NeverRemove priorities

### 🔗 Cache Dependencies

Cache invalidation based on related data changes:
```csharp
[CacheDependency("user-orders", DependencyType: CacheDependencyType.InvalidateOnUpdate)]
public record GetUserOrdersRequest(int UserId) : IRequest<List<Order>>;
```

### 🏷️ Cache Tags

Organize and manage cache entries using tags for:
- Bulk invalidation by tag
- Cache statistics by category
- Better cache organization

## Running the Sample

1. Navigate to the sample directory:
   ```bash
   cd samples/CachingSample
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

## Sample Output

The application demonstrates several caching scenarios:

### Demo 1: Basic Caching with Compression
Shows how user and product data are cached with compression enabled for large payloads.

### Demo 2: Cache Hits and Performance
Demonstrates the performance difference between cache misses and hits:
- First call: ~200ms (database simulation)
- Subsequent calls: ~1-2ms (cache hits)

### Demo 3: Cache Expiration and Priorities
Shows different expiration strategies:
- Analytics reports: 60 seconds (low priority)
- User orders: 30 minutes (high priority)

### Demo 4: Real-time Non-Cached Requests
Demonstrates requests that bypass caching for real-time data like stock prices.

### Demo 5: Cache Statistics
Displays comprehensive cache performance metrics.

## Configuration

The sample configures enhanced caching services in `Program.cs`:

```csharp
// Add enhanced caching services
services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
services.AddSingleton<ICacheCompressor, GzipCacheCompressor>();
services.AddSingleton<ICacheMetrics, DefaultCacheMetrics>();
services.AddSingleton<ICacheInvalidator, DefaultCacheInvalidator>();
services.AddSingleton<ICacheKeyTracker, DefaultCacheKeyTracker>();

// Add Relay with enhanced caching
services.AddRelay(builder =>
{
    builder.AddHandlersFromAssemblyContaining<EnhancedDataService>();
    builder.AddCaching();
    builder.AddEnhancedCaching();
});
```

## Key Concepts

### Memory vs Distributed Cache

The sample uses both:
- **Memory Cache**: Fast local caching
- **Distributed Cache**: For multi-instance scenarios (using memory cache for demo)

### Cache Key Generation

Automatic cache key generation based on:
- Request type
- Request properties
- Custom patterns

### Serialization

JSON-based serialization with:
- Custom serializer support
- Type safety
- Performance optimization

## Advanced Features

### Cache Invalidation

Programmatic cache invalidation:
```csharp
public void InvalidateUserOrdersCache(int userId)
{
    // Use ICacheInvalidator to invalidate specific cache entries
}
```

### Performance Monitoring

Built-in metrics collection:
```csharp
public void DisplayCacheStatistics()
{
    var stats = _cacheMetrics.GetStatistics();
    // Display comprehensive cache statistics
}
```

### Compression Thresholds

Automatic compression based on data size:
- Small data (< 1KB): No compression
- Large data (> 1KB): GZIP compression

## Best Practices Demonstrated

1. **Appropriate Caching**: Cache frequently accessed, slow-to-generate data
2. **Expiration Strategies**: Use appropriate expiration times based on data volatility
3. **Compression**: Enable compression for large data payloads
4. **Monitoring**: Track cache performance to optimize strategies
5. **Tagging**: Use tags for organized cache management
6. **Dependencies**: Set up cache dependencies for related data

## Production Considerations

In production environments:

1. **Use Redis**: Replace `AddDistributedMemoryCache()` with Redis for true distributed caching
2. **Configure Compression**: Adjust compression thresholds based on your data patterns
3. **Monitor Metrics**: Set up monitoring and alerting for cache performance
4. **Cache Warming**: Implement cache warming strategies for critical data
5. **Security**: Ensure sensitive data is properly encrypted in distributed cache

## Related Samples

- [ComprehensiveRelayAPI](../ComprehensiveRelayAPI/) - Full API example with caching
- [ObservabilitySample](../ObservabilitySample/) - Metrics and monitoring
- [WebApiIntegrationSample](../WebApiIntegrationSample/) - Web API integration

## Learn More

- [Relay Caching Documentation](../../../docs/caching-guide.md)
- [Enhanced Cache Attributes](../../../docs/contract-validation-guide.md)
- [Performance Optimization](../../../docs/performance-guide.md)