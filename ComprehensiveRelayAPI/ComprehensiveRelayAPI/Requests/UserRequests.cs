using ComprehensiveRelayAPI.Models;
using FluentValidation;
using Relay.Core;

namespace ComprehensiveRelayAPI.Requests;

// ==================== USER REQUESTS ====================

/// <summary>
/// Query to get a user by ID
/// </summary>
public record GetUserQuery(int UserId) : IRequest<User?>;

/// <summary>
/// Query to get users with pagination and filtering
/// </summary>
public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    bool? IsActive = null
) : IRequest<PagedResponse<User>>;

/// <summary>
/// Command to create a new user
/// </summary>
public record CreateUserCommand(
    string Name,
    string Email,
    string? PhoneNumber = null,
    List<string>? Roles = null
) : IRequest<User>;

/// <summary>
/// Command to update an existing user
/// </summary>
public record UpdateUserCommand(
    int UserId,
    string Name,
    string Email,
    string? PhoneNumber = null,
    List<string>? Roles = null
) : IRequest<User?>;

/// <summary>
/// Command to delete a user
/// </summary>
public record DeleteUserCommand(int UserId) : IRequest<bool>;

/// <summary>
/// Streaming query to get user activity logs
/// </summary>
public record GetUserActivityStream(int UserId, DateTime? FromDate = null) : IStreamRequest<string>;

// ==================== PRODUCT REQUESTS ====================

/// <summary>
/// Query to get a product by ID
/// </summary>
public record GetProductQuery(int ProductId) : IRequest<Product?>;

/// <summary>
/// Query to get products with pagination and filtering
/// </summary>
public record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? IsActive = null
) : IRequest<PagedResponse<Product>>;

/// <summary>
/// Command to create a new product
/// </summary>
public record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Category
) : IRequest<Product>;

/// <summary>
/// Command to update product stock
/// </summary>
public record UpdateProductStockCommand(int ProductId, int NewStock) : IRequest<Product?>;

// ==================== ORDER REQUESTS ====================

/// <summary>
/// Query to get an order by ID
/// </summary>
public record GetOrderQuery(int OrderId) : IRequest<Order?>;

/// <summary>
/// Query to get orders for a user
/// </summary>
public record GetUserOrdersQuery(
    int UserId,
    int PageNumber = 1,
    int PageSize = 10,
    OrderStatus? Status = null
) : IRequest<PagedResponse<Order>>;

/// <summary>
/// Command to create a new order
/// </summary>
public record CreateOrderCommand(
    int UserId,
    List<OrderItemRequest> Items,
    string? Notes = null
) : IRequest<Order>;

/// <summary>
/// Command to update order status
/// </summary>
public record UpdateOrderStatusCommand(int OrderId, OrderStatus Status) : IRequest<Order?>;

/// <summary>
/// Order item request DTO
/// </summary>
public record OrderItemRequest(int ProductId, int Quantity);

// ==================== NOTIFICATIONS ====================

/// <summary>
/// Notification sent when a user is created
/// </summary>
public record UserCreatedNotification(int UserId, string UserName, string Email) : INotification;

/// <summary>
/// Notification sent when an order is created
/// </summary>
public record OrderCreatedNotification(int OrderId, int UserId, decimal TotalAmount) : INotification;

/// <summary>
/// Notification sent when order status changes
/// </summary>
public record OrderStatusChangedNotification(int OrderId, OrderStatus OldStatus, OrderStatus NewStatus) : INotification;

// ==================== VALIDATION RULES ====================

/// <summary>
/// Validator for CreateUserCommand
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

/// <summary>
/// Validator for CreateProductCommand
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .Length(3, 200).WithMessage("Product name must be between 3 and 200 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required");
    }
}

/// <summary>
/// Validator for CreateOrderCommand
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Valid User ID is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("Valid Product ID is required");
            
            item.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");
        });
    }
}