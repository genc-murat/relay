using ComprehensiveRelayAPI.Models;
using System.Collections.Concurrent;

namespace ComprehensiveRelayAPI.Services;

/// <summary>
/// In-memory data service for demonstration purposes
/// In a real application, this would be replaced with Entity Framework or similar
/// </summary>
public class DataService
{
    private static int _nextUserId = 1;
    private static int _nextProductId = 1;
    private static int _nextOrderId = 1;
    private static int _nextOrderItemId = 1;

    private readonly ConcurrentDictionary<int, User> _users = new();
    private readonly ConcurrentDictionary<int, Product> _products = new();
    private readonly ConcurrentDictionary<int, Order> _orders = new();

    public DataService()
    {
        SeedData();
    }

    private void SeedData()
    {
        // Seed Users
        var users = new[]
        {
            new User { Id = _nextUserId++, Name = "Murat Genç", Email = "murat@example.com", Roles = ["Admin", "User"] },
            new User { Id = _nextUserId++, Name = "Ayşe Yılmaz", Email = "ayse@example.com", Roles = ["User"] },
            new User { Id = _nextUserId++, Name = "Mehmet Kaya", Email = "mehmet@example.com", Roles = ["Manager", "User"] }
        };

        foreach (var user in users)
            _users[user.Id] = user;

        // Seed Products
        var products = new[]
        {
            new Product { Id = _nextProductId++, Name = "Laptop", Description = "Gaming Laptop", Price = 15000, Stock = 10, Category = "Electronics" },
            new Product { Id = _nextProductId++, Name = "Mouse", Description = "Wireless Mouse", Price = 250, Stock = 50, Category = "Electronics" },
            new Product { Id = _nextProductId++, Name = "Keyboard", Description = "Mechanical Keyboard", Price = 800, Stock = 25, Category = "Electronics" },
            new Product { Id = _nextProductId++, Name = "Book", Description = "Programming Book", Price = 120, Stock = 100, Category = "Books" }
        };

        foreach (var product in products)
            _products[product.Id] = product;

        // Seed Orders
        var order = new Order
        {
            Id = _nextOrderId++,
            UserId = 1,
            User = _users[1],
            Status = OrderStatus.Processing,
            Items = new List<OrderItem>
            {
                new() { Id = _nextOrderItemId++, ProductId = 1, Product = _products[1], Quantity = 1, UnitPrice = 15000 },
                new() { Id = _nextOrderItemId++, ProductId = 2, Product = _products[2], Quantity = 2, UnitPrice = 250 }
            }
        };
        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
        _orders[order.Id] = order;
    }

    // ==================== USER OPERATIONS ====================

    public async Task<User?> GetUserAsync(int userId)
    {
        await Task.Delay(50); // Simulate database delay
        return _users.TryGetValue(userId, out var user) ? user : null;
    }

    public async Task<PagedResponse<User>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm, bool? isActive)
    {
        await Task.Delay(100); // Simulate database delay

        var query = _users.Values.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => u.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResponse<User>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<User> CreateUserAsync(string name, string email, string? phoneNumber, List<string>? roles)
    {
        await Task.Delay(200); // Simulate database delay

        var user = new User
        {
            Id = Interlocked.Increment(ref _nextUserId),
            Name = name,
            Email = email,
            PhoneNumber = phoneNumber,
            Roles = roles ?? new List<string> { "User" }
        };

        _users[user.Id] = user;
        return user;
    }

    public async Task<User?> UpdateUserAsync(int userId, string name, string email, string? phoneNumber, List<string>? roles)
    {
        await Task.Delay(150); // Simulate database delay

        if (!_users.TryGetValue(userId, out var user))
            return null;

        user.Name = name;
        user.Email = email;
        user.PhoneNumber = phoneNumber;
        user.Roles = roles ?? user.Roles;
        user.UpdatedAt = DateTime.UtcNow;

        return user;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        await Task.Delay(100); // Simulate database delay
        return _users.TryRemove(userId, out _);
    }

    // ==================== PRODUCT OPERATIONS ====================

    public async Task<Product?> GetProductAsync(int productId)
    {
        await Task.Delay(50); // Simulate database delay
        return _products.TryGetValue(productId, out var product) ? product : null;
    }

    public async Task<PagedResponse<Product>> GetProductsAsync(int pageNumber, int pageSize, string? category, decimal? minPrice, decimal? maxPrice, bool? isActive)
    {
        await Task.Delay(100); // Simulate database delay

        var query = _products.Values.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResponse<Product>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<Product> CreateProductAsync(string name, string? description, decimal price, int stock, string category)
    {
        await Task.Delay(200); // Simulate database delay

        var product = new Product
        {
            Id = Interlocked.Increment(ref _nextProductId),
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            Category = category
        };

        _products[product.Id] = product;
        return product;
    }

    public async Task<Product?> UpdateProductStockAsync(int productId, int newStock)
    {
        await Task.Delay(100); // Simulate database delay

        if (!_products.TryGetValue(productId, out var product))
            return null;

        product.Stock = newStock;
        return product;
    }

    // ==================== ORDER OPERATIONS ====================

    public async Task<Order?> GetOrderAsync(int orderId)
    {
        await Task.Delay(50); // Simulate database delay
        return _orders.TryGetValue(orderId, out var order) ? order : null;
    }

    public async Task<PagedResponse<Order>> GetUserOrdersAsync(int userId, int pageNumber, int pageSize, OrderStatus? status)
    {
        await Task.Delay(100); // Simulate database delay

        var query = _orders.Values.Where(o => o.UserId == userId).AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResponse<Order>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<Order> CreateOrderAsync(int userId, List<(int ProductId, int Quantity)> items, string? notes)
    {
        await Task.Delay(300); // Simulate database delay

        var user = await GetUserAsync(userId);
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var (productId, quantity) in items)
        {
            var product = await GetProductAsync(productId);
            if (product != null)
            {
                var orderItem = new OrderItem
                {
                    Id = Interlocked.Increment(ref _nextOrderItemId),
                    ProductId = productId,
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };
                orderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;

                // Update stock
                product.Stock = Math.Max(0, product.Stock - quantity);
            }
        }

        var order = new Order
        {
            Id = Interlocked.Increment(ref _nextOrderId),
            UserId = userId,
            User = user,
            Items = orderItems,
            TotalAmount = totalAmount,
            Notes = notes
        };

        _orders[order.Id] = order;
        return order;
    }

    public async Task<Order?> UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        await Task.Delay(100); // Simulate database delay

        if (!_orders.TryGetValue(orderId, out var order))
            return null;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        return order;
    }

    // ==================== STREAMING OPERATIONS ====================

    public async IAsyncEnumerable<string> GetUserActivityStreamAsync(int userId, DateTime? fromDate)
    {
        var activities = new[]
        {
            "User logged in",
            "User viewed profile",
            "User updated profile",
            "User created order",
            "User viewed orders",
            "User logged out"
        };

        foreach (var activity in activities)
        {
            await Task.Delay(500); // Simulate delay between activities
            yield return $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - User {userId}: {activity}";
        }
    }
}