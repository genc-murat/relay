using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing.Sample;

/// <summary>
/// Sample request handlers for the testing framework demonstration.
/// </summary>
public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public CreateUserHandler(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async ValueTask<User> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email
        };

        var createdUser = await _userRepository.CreateAsync(user);
        await _emailService.SendWelcomeEmailAsync(createdUser.Email, createdUser.Name);

        // Publish event
        await Task.CompletedTask; // In real implementation, this would publish UserCreatedEvent

        return createdUser;
    }
}

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, User>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public UpdateUserHandler(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async ValueTask<User> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByIdAsync(request.UserId);
        if (existingUser == null)
        {
            throw new InvalidOperationException("User not found");
        }

        existingUser.Name = request.Name;
        existingUser.Email = request.Email;

        var updatedUser = await _userRepository.UpdateAsync(existingUser);
        await _emailService.SendUserUpdatedEmailAsync(updatedUser.Email, updatedUser.Name);

        // Publish event
        await Task.CompletedTask; // In real implementation, this would publish UserUpdatedEvent

        return updatedUser;
    }
}

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, User>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async ValueTask<User> HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        return user;
    }
}

public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, List<User>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async ValueTask<List<User>> HandleAsync(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userRepository.GetAllAsync();
    }
}

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Product>
{
    private readonly IProductRepository _productRepository;

    public CreateProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async ValueTask<Product> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        var createdProduct = await _productRepository.CreateAsync(product);

        // Publish event
        await Task.CompletedTask; // In real implementation, this would publish ProductCreatedEvent

        return createdProduct;
    }
}

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Product>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async ValueTask<Product> HandleAsync(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Product not found");
        }

        return product;
    }
}