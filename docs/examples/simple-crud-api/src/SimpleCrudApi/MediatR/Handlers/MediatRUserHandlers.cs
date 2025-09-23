using MediatR;
using SimpleCrudApi.Data;
using SimpleCrudApi.Models;
using SimpleCrudApi.MediatR.Requests;

namespace SimpleCrudApi.MediatR.Handlers;

public class MediatRUserHandlers :
    IRequestHandler<MediatRGetUserQuery, User?>,
    IRequestHandler<MediatRGetUsersQuery, IEnumerable<User>>,
    IRequestHandler<MediatRCreateUserCommand, User>,
    IRequestHandler<MediatRUpdateUserCommand, User?>,
    IRequestHandler<MediatRDeleteUserCommand>
{
    private readonly IUserRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<MediatRUserHandlers> _logger;

    public MediatRUserHandlers(IUserRepository repository, IMediator mediator, ILogger<MediatRUserHandlers> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<User?> Handle(MediatRGetUserQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", request.Id);
        return await _repository.GetByIdAsync(request.Id, cancellationToken);
    }

    public async Task<IEnumerable<User>> Handle(MediatRGetUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting users - Page: {Page}, PageSize: {PageSize}",
            request.Page, request.PageSize);
        return await _repository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async Task<User> Handle(MediatRCreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user: {Name} ({Email})", request.Name, request.Email);

        var user = new User
        {
            Id = 0, // Will be set by repository
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _repository.CreateAsync(user, cancellationToken);

        // Publish notification
        await _mediator.Publish(new MediatRUserCreatedNotification(createdUser), cancellationToken);

        return createdUser;
    }

    public async Task<User?> Handle(MediatRUpdateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user: {UserId}", request.Id);

        var existingUser = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existingUser == null)
            return null;

        var updatedUser = existingUser with
        {
            Name = request.Name,
            Email = request.Email,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _repository.UpdateAsync(updatedUser, cancellationToken);

        if (result != null)
        {
            await _mediator.Publish(new MediatRUserUpdatedNotification(result), cancellationToken);
        }

        return result;
    }

    public async Task Handle(MediatRDeleteUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user: {UserId}", request.Id);

        var deleted = await _repository.DeleteAsync(request.Id, cancellationToken);

        if (deleted)
        {
            await _mediator.Publish(new MediatRUserDeletedNotification(request.Id), cancellationToken);
        }
    }
}

public class MediatRUserNotificationHandlers :
    INotificationHandler<MediatRUserCreatedNotification>,
    INotificationHandler<MediatRUserUpdatedNotification>,
    INotificationHandler<MediatRUserDeletedNotification>
{
    private readonly ILogger<MediatRUserNotificationHandlers> _logger;

    public MediatRUserNotificationHandlers(ILogger<MediatRUserNotificationHandlers> logger)
    {
        _logger = logger;
    }

    public async Task Handle(MediatRUserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId} - {Name}",
            notification.User.Id, notification.User.Name);

        // Could send welcome email, update analytics, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }

    public async Task Handle(MediatRUserUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User updated: {UserId} - {Name}",
            notification.User.Id, notification.User.Name);

        // Could invalidate cache, update search index, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }

    public async Task Handle(MediatRUserDeletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User deleted: {UserId}", notification.UserId);

        // Could clean up related data, update analytics, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }
}