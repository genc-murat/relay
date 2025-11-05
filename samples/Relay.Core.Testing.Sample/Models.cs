using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing.Sample;

/// <summary>
/// Sample domain models for the testing framework demonstration.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>
/// Commands
/// </summary>
public class CreateUserCommand : IRequest<User>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateUserCommand : IRequest<User>
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateProductCommand : IRequest<Product>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}

/// <summary>
/// Queries
/// </summary>
public class GetUserByIdQuery : IRequest<User>
{
    public Guid UserId { get; set; }
}

public class GetProductByIdQuery : IRequest<Product>
{
    public Guid ProductId { get; set; }
}

public class GetAllUsersQuery : IRequest<List<User>>
{
}

/// <summary>
/// Events
/// </summary>
public class UserCreatedEvent : INotification
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ProductCreatedEvent : INotification
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class UserUpdatedEvent : INotification
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}