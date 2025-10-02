// 🏷️ Relay Framework - Comprehensive Attribute Usage Examples
// Bu dosya, gerçek world senaryolarında attribute kullanımını gösterir

using Relay.Core;
using System.ComponentModel.DataAnnotations;

namespace ComprehensiveRelayAPI;

// ============================================================================
// 📋 DOMAIN MODELS
// ============================================================================

public record User(int Id, string Name, string Email, DateTime CreatedAt, List<string> Roles);
public record Product(int Id, string Name, string Description, decimal Price, int Stock, string Category);
public record Order(int Id, int UserId, List<OrderItem> Items, decimal TotalAmount, OrderStatus Status, DateTime CreatedAt);
public record OrderItem(int ProductId, int Quantity, decimal UnitPrice);

public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled }

// ============================================================================
// 📨 REQUEST/RESPONSE DTOs
// ============================================================================

// Query Requests
public record GetUserQuery(int UserId) : IRequest<User?>;
public record GetUsersQuery(int Page = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResponse<User>>;
public record GetProductQuery(int ProductId) : IRequest<Product?>;
public record GetProductsQuery(string? Category = null, decimal? MinPrice = null, decimal? MaxPrice = null, int Page = 1, int PageSize = 10) : IRequest<PagedResponse<Product>>;
public record GetOrderQuery(int OrderId) : IRequest<Order?>;

// Command Requests
public record CreateUserCommand(string Name, string Email, string? PhoneNumber, List<string> Roles) : IRequest<User>;
public record UpdateUserCommand(int UserId, string? Name, string? Email, List<string>? Roles) : IRequest<User?>;
public record DeleteUserCommand(int UserId) : IRequest<bool>;
public record CreateProductCommand(string Name, string Description, decimal Price, int Stock, string Category) : IRequest<Product>;
public record UpdateProductStockCommand(int ProductId, int NewStock) : IRequest<Product?>;
public record CreateOrderCommand(int UserId, List<OrderItemRequest> Items, string? Notes) : IRequest<Order>;
public record UpdateOrderStatusCommand(int OrderId, OrderStatus NewStatus) : IRequest<Order?>;

// Stream Requests
public record GetUserActivityStreamQuery(int UserId, DateTime? FromDate = null) : IStreamRequest<UserActivity>;

// Support Types
public record PagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize);
public record OrderItemRequest(int ProductId, int Quantity);
public record UserActivity(DateTime Timestamp, string Activity, string Details);

// Notifications
public record UserCreatedNotification(int UserId, string UserName, string Email) : INotification;
public record OrderCreatedNotification(int OrderId, int UserId, decimal TotalAmount) : INotification;
public record OrderStatusChangedNotification(int OrderId, OrderStatus OldStatus, OrderStatus NewStatus) : INotification;

// ============================================================================
// 🎯 REQUEST HANDLERS WITH ATTRIBUTES
// ============================================================================

/// <summary>
/// User management handlers demonstrating various Handle attribute uses
/// </summary>
public class UserHandlers : 
    IRequestHandler<GetUserQuery, User?>,
    IRequestHandler<GetUsersQuery, PagedResponse<User>>,
    IRequestHandler<CreateUserCommand, User>,
    IRequestHandler<UpdateUserCommand, User?>,
    IRequestHandler<DeleteUserCommand, bool>
{
    private readonly IUserService _userService;
    private readonly IRelay _mediator;
    private readonly ILogger<UserHandlers> _logger;

    public UserHandlers(IUserService userService, IRelay mediator, ILogger<UserHandlers> logger)
    {
        _userService = userService;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// High-priority cached user retrieval with automatic endpoint exposure
    /// </summary>
    [Handle(Name = "GetUser_Optimized", Priority = 10)]
    [ExposeAsEndpoint(Route = "/api/users/{userId}", HttpMethod = "GET", Version = "2.0")]
    public async ValueTask<User?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving user {UserId} with optimized handler", request.UserId);
        return await _userService.GetByIdAsync(request.UserId);
    }

    /// <summary>
    /// Paginated user listing with search capability
    /// </summary>
    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/users", HttpMethod = "GET")]
    public async ValueTask<PagedResponse<User>> HandleAsync(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetPagedAsync(request.Page, request.PageSize, request.SearchTerm);
    }

    /// <summary>
    /// User creation with notification publishing
    /// </summary>
    [Handle(Name = "CreateUser_WithNotifications", Priority = 8)]
    [ExposeAsEndpoint(Route = "/api/users", HttpMethod = "POST")]
    public async ValueTask<User> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new user: {Email}", request.Email);
        
        var user = await _userService.CreateAsync(new User(
            Id: 0,
            Name: request.Name,
            Email: request.Email,
            CreatedAt: DateTime.UtcNow,
            Roles: request.Roles
        ));

        // Publish notification for downstream processing
        await _mediator.PublishAsync(new UserCreatedNotification(user.Id, user.Name, user.Email), cancellationToken);
        
        return user;
    }

    /// <summary>
    /// User update with validation
    /// </summary>
    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/users/{userId}", HttpMethod = "PUT")]
    public async ValueTask<User?> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userService.GetByIdAsync(request.UserId);
        if (existingUser == null)
        {
            return null;
        }

        var updatedUser = existingUser with
        {
            Name = request.Name ?? existingUser.Name,
            Email = request.Email ?? existingUser.Email,
            Roles = request.Roles ?? existingUser.Roles
        };

        return await _userService.UpdateAsync(updatedUser);
    }

    /// <summary>
    /// User deletion with soft delete pattern
    /// </summary>
    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/users/{userId}", HttpMethod = "DELETE")]
    public async ValueTask<bool> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        return await _userService.DeleteAsync(request.UserId);
    }
}

/// <summary>
/// Alternative user handlers showing different implementation strategies
/// </summary>
public class AlternativeUserHandlers : IRequestHandler<GetUserQuery, User?>
{
    private readonly ICacheService _cacheService;
    private readonly IUserService _userService;

    public AlternativeUserHandlers(ICacheService cacheService, IUserService userService)
    {
        _cacheService = cacheService;
        _userService = userService;
    }

    /// <summary>
    /// Cache-first user retrieval strategy
    /// </summary>
    [Handle(Name = "GetUser_FromCache", Priority = 15)]
    public async ValueTask<User?> HandleFromCache(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Try cache first
        var cachedUser = await _cacheService.GetAsync<User>($"user:{request.UserId}");
        if (cachedUser != null)
        {
            return cachedUser;
        }

        // Fallback to database
        var user = await _userService.GetByIdAsync(request.UserId);
        if (user != null)
        {
            await _cacheService.SetAsync($"user:{request.UserId}", user, TimeSpan.FromMinutes(5));
        }

        return user;
    }

    /// <summary>
    /// Fallback user retrieval from backup source
    /// </summary>
    [Handle(Name = "GetUser_Fallback", Priority = 1)]
    public async ValueTask<User?> HandleFallback(GetUserQuery request, CancellationToken cancellationToken)
    {
        // This would run if higher priority handlers fail or return null
        return await _userService.GetFromBackupSourceAsync(request.UserId);
    }
}

// ============================================================================
// 🛍️ PRODUCT HANDLERS
// ============================================================================

public class ProductHandlers : 
    IRequestHandler<GetProductQuery, Product?>,
    IRequestHandler<GetProductsQuery, PagedResponse<Product>>,
    IRequestHandler<CreateProductCommand, Product>,
    IRequestHandler<UpdateProductStockCommand, Product?>
{
    private readonly IProductService _productService;

    public ProductHandlers(IProductService productService)
    {
        _productService = productService;
    }

    [Handle(Priority = 10)]
    [ExposeAsEndpoint(Route = "/api/products/{productId}", HttpMethod = "GET")]
    public async ValueTask<Product?> HandleAsync(GetProductQuery request, CancellationToken cancellationToken)
    {
        return await _productService.GetByIdAsync(request.ProductId);
    }

    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/products", HttpMethod = "GET")]
    public async ValueTask<PagedResponse<Product>> HandleAsync(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await _productService.GetFilteredAsync(
            request.Category, 
            request.MinPrice, 
            request.MaxPrice, 
            request.Page, 
            request.PageSize
        );
    }

    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/products", HttpMethod = "POST")]
    public async ValueTask<Product> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        return await _productService.CreateAsync(new Product(
            Id: 0,
            Name: request.Name,
            Description: request.Description,
            Price: request.Price,
            Stock: request.Stock,
            Category: request.Category
        ));
    }

    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/products/{productId}/stock", HttpMethod = "PUT")]
    public async ValueTask<Product?> HandleAsync(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        return await _productService.UpdateStockAsync(request.ProductId, request.NewStock);
    }
}

// ============================================================================
// 📦 ORDER HANDLERS
// ============================================================================

public class OrderHandlers : 
    IRequestHandler<GetOrderQuery, Order?>,
    IRequestHandler<CreateOrderCommand, Order>,
    IRequestHandler<UpdateOrderStatusCommand, Order?>
{
    private readonly IOrderService _orderService;
    private readonly IRelay _mediator;

    public OrderHandlers(IOrderService orderService, IRelay mediator)
    {
        _orderService = orderService;
        _mediator = mediator;
    }

    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/orders/{orderId}", HttpMethod = "GET")]
    public async ValueTask<Order?> HandleAsync(GetOrderQuery request, CancellationToken cancellationToken)
    {
        return await _orderService.GetByIdAsync(request.OrderId);
    }

    [Handle(Priority = 8)]
    [ExposeAsEndpoint(Route = "/api/orders", HttpMethod = "POST")]
    public async ValueTask<Order> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(request);
        
        // Publish notification for order processing pipeline
        await _mediator.PublishAsync(new OrderCreatedNotification(order.Id, order.UserId, order.TotalAmount), cancellationToken);
        
        return order;
    }

    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/orders/{orderId}/status", HttpMethod = "PUT")]
    public async ValueTask<Order?> HandleAsync(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var existingOrder = await _orderService.GetByIdAsync(request.OrderId);
        if (existingOrder == null) return null;

        var updatedOrder = await _orderService.UpdateStatusAsync(request.OrderId, request.NewStatus);
        
        if (updatedOrder != null)
        {
            await _mediator.PublishAsync(new OrderStatusChangedNotification(
                updatedOrder.Id, 
                existingOrder.Status, 
                updatedOrder.Status
            ), cancellationToken);
        }

        return updatedOrder;
    }
}

// ============================================================================
// 🌊 STREAMING HANDLERS
// ============================================================================

public class StreamingHandlers : IStreamHandler<GetUserActivityStreamQuery, UserActivity>
{
    private readonly IUserActivityService _activityService;

    public StreamingHandlers(IUserActivityService activityService)
    {
        _activityService = activityService;
    }

    [Handle(Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/users/{userId}/activity/stream", HttpMethod = "GET")]
    public async IAsyncEnumerable<UserActivity> HandleAsync(
        GetUserActivityStreamQuery request, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var activity in _activityService.GetActivityStreamAsync(request.UserId, request.FromDate, cancellationToken))
        {
            yield return activity;
        }
    }
}

// ============================================================================
// 📢 NOTIFICATION HANDLERS WITH ATTRIBUTES
// ============================================================================

/// <summary>
/// User-related notification handlers with different priorities and dispatch modes
/// </summary>
public class UserNotificationHandlers : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IAuditService _auditService;
    private readonly ISecurityService _securityService;

    public UserNotificationHandlers(
        IEmailService emailService,
        IAnalyticsService analyticsService,
        IAuditService auditService,
        ISecurityService securityService)
    {
        _emailService = emailService;
        _analyticsService = analyticsService;
        _auditService = auditService;
        _securityService = securityService;
    }

    /// <summary>
    /// Critical user setup - runs first, sequentially
    /// </summary>
    [Notification(Priority = 100, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandleUserValidationAndSetup(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Critical operations that must complete successfully
        await _auditService.LogUserCreatedAsync(notification.UserId, notification.UserName);
        await _securityService.SetupUserPermissionsAsync(notification.UserId);
    }

    /// <summary>
    /// Welcome email - high priority, sequential (after validation)
    /// </summary>
    [Notification(Priority = 90, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandleWelcomeEmail(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email, notification.UserName);
    }

    /// <summary>
    /// Analytics tracking - can run in parallel
    /// </summary>
    [Notification(Priority = 50, DispatchMode = NotificationDispatchMode.Parallel)]
    public async ValueTask HandleAnalytics(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _analyticsService.TrackUserCreatedAsync(notification.UserId, notification.Email);
    }

    /// <summary>
    /// External system notification - low priority, parallel
    /// </summary>
    [Notification(Priority = 10, DispatchMode = NotificationDispatchMode.Parallel)]
    public async ValueTask HandleExternalNotification(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _analyticsService.NotifyExternalSystemAsync("user_created", new { 
                UserId = notification.UserId, 
                Email = notification.Email 
            });
        }
        catch (Exception ex)
        {
            // Don't fail the entire flow for external system issues
            // Log and continue
        }
    }
}

/// <summary>
/// Order-related notifications with complex processing pipeline
/// </summary>
public class OrderNotificationHandlers : 
    INotificationHandler<OrderCreatedNotification>,
    INotificationHandler<OrderStatusChangedNotification>
{
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ILogisticsService _logisticsService;

    public OrderNotificationHandlers(
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IEmailService emailService,
        ILogisticsService logisticsService)
    {
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _emailService = emailService;
        _logisticsService = logisticsService;
    }

    // Order Created Notifications - Sequential processing for business logic
    [Notification(Priority = 100, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandleInventoryReservation(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _inventoryService.ReserveItemsAsync(notification.OrderId);
    }

    [Notification(Priority = 90, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandlePaymentProcessing(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _paymentService.ProcessPaymentAsync(notification.OrderId, notification.TotalAmount);
    }

    // Parallel notifications for non-critical operations
    [Notification(Priority = 50, DispatchMode = NotificationDispatchMode.Parallel)]
    public async ValueTask HandleOrderConfirmationEmail(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendOrderConfirmationAsync(notification.UserId, notification.OrderId);
    }

    [Notification(Priority = 50, DispatchMode = NotificationDispatchMode.Parallel)]
    public async ValueTask HandleOrderAnalytics(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _analyticsService.TrackOrderCreatedAsync(notification.OrderId, notification.TotalAmount);
    }

    // Order Status Changed Notifications
    [Notification(Priority = 80, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandleStatusChangeNotification(OrderStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.NewStatus == OrderStatus.Shipped)
        {
            await _logisticsService.InitiateShippingAsync(notification.OrderId);
        }
    }

    [Notification(Priority = 50, DispatchMode = NotificationDispatchMode.Parallel)]
    public async ValueTask HandleCustomerStatusNotification(OrderStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendOrderStatusUpdateAsync(notification.OrderId, notification.NewStatus);
    }
}

// ============================================================================
// 🔧 PIPELINE BEHAVIORS WITH ATTRIBUTES
// ============================================================================

/// <summary>
/// Performance monitoring pipeline - wraps all operations
/// </summary>
public class PerformanceMonitoringPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<PerformanceMonitoringPipeline<TRequest, TResponse>> _logger;
    
    public PerformanceMonitoringPipeline(ILogger<PerformanceMonitoringPipeline<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    [Pipeline(Order = -2, Scope = PipelineScope.All)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;
            if (elapsedMs > 1000)
            {
                _logger.LogWarning("Slow request detected: {RequestName} took {ElapsedMs}ms", requestName, elapsedMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Comprehensive logging pipeline
/// </summary>
public class LoggingPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingPipeline<TRequest, TResponse>> _logger;

    public LoggingPipeline(ILogger<LoggingPipeline<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    [Pipeline(Order = -1, Scope = PipelineScope.All)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling request: {RequestName} with data: {@Request}", requestName, request);

        var response = await next();

        _logger.LogInformation("Handled request: {RequestName}", requestName);
        return response;
    }
}

/// <summary>
/// Request validation pipeline - only for requests
/// </summary>
public class ValidationPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IValidator<TRequest> _validator;

    public ValidationPipeline(IValidator<TRequest> validator)
    {
        _validator = validator;
    }

    [Pipeline(Order = 1, Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validator != null)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
            }
        }

        return await next();
    }
}

/// <summary>
/// Intelligent caching pipeline
/// </summary>
public class CachingPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingPipeline<TRequest, TResponse>> _logger;

    public CachingPipeline(IMemoryCache cache, ILogger<CachingPipeline<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    [Pipeline(Order = 2, Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only cache query operations
        if (!IsQueryRequest(typeof(TRequest)))
        {
            return await next();
        }

        var cacheKey = GenerateCacheKey(request);
        
        if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
        {
            _logger.LogDebug("Cache hit for {RequestType} with key {CacheKey}", typeof(TRequest).Name, cacheKey);
            return cachedResponse;
        }

        var response = await next();
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = GetCacheDuration(typeof(TRequest)),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };
        
        _cache.Set(cacheKey, response, cacheOptions);
        _logger.LogDebug("Cached response for {RequestType} with key {CacheKey}", typeof(TRequest).Name, cacheKey);
        
        return response;
    }

    private static bool IsQueryRequest(Type requestType)
    {
        return requestType.Name.Contains("Query") || requestType.Name.StartsWith("Get");
    }

    private static string GenerateCacheKey<T>(T request)
    {
        return $"{typeof(T).Name}:{request?.GetHashCode()}";
    }

    private static TimeSpan GetCacheDuration(Type requestType)
    {
        return requestType.Name switch
        {
            var name when name.Contains("User") => TimeSpan.FromMinutes(5),
            var name when name.Contains("Product") => TimeSpan.FromMinutes(10),
            var name when name.Contains("Order") => TimeSpan.FromMinutes(2),
            _ => TimeSpan.FromMinutes(1)
        };
    }
}

/// <summary>
/// Global exception handling pipeline
/// </summary>
public class ExceptionHandlingPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<ExceptionHandlingPipeline<TRequest, TResponse>> _logger;

    public ExceptionHandlingPipeline(ILogger<ExceptionHandlingPipeline<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    [Pipeline(Order = 10, Scope = PipelineScope.All)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed for {RequestType}: {Errors}", typeof(TRequest).Name, string.Join(", ", ex.Errors));
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access for {RequestType}: {Message}", typeof(TRequest).Name, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}

// ============================================================================
// 🎯 SERVICE INTERFACES (for demonstration)
// ============================================================================

public interface IUserService
{
    ValueTask<User?> GetByIdAsync(int id);
    ValueTask<PagedResponse<User>> GetPagedAsync(int page, int pageSize, string? searchTerm);
    ValueTask<User> CreateAsync(User user);
    ValueTask<User> UpdateAsync(User user);
    ValueTask<bool> DeleteAsync(int id);
    ValueTask<User?> GetFromBackupSourceAsync(int id);
}

public interface IProductService
{
    ValueTask<Product?> GetByIdAsync(int id);
    ValueTask<PagedResponse<Product>> GetFilteredAsync(string? category, decimal? minPrice, decimal? maxPrice, int page, int pageSize);
    ValueTask<Product> CreateAsync(Product product);
    ValueTask<Product?> UpdateStockAsync(int productId, int newStock);
}

public interface IOrderService
{
    ValueTask<Order?> GetByIdAsync(int id);
    ValueTask<Order> CreateAsync(CreateOrderCommand command);
    ValueTask<Order?> UpdateStatusAsync(int orderId, OrderStatus newStatus);
}

public interface IUserActivityService
{
    IAsyncEnumerable<UserActivity> GetActivityStreamAsync(int userId, DateTime? fromDate, CancellationToken cancellationToken);
}

public interface ICacheService
{
    ValueTask<T?> GetAsync<T>(string key);
    ValueTask SetAsync<T>(string key, T value, TimeSpan expiration);
}

public interface IEmailService
{
    ValueTask SendWelcomeEmailAsync(string email, string userName);
    ValueTask SendOrderConfirmationAsync(int userId, int orderId);
    ValueTask SendOrderStatusUpdateAsync(int orderId, OrderStatus newStatus);
}

public interface IAnalyticsService
{
    ValueTask TrackUserCreatedAsync(int userId, string email);
    ValueTask TrackOrderCreatedAsync(int orderId, decimal totalAmount);
    ValueTask NotifyExternalSystemAsync(string eventType, object data);
}

public interface IAuditService
{
    ValueTask LogUserCreatedAsync(int userId, string userName);
}

public interface ISecurityService
{
    ValueTask SetupUserPermissionsAsync(int userId);
}

public interface IInventoryService
{
    ValueTask ReserveItemsAsync(int orderId);
}

public interface IPaymentService
{
    ValueTask ProcessPaymentAsync(int orderId, decimal amount);
}

public interface ILogisticsService
{
    ValueTask InitiateShippingAsync(int orderId);
}

public interface IValidator<T>
{
    ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; init; }
    public List<ValidationError> Errors { get; init; } = new();
}

public record ValidationError(string ErrorMessage);

public class ValidationException : Exception
{
    public IEnumerable<string> Errors { get; }
    
    public ValidationException(IEnumerable<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }
}