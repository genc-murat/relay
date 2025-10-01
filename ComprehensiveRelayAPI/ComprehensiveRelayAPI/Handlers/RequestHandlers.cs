using ComprehensiveRelayAPI.Models;
using ComprehensiveRelayAPI.Requests;
using ComprehensiveRelayAPI.Services;
using Microsoft.Extensions.Caching.Memory;
using Relay.Core;
using System.Runtime.CompilerServices;

namespace ComprehensiveRelayAPI.Handlers;

// ==================== USER HANDLERS ====================

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, User?>
{
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetUserQueryHandler> _logger;

    public GetUserQueryHandler(DataService dataService, IMemoryCache cache, ILogger<GetUserQueryHandler> logger)
    {
        _dataService = dataService;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<User?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", request.UserId);

        var cacheKey = $"user_{request.UserId}";
        
        if (_cache.TryGetValue(cacheKey, out User? cachedUser))
        {
            _logger.LogDebug("User {UserId} found in cache", request.UserId);
            return cachedUser;
        }

        var user = await _dataService.GetUserAsync(request.UserId);
        
        if (user != null)
        {
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
            _logger.LogDebug("User {UserId} cached for 5 minutes", request.UserId);
        }

        return user;
    }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResponse<User>>
{
    private readonly DataService _dataService;
    private readonly ILogger<GetUsersQueryHandler> _logger;

    public GetUsersQueryHandler(DataService dataService, ILogger<GetUsersQueryHandler> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async ValueTask<PagedResponse<User>> HandleAsync(GetUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting users - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}", 
            request.PageNumber, request.PageSize, request.SearchTerm);

        return await _dataService.GetUsersAsync(
            request.PageNumber, 
            request.PageSize, 
            request.SearchTerm, 
            request.IsActive);
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(DataService dataService, IMemoryCache cache, ILogger<CreateUserCommandHandler> logger)
    {
        _dataService = dataService;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<User> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user: {UserName} ({Email})", request.Name, request.Email);

        var user = await _dataService.CreateUserAsync(
            request.Name, 
            request.Email, 
            request.PhoneNumber, 
            request.Roles);

        // Cache the new user
        var cacheKey = $"user_{user.Id}";
        _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));

        _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);
        return user;
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, User?>
{
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(DataService dataService, IMemoryCache cache, ILogger<UpdateUserCommandHandler> logger)
    {
        _dataService = dataService;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<User?> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user: {UserId}", request.UserId);

        var user = await _dataService.UpdateUserAsync(
            request.UserId,
            request.Name,
            request.Email,
            request.PhoneNumber,
            request.Roles);

        if (user != null)
        {
            // Update cache
            var cacheKey = $"user_{user.Id}";
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
            _logger.LogInformation("User {UserId} updated successfully", user.Id);
        }
        else
        {
            _logger.LogWarning("User {UserId} not found for update", request.UserId);
        }

        return user;
    }
}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(DataService dataService, IMemoryCache cache, ILogger<DeleteUserCommandHandler> logger)
    {
        _dataService = dataService;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<bool> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user: {UserId}", request.UserId);

        var result = await _dataService.DeleteUserAsync(request.UserId);

        if (result)
        {
            // Remove from cache
            var cacheKey = $"user_{request.UserId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("User {UserId} deleted successfully", request.UserId);
        }
        else
        {
            _logger.LogWarning("User {UserId} not found for deletion", request.UserId);
        }

        return result;
    }
}

public class GetUserActivityStreamHandler : IStreamHandler<GetUserActivityStream, string>
{
    private readonly DataService _dataService;
    private readonly ILogger<GetUserActivityStreamHandler> _logger;

    public GetUserActivityStreamHandler(DataService dataService, ILogger<GetUserActivityStreamHandler> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> HandleAsync(GetUserActivityStream request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting activity stream for user: {UserId}", request.UserId);

        await foreach (var activity in _dataService.GetUserActivityStreamAsync(request.UserId, request.FromDate))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Activity stream cancelled for user: {UserId}", request.UserId);
                yield break;
            }

            yield return activity;
        }

        _logger.LogInformation("Activity stream completed for user: {UserId}", request.UserId);
    }
}

// ==================== PRODUCT HANDLERS ====================

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, Product?>
{
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetProductQueryHandler> _logger;

    public GetProductQueryHandler(DataService dataService, IMemoryCache cache, ILogger<GetProductQueryHandler> logger)
    {
        _dataService = dataService;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<Product?> HandleAsync(GetProductQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", request.ProductId);

        var cacheKey = $"product_{request.ProductId}";
        
        if (_cache.TryGetValue(cacheKey, out Product? cachedProduct))
        {
            _logger.LogDebug("Product {ProductId} found in cache", request.ProductId);
            return cachedProduct;
        }

        var product = await _dataService.GetProductAsync(request.ProductId);
        
        if (product != null)
        {
            _cache.Set(cacheKey, product, TimeSpan.FromMinutes(10));
            _logger.LogDebug("Product {ProductId} cached for 10 minutes", request.ProductId);
        }

        return product;
    }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResponse<Product>>
{
    private readonly DataService _dataService;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(DataService dataService, ILogger<GetProductsQueryHandler> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async ValueTask<PagedResponse<Product>> HandleAsync(GetProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting products - Page: {PageNumber}, Size: {PageSize}, Category: {Category}", 
            request.PageNumber, request.PageSize, request.Category);

        return await _dataService.GetProductsAsync(
            request.PageNumber,
            request.PageSize,
            request.Category,
            request.MinPrice,
            request.MaxPrice,
            request.IsActive);
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Product>
{
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(DataService dataService, IMemoryCache cache, ILogger<CreateProductCommandHandler> logger)
    {
        _dataService = dataService;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<Product> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating product: {ProductName}", request.Name);

        var product = await _dataService.CreateProductAsync(
            request.Name,
            request.Description,
            request.Price,
            request.Stock,
            request.Category);

        // Cache the new product
        var cacheKey = $"product_{product.Id}";
        _cache.Set(cacheKey, product, TimeSpan.FromMinutes(10));

        _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
        return product;
    }
}

public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, Product?>
{
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateProductStockCommandHandler> _logger;

    public UpdateProductStockCommandHandler(DataService dataService, IMemoryCache cache, ILogger<UpdateProductStockCommandHandler> logger)
    {
        _dataService = dataService;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<Product?> HandleAsync(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating stock for product: {ProductId} to {NewStock}", request.ProductId, request.NewStock);

        var product = await _dataService.UpdateProductStockAsync(request.ProductId, request.NewStock);

        if (product != null)
        {
            // Update cache
            var cacheKey = $"product_{product.Id}";
            _cache.Set(cacheKey, product, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Product {ProductId} stock updated successfully", product.Id);
        }
        else
        {
            _logger.LogWarning("Product {ProductId} not found for stock update", request.ProductId);
        }

        return product;
    }
}

// ==================== ORDER HANDLERS ====================

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Order?>
{
    private readonly DataService _dataService;
    private readonly ILogger<GetOrderQueryHandler> _logger;

    public GetOrderQueryHandler(DataService dataService, ILogger<GetOrderQueryHandler> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async ValueTask<Order?> HandleAsync(GetOrderQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting order with ID: {OrderId}", request.OrderId);
        return await _dataService.GetOrderAsync(request.OrderId);
    }
}

public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, PagedResponse<Order>>
{
    private readonly DataService _dataService;
    private readonly ILogger<GetUserOrdersQueryHandler> _logger;

    public GetUserOrdersQueryHandler(DataService dataService, ILogger<GetUserOrdersQueryHandler> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async ValueTask<PagedResponse<Order>> HandleAsync(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting orders for user: {UserId}", request.UserId);

        return await _dataService.GetUserOrdersAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize,
            request.Status);
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Order>
{
    private readonly DataService _dataService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(DataService dataService, ILogger<CreateOrderCommandHandler> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async ValueTask<Order> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for user: {UserId} with {ItemCount} items", 
            request.UserId, request.Items.Count);

        var items = request.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        var order = await _dataService.CreateOrderAsync(request.UserId, items, request.Notes);

        _logger.LogInformation("Order created successfully with ID: {OrderId}", order.Id);
        return order;
    }
}

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Order?>
{
    private readonly DataService _dataService;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(DataService dataService, ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async ValueTask<Order?> HandleAsync(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", request.OrderId, request.Status);

        var order = await _dataService.UpdateOrderStatusAsync(request.OrderId, request.Status);

        if (order != null)
        {
            _logger.LogInformation("Order {OrderId} status updated successfully", order.Id);
        }
        else
        {
            _logger.LogWarning("Order {OrderId} not found for status update", request.OrderId);
        }

        return order;
    }
}

// ==================== NOTIFICATION HANDLERS ====================

public class UserCreatedEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<UserCreatedEmailHandler> _logger;

    public UserCreatedEmailHandler(ILogger<UserCreatedEmailHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📧 Sending welcome email to user: {UserName} ({Email})", 
            notification.UserName, notification.Email);

        await Task.Delay(2000, cancellationToken);

        _logger.LogInformation("✅ Welcome email sent successfully to {Email}", notification.Email);
    }
}

public class UserCreatedAnalyticsHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<UserCreatedAnalyticsHandler> _logger;

    public UserCreatedAnalyticsHandler(ILogger<UserCreatedAnalyticsHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📊 Recording user creation analytics for user: {UserId}", notification.UserId);

        await Task.Delay(500, cancellationToken);

        _logger.LogInformation("✅ User creation analytics recorded for user: {UserId}", notification.UserId);
    }
}

public class UserCreatedAuditHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<UserCreatedAuditHandler> _logger;

    public UserCreatedAuditHandler(ILogger<UserCreatedAuditHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📝 Creating audit log entry for user creation: {UserId}", notification.UserId);

        await Task.Delay(100, cancellationToken);

        _logger.LogInformation("✅ Audit log entry created for user: {UserId}", notification.UserId);
    }
}

public class OrderCreatedInventoryHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<OrderCreatedInventoryHandler> _logger;

    public OrderCreatedInventoryHandler(ILogger<OrderCreatedInventoryHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📦 Updating inventory for order: {OrderId}", notification.OrderId);

        await Task.Delay(1000, cancellationToken);

        _logger.LogInformation("✅ Inventory updated for order: {OrderId}", notification.OrderId);
    }
}

public class OrderCreatedPaymentHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<OrderCreatedPaymentHandler> _logger;

    public OrderCreatedPaymentHandler(ILogger<OrderCreatedPaymentHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("💳 Processing payment for order: {OrderId}, Amount: {Amount:C}", 
            notification.OrderId, notification.TotalAmount);

        await Task.Delay(3000, cancellationToken);

        _logger.LogInformation("✅ Payment processed successfully for order: {OrderId}", notification.OrderId);
    }
}

public class OrderCreatedEmailHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<OrderCreatedEmailHandler> _logger;

    public OrderCreatedEmailHandler(ILogger<OrderCreatedEmailHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📧 Sending order confirmation email for order: {OrderId}", notification.OrderId);

        await Task.Delay(1500, cancellationToken);

        _logger.LogInformation("✅ Order confirmation email sent for order: {OrderId}", notification.OrderId);
    }
}

public class OrderStatusChangedCustomerHandler : INotificationHandler<OrderStatusChangedNotification>
{
    private readonly ILogger<OrderStatusChangedCustomerHandler> _logger;

    public OrderStatusChangedCustomerHandler(ILogger<OrderStatusChangedCustomerHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(OrderStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📱 Notifying customer about order status change: {OrderId} ({OldStatus} → {NewStatus})",
            notification.OrderId, notification.OldStatus, notification.NewStatus);

        await Task.Delay(800, cancellationToken);

        _logger.LogInformation("✅ Customer notified about order status change: {OrderId}", notification.OrderId);
    }
}

public class OrderStatusChangedLogisticsHandler : INotificationHandler<OrderStatusChangedNotification>
{
    private readonly ILogger<OrderStatusChangedLogisticsHandler> _logger;

    public OrderStatusChangedLogisticsHandler(ILogger<OrderStatusChangedLogisticsHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(OrderStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.NewStatus == Models.OrderStatus.Shipped)
        {
            _logger.LogInformation("🚚 Updating logistics system for shipped order: {OrderId}", notification.OrderId);

            await Task.Delay(2000, cancellationToken);

            _logger.LogInformation("✅ Logistics system updated for order: {OrderId}", notification.OrderId);
        }
    }
}

public class OrderStatusChangedAnalyticsHandler : INotificationHandler<OrderStatusChangedNotification>
{
    private readonly ILogger<OrderStatusChangedAnalyticsHandler> _logger;

    public OrderStatusChangedAnalyticsHandler(ILogger<OrderStatusChangedAnalyticsHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(OrderStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📊 Recording order status analytics: {OrderId} ({OldStatus} → {NewStatus})",
            notification.OrderId, notification.OldStatus, notification.NewStatus);

        await Task.Delay(300, cancellationToken);

        _logger.LogInformation("✅ Order status analytics recorded for order: {OrderId}", notification.OrderId);
    }
}