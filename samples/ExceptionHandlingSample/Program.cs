using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core;
using Relay.Core.Pipeline;

namespace ExceptionHandlingSample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Relay Exception Handling Sample ===\n");

        // Setup dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add Relay core services
        services.AddSingleton<IRelay, SimpleRelay>();

        // Add handlers
        services.AddTransient<ProcessPaymentHandler>();
        services.AddTransient<GetUserHandler>();

        // Add exception handling behaviors
        services.AddRelayExceptionHandlers();

        // Register exception handlers
        services.AddExceptionHandler<ProcessPaymentCommand, PaymentResult, InsufficientFundsException, InsufficientFundsHandler>();
        services.AddExceptionHandler<ProcessPaymentCommand, PaymentResult, InvalidCardException, InvalidCardHandler>();
        services.AddExceptionHandler<GetUserCommand, User, UserNotFoundException, UserNotFoundHandler>();

        // Register exception actions (for logging/monitoring)
        services.AddExceptionAction<ProcessPaymentCommand, PaymentException, PaymentExceptionLogger>();
        services.AddExceptionAction<GetUserCommand, Exception, GeneralExceptionLogger>();

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();

        // Demo 1: Handle insufficient funds exception
        Console.WriteLine("1. Processing payment with insufficient funds...\n");
        var payment1 = new ProcessPaymentCommand("1234-5678", 1000.00m, "John Doe");
        var result1 = await relay.SendAsync(payment1);
        Console.WriteLine($"Result: {result1.Status} - {result1.Message}\n");

        Console.WriteLine(new string('-', 60));

        // Demo 2: Handle invalid card exception
        Console.WriteLine("\n2. Processing payment with invalid card...\n");
        var payment2 = new ProcessPaymentCommand("INVALID", 50.00m, "Jane Smith");
        var result2 = await relay.SendAsync(payment2);
        Console.WriteLine($"Result: {result2.Status} - {result2.Message}\n");

        Console.WriteLine(new string('-', 60));

        // Demo 3: Handle user not found exception
        Console.WriteLine("\n3. Getting non-existent user...\n");
        var getUserCmd = new GetUserCommand(999);
        var userResult = await relay.SendAsync(getUserCmd);
        Console.WriteLine($"User: {userResult?.Name ?? "Not found"} (ID: {userResult?.Id ?? 0})\n");

        Console.WriteLine(new string('-', 60));

        // Demo 4: Successful payment (no exception)
        Console.WriteLine("\n4. Processing successful payment...\n");
        var payment3 = new ProcessPaymentCommand("VALID-CARD", 50.00m, "Alice Johnson");
        var result3 = await relay.SendAsync(payment3);
        Console.WriteLine($"Result: {result3.Status} - {result3.Message}\n");

        Console.WriteLine("\n=== Sample Completed ===");
    }
}

#region Models and Exceptions

public record User(int Id, string Name, string Email);

public record ProcessPaymentCommand(string CardNumber, decimal Amount, string CustomerName) : IRequest<PaymentResult>;

public record GetUserCommand(int UserId) : IRequest<User>;

public record PaymentResult(string Status, string Message, string? TransactionId = null);

// Exception hierarchy
public class PaymentException : Exception
{
    public PaymentException(string message) : base(message) { }
}

public class InsufficientFundsException : PaymentException
{
    public decimal RequiredAmount { get; }
    public decimal AvailableAmount { get; }

    public InsufficientFundsException(decimal required, decimal available)
        : base($"Insufficient funds. Required: {required:C}, Available: {available:C}")
    {
        RequiredAmount = required;
        AvailableAmount = available;
    }
}

public class InvalidCardException : PaymentException
{
    public string CardNumber { get; }

    public InvalidCardException(string cardNumber)
        : base($"Invalid card number: {cardNumber}")
    {
        CardNumber = cardNumber;
    }
}

public class UserNotFoundException : Exception
{
    public int UserId { get; }

    public UserNotFoundException(int userId)
        : base($"User with ID {userId} not found")
    {
        UserId = userId;
    }
}

#endregion

#region Handlers

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, PaymentResult>
{
    public ValueTask<PaymentResult> HandleAsync(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [Handler] Processing payment for {request.CustomerName}");
        Console.WriteLine($"  [Handler] Card: {request.CardNumber}, Amount: {request.Amount:C}");

        // Simulate different scenarios
        if (request.CardNumber == "INVALID" || request.CardNumber.Length < 10)
        {
            throw new InvalidCardException(request.CardNumber);
        }

        if (request.Amount > 500)
        {
            throw new InsufficientFundsException(request.Amount, 500);
        }

        if (request.CardNumber == "VALID-CARD")
        {
            var result = new PaymentResult(
                "Success",
                $"Payment of {request.Amount:C} processed successfully",
                Guid.NewGuid().ToString("N")[..8]);
            return new ValueTask<PaymentResult>(result);
        }

        throw new PaymentException("Payment processing failed");
    }
}

public class GetUserHandler : IRequestHandler<GetUserCommand, User>
{
    private static readonly Dictionary<int, User> _users = new()
    {
        { 1, new User(1, "John Doe", "john@example.com") },
        { 2, new User(2, "Jane Smith", "jane@example.com") }
    };

    public ValueTask<User> HandleAsync(GetUserCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [Handler] Looking up user ID: {request.UserId}");

        if (!_users.TryGetValue(request.UserId, out var user))
        {
            throw new UserNotFoundException(request.UserId);
        }

        return new ValueTask<User>(user);
    }
}

#endregion

#region Exception Handlers

public class InsufficientFundsHandler : IRequestExceptionHandler<ProcessPaymentCommand, PaymentResult, InsufficientFundsException>
{
    public ValueTask<ExceptionHandlerResult<PaymentResult>> HandleAsync(
        ProcessPaymentCommand request,
        InsufficientFundsException exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [ExceptionHandler] Handling insufficient funds");
        Console.WriteLine($"  [ExceptionHandler] Required: {exception.RequiredAmount:C}, Available: {exception.AvailableAmount:C}");

        var result = new PaymentResult(
            "InsufficientFunds",
            $"Payment declined. Available balance: {exception.AvailableAmount:C}");

        return new ValueTask<ExceptionHandlerResult<PaymentResult>>(
            ExceptionHandlerResult<PaymentResult>.Handle(result));
    }
}

public class InvalidCardHandler : IRequestExceptionHandler<ProcessPaymentCommand, PaymentResult, InvalidCardException>
{
    public ValueTask<ExceptionHandlerResult<PaymentResult>> HandleAsync(
        ProcessPaymentCommand request,
        InvalidCardException exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [ExceptionHandler] Handling invalid card");
        Console.WriteLine($"  [ExceptionHandler] Card: {exception.CardNumber}");

        var result = new PaymentResult(
            "InvalidCard",
            "Payment declined. Please check your card number.");

        return new ValueTask<ExceptionHandlerResult<PaymentResult>>(
            ExceptionHandlerResult<PaymentResult>.Handle(result));
    }
}

public class UserNotFoundHandler : IRequestExceptionHandler<GetUserCommand, User, UserNotFoundException>
{
    public ValueTask<ExceptionHandlerResult<User>> HandleAsync(
        GetUserCommand request,
        UserNotFoundException exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [ExceptionHandler] User not found: {exception.UserId}");
        Console.WriteLine($"  [ExceptionHandler] Returning default user");

        var defaultUser = new User(0, "Unknown User", "unknown@example.com");

        return new ValueTask<ExceptionHandlerResult<User>>(
            ExceptionHandlerResult<User>.Handle(defaultUser));
    }
}

#endregion

#region Exception Actions

public class PaymentExceptionLogger : IRequestExceptionAction<ProcessPaymentCommand, PaymentException>
{
    public ValueTask ExecuteAsync(ProcessPaymentCommand request, PaymentException exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [ExceptionAction - Monitoring] Payment exception occurred");
        Console.WriteLine($"  [ExceptionAction - Monitoring] Customer: {request.CustomerName}");
        Console.WriteLine($"  [ExceptionAction - Monitoring] Exception: {exception.GetType().Name}");
        Console.WriteLine($"  [ExceptionAction - Monitoring] Logging to monitoring system...");

        // In real app: send to monitoring system, increment metrics, etc.
        return default;
    }
}

public class GeneralExceptionLogger : IRequestExceptionAction<GetUserCommand, Exception>
{
    public ValueTask ExecuteAsync(GetUserCommand request, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [ExceptionAction - General] Exception occurred");
        Console.WriteLine($"  [ExceptionAction - General] Request: GetUserCommand(UserId={request.UserId})");
        Console.WriteLine($"  [ExceptionAction - General] Exception Type: {exception.GetType().Name}");

        return default;
    }
}

#endregion

#region Simple Relay Implementation for Demo

public class SimpleRelay : IRelay
{
    private readonly IServiceProvider _serviceProvider;

    public SimpleRelay(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Get exception behaviors
        var exceptionHandlerBehavior = ActivatorUtilities.CreateInstance<RequestExceptionHandlerBehavior<IRequest<TResponse>, TResponse>>(_serviceProvider);
        var exceptionActionBehavior = ActivatorUtilities.CreateInstance<RequestExceptionActionBehavior<IRequest<TResponse>, TResponse>>(_serviceProvider);

        // Execute pipeline: ExceptionHandler -> ExceptionAction -> Handler
        var response = await exceptionHandlerBehavior.HandleAsync(
            request,
            async () => await exceptionActionBehavior.HandleAsync(
                request,
                async () =>
                {
                    // Execute actual handler
                    if (request is ProcessPaymentCommand paymentCmd)
                    {
                        var handler = _serviceProvider.GetRequiredService<ProcessPaymentHandler>();
                        return (TResponse)(object)(await handler.HandleAsync(paymentCmd, cancellationToken));
                    }
                    else if (request is GetUserCommand getUserCmd)
                    {
                        var handler = _serviceProvider.GetRequiredService<GetUserHandler>();
                        return (TResponse)(object)(await handler.HandleAsync(getUserCmd, cancellationToken))!;
                    }
                    throw new NotSupportedException($"Handler not found for {request.GetType().Name}");
                },
                cancellationToken),
            cancellationToken);

        return response;
    }

    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        throw new NotImplementedException();
    }
}

#endregion
