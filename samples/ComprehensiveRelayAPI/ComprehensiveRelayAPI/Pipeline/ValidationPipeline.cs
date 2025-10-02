using ComprehensiveRelayAPI.Models;
using FluentValidation;
using Relay.Core;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace ComprehensiveRelayAPI.Pipeline;

/// <summary>
/// Pipeline behavior for request validation using FluentValidation
/// </summary>
public class ValidationPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationPipeline> _logger;

    public ValidationPipeline(IServiceProvider serviceProvider, ILogger<ValidationPipeline> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Validate requests before processing
    /// </summary>
    [Pipeline(Order = 1, Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> ValidateRequest<TRequest, TResponse>(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var validator = _serviceProvider.GetService<IValidator<TRequest>>();
        
        if (validator != null)
        {
            _logger.LogDebug("Validating request of type: {RequestType}", typeof(TRequest).Name);
            
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed for {RequestType}: {Errors}", 
                    typeof(TRequest).Name, string.Join(", ", errors));
                
                throw new ValidationException($"Validation failed for {typeof(TRequest).Name}: {string.Join(", ", errors)}");
            }
            
            _logger.LogDebug("Validation passed for request of type: {RequestType}", typeof(TRequest).Name);
        }

        return await next();
    }
}

/// <summary>
/// Pipeline behavior for logging requests and responses
/// </summary>
public class LoggingPipeline
{
    private readonly ILogger<LoggingPipeline> _logger;

    public LoggingPipeline(ILogger<LoggingPipeline> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Log all requests and responses with performance metrics
    /// </summary>
    [Pipeline(Order = 0, Scope = PipelineScope.All)]
    public async ValueTask<TResponse> LogRequests<TRequest, TResponse>(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var responseType = typeof(TResponse).Name;
        
        _logger.LogInformation("🚀 Processing request: {RequestType} → {ResponseType}", requestType, responseType);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            stopwatch.Stop();
            
            _logger.LogInformation("✅ Completed request: {RequestType} in {ElapsedMs}ms", 
                requestType, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "❌ Failed request: {RequestType} after {ElapsedMs}ms - {ErrorMessage}", 
                requestType, stopwatch.ElapsedMilliseconds, ex.Message);
            
            throw;
        }
    }

    /// <summary>
    /// Log notifications
    /// </summary>
    [Pipeline(Order = 0, Scope = PipelineScope.Notifications)]
    public async ValueTask LogNotifications<TNotification>(
        TNotification notification,
        Func<ValueTask> next,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var notificationType = typeof(TNotification).Name;
        
        _logger.LogInformation("📢 Publishing notification: {NotificationType}", notificationType);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await next();
            stopwatch.Stop();
            
            _logger.LogInformation("✅ Notification published: {NotificationType} in {ElapsedMs}ms", 
                notificationType, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "❌ Failed notification: {NotificationType} after {ElapsedMs}ms - {ErrorMessage}", 
                notificationType, stopwatch.ElapsedMilliseconds, ex.Message);
            
            throw;
        }
    }
}

/// <summary>
/// Pipeline behavior for caching responses
/// </summary>
public class CachingPipeline
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingPipeline> _logger;

    public CachingPipeline(IMemoryCache cache, ILogger<CachingPipeline> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Cache GET requests that implement ICacheableRequest
    /// </summary>
    [Pipeline(Order = 2, Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> CacheRequests<TRequest, TResponse>(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        // Only cache if the request implements our caching interface
        if (request is ICacheableRequest cacheableRequest)
        {
            var cacheKey = cacheableRequest.GetCacheKey();
            
            if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return cachedResponse!;
            }
            
            _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
            
            var response = await next();
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheableRequest.GetCacheDuration(),
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(cacheKey, response, cacheOptions);
            _logger.LogDebug("Response cached with key: {CacheKey} for {Duration}", 
                cacheKey, cacheableRequest.GetCacheDuration());
            
            return response;
        }

        return await next();
    }
}

/// <summary>
/// Pipeline behavior for handling exceptions and converting them to API responses
/// </summary>
public class ExceptionHandlingPipeline
{
    private readonly ILogger<ExceptionHandlingPipeline> _logger;

    public ExceptionHandlingPipeline(ILogger<ExceptionHandlingPipeline> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle exceptions and convert them to proper API responses
    /// </summary>
    [Pipeline(Order = 10, Scope = PipelineScope.All)]
    public async ValueTask<TResponse> HandleExceptions<TRequest, TResponse>(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error in {RequestType}: {Message}", typeof(TRequest).Name, ex.Message);
            
            // If TResponse is ApiResponse<T>, create an error response
            if (typeof(TResponse).IsGenericType && 
                typeof(TResponse).GetGenericTypeDefinition() == typeof(ApiResponse<>))
            {
                var responseType = typeof(TResponse).GetGenericArguments()[0];
                var errorResponse = Activator.CreateInstance(typeof(TResponse)) as dynamic;
                
                if (errorResponse != null)
                {
                    errorResponse.Success = false;
                    errorResponse.Message = "Validation failed";
                    errorResponse.Errors = ex.Message.Split(',').Select(e => e.Trim()).ToList();
                    errorResponse.Data = null;
                    
                    return (TResponse)errorResponse;
                }
            }
            
            throw; // Re-throw if we can't handle it
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argument error in {RequestType}: {Message}", typeof(TRequest).Name, ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation in {RequestType}: {Message}", typeof(TRequest).Name, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {RequestType}: {Message}", typeof(TRequest).Name, ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Interface for requests that can be cached
/// </summary>
public interface ICacheableRequest
{
    string GetCacheKey();
    TimeSpan GetCacheDuration();
}

/// <summary>
/// Performance monitoring pipeline
/// </summary>
public class PerformanceMonitoringPipeline
{
    private readonly ILogger<PerformanceMonitoringPipeline> _logger;

    public PerformanceMonitoringPipeline(ILogger<PerformanceMonitoringPipeline> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Monitor performance and log slow requests
    /// </summary>
    [Pipeline(Order = -1, Scope = PipelineScope.All)]
    public async ValueTask<TResponse> MonitorPerformance<TRequest, TResponse>(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            stopwatch.Stop();
            
            var elapsed = stopwatch.ElapsedMilliseconds;
            var requestType = typeof(TRequest).Name;
            
            // Log slow requests (> 1 second)
            if (elapsed > 1000)
            {
                _logger.LogWarning("🐌 Slow request detected: {RequestType} took {ElapsedMs}ms", requestType, elapsed);
            }
            else if (elapsed > 500)
            {
                _logger.LogInformation("⚠️ Request took {ElapsedMs}ms: {RequestType}", elapsed, requestType);
            }
            else
            {
                _logger.LogDebug("⚡ Fast request: {RequestType} took {ElapsedMs}ms", requestType, elapsed);
            }
            
            // Track performance metrics (in a real app, you'd send these to a monitoring system)
            using var activity = Activity.Current;
            activity?.SetTag("request.type", requestType);
            activity?.SetTag("request.duration_ms", elapsed);
            activity?.SetTag("request.performance.category", 
                elapsed > 1000 ? "slow" : elapsed > 500 ? "moderate" : "fast");
            
            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            throw;
        }
    }
}