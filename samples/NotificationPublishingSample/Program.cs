using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;

namespace Relay.NotificationPublishing.Example
{
    // Domain events (notifications)
    public record UserRegisteredEvent(int UserId, string Email, string Name) : INotification;
    public record OrderPlacedEvent(int OrderId, int UserId, decimal Amount) : INotification;
    public record PaymentProcessedEvent(int OrderId, string TransactionId, decimal Amount) : INotification;

    // Multiple handlers for UserRegisteredEvent
    public class EmailNotificationHandler
    {
        [Notification(Priority = 1)] // Higher priority = executes first
        public async ValueTask HandleAsync(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  📧 [Priority 1] Sending welcome email to {notification.Email}...");
            await Task.Delay(300, cancellationToken);
            Console.WriteLine($"     ✅ Welcome email sent!");
        }
    }

    public class AnalyticsHandler
    {
        [Notification(Priority = 0, DispatchMode = NotificationDispatchMode.Parallel)]
        public async ValueTask HandleAsync(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  📊 [Priority 0, Parallel] Recording user registration in analytics...");
            await Task.Delay(500, cancellationToken);
            Console.WriteLine($"     ✅ Analytics recorded!");
        }
    }

    public class WelcomePackageHandler
    {
        [Notification(Priority = 2)]
        public async ValueTask HandleAsync(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  🎁 [Priority 2] Preparing welcome package for {notification.Name}...");
            await Task.Delay(200, cancellationToken);
            Console.WriteLine($"     ✅ Welcome package prepared!");
        }
    }

    public class CrmSyncHandler
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public async ValueTask HandleAsync(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  🔄 [Parallel] Syncing user to CRM system...");
            await Task.Delay(400, cancellationToken);
            Console.WriteLine($"     ✅ CRM synced!");
        }
    }

    // Handlers for OrderPlacedEvent
    public class OrderConfirmationHandler
    {
        [Notification(Priority = 10)]
        public async ValueTask HandleAsync(OrderPlacedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  📧 [Priority 10] Sending order confirmation for Order #{notification.OrderId}...");
            await Task.Delay(200, cancellationToken);
            Console.WriteLine($"     ✅ Order confirmation sent!");
        }
    }

    public class InventoryHandler
    {
        [Notification(Priority = 9)]
        public async ValueTask HandleAsync(OrderPlacedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  📦 [Priority 9] Reserving inventory for Order #{notification.OrderId}...");
            await Task.Delay(300, cancellationToken);
            Console.WriteLine($"     ✅ Inventory reserved!");
        }
    }

    public class LoyaltyPointsHandler
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public async ValueTask HandleAsync(OrderPlacedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  🌟 [Parallel] Calculating loyalty points...");
            await Task.Delay(250, cancellationToken);
            var points = (int)(notification.Amount * 0.1m);
            Console.WriteLine($"     ✅ {points} loyalty points added!");
        }
    }

    // Multiple handlers for PaymentProcessedEvent
    public class PaymentNotificationHandler
    {
        [Notification]
        public async ValueTask HandleAsync(PaymentProcessedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  💳 Sending payment receipt for transaction {notification.TransactionId}...");
            await Task.Delay(200, cancellationToken);
            Console.WriteLine($"     ✅ Payment receipt sent!");
        }
    }

    public class AccountingHandler
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public async ValueTask HandleAsync(PaymentProcessedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  📊 [Parallel] Recording in accounting system...");
            await Task.Delay(400, cancellationToken);
            Console.WriteLine($"     ✅ Accounting entry created!");
        }
    }

    public class FraudDetectionHandler
    {
        [Notification(Priority = 100, DispatchMode = NotificationDispatchMode.Parallel)]
        public async ValueTask HandleAsync(PaymentProcessedEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  🔍 [Priority 100, Parallel] Running fraud detection...");
            await Task.Delay(600, cancellationToken);
            Console.WriteLine($"     ✅ Fraud check passed!");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Relay Notification Publishing Sample");
            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine();
            Console.WriteLine("Demonstrates Event-Driven Architecture with multiple handlers");
            Console.WriteLine("Priority-based execution and Parallel/Sequential dispatch modes");
            Console.WriteLine();

            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddRelayConfiguration();
                
                // Register notification handlers
                services.AddScoped<EmailNotificationHandler>();
                services.AddScoped<AnalyticsHandler>();
                services.AddScoped<WelcomePackageHandler>();
                services.AddScoped<CrmSyncHandler>();
                
                services.AddScoped<OrderConfirmationHandler>();
                services.AddScoped<InventoryHandler>();
                services.AddScoped<LoyaltyPointsHandler>();
                
                services.AddScoped<PaymentNotificationHandler>();
                services.AddScoped<AccountingHandler>();
                services.AddScoped<FraudDetectionHandler>();
            });

            var host = builder.Build();
            var relay = host.Services.GetRequiredService<IRelay>();

            // Example 1: User registration with multiple handlers
            Console.WriteLine("📊 Example 1: User Registration Event");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Publishing UserRegisteredEvent with 4 handlers...");
            Console.WriteLine();

            var userEvent = new UserRegisteredEvent(
                UserId: 12345,
                Email: "john.doe@example.com",
                Name: "John Doe"
            );

            var startTime = DateTime.UtcNow;
            await relay.PublishAsync(userEvent);
            var duration = DateTime.UtcNow - startTime;

            Console.WriteLine();
            Console.WriteLine($"✅ All handlers completed in {duration.TotalMilliseconds:F0}ms");
            Console.WriteLine();
            Console.WriteLine("Handler Execution Order:");
            Console.WriteLine("  1. WelcomePackageHandler (Priority 2, Sequential)");
            Console.WriteLine("  2. EmailNotificationHandler (Priority 1, Sequential)");
            Console.WriteLine("  3. AnalyticsHandler (Priority 0, Parallel)");
            Console.WriteLine("  4. CrmSyncHandler (Default Priority, Parallel)");

            Console.WriteLine();
            await Task.Delay(1000);

            // Example 2: Order placed event
            Console.WriteLine("📊 Example 2: Order Placed Event");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Publishing OrderPlacedEvent with 3 handlers...");
            Console.WriteLine();

            var orderEvent = new OrderPlacedEvent(
                OrderId: 98765,
                UserId: 12345,
                Amount: 149.99m
            );

            startTime = DateTime.UtcNow;
            await relay.PublishAsync(orderEvent);
            duration = DateTime.UtcNow - startTime;

            Console.WriteLine();
            Console.WriteLine($"✅ All handlers completed in {duration.TotalMilliseconds:F0}ms");

            Console.WriteLine();
            await Task.Delay(1000);

            // Example 3: Payment processed event
            Console.WriteLine("📊 Example 3: Payment Processed Event");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Publishing PaymentProcessedEvent with 3 handlers...");
            Console.WriteLine();

            var paymentEvent = new PaymentProcessedEvent(
                OrderId: 98765,
                TransactionId: "TXN-" + Guid.NewGuid().ToString("N")[..16],
                Amount: 149.99m
            );

            startTime = DateTime.UtcNow;
            await relay.PublishAsync(paymentEvent);
            duration = DateTime.UtcNow - startTime;

            Console.WriteLine();
            Console.WriteLine($"✅ All handlers completed in {duration.TotalMilliseconds:F0}ms");

            Console.WriteLine();
            await Task.Delay(1000);

            // Example 4: Multiple events in sequence
            Console.WriteLine("📊 Example 4: Event Chain (User → Order → Payment)");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Simulating complete user journey...");
            Console.WriteLine();

            var chainStart = DateTime.UtcNow;

            // 1. User registers
            Console.WriteLine("1️⃣  User Registration:");
            await relay.PublishAsync(new UserRegisteredEvent(54321, "jane@example.com", "Jane Smith"));
            await Task.Delay(500);

            // 2. User places order
            Console.WriteLine("\n2️⃣  Order Placement:");
            await relay.PublishAsync(new OrderPlacedEvent(11111, 54321, 299.99m));
            await Task.Delay(500);

            // 3. Payment processed
            Console.WriteLine("\n3️⃣  Payment Processing:");
            await relay.PublishAsync(new PaymentProcessedEvent(11111, "TXN-ABC123", 299.99m));

            var chainDuration = DateTime.UtcNow - chainStart;

            Console.WriteLine();
            Console.WriteLine($"✅ Complete user journey processed in {chainDuration.TotalSeconds:F1}s");

            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine("✅ All notification publishing examples completed!");
            Console.WriteLine();
            Console.WriteLine("Key Features Demonstrated:");
            Console.WriteLine("  • Multiple handlers for single event");
            Console.WriteLine("  • Priority-based handler execution");
            Console.WriteLine("  • Sequential execution (ordered by priority)");
            Console.WriteLine("  • Parallel execution (for independent handlers)");
            Console.WriteLine("  • Event-driven architecture");
            Console.WriteLine("  • Domain events pattern");
            Console.WriteLine("  • Fire-and-forget notifications");
            Console.WriteLine();
            Console.WriteLine("Dispatch Modes:");
            Console.WriteLine("  • Sequential: Handlers execute in priority order, one after another");
            Console.WriteLine("  • Parallel: Handlers execute concurrently for better performance");
            Console.WriteLine();
            Console.WriteLine("Use Cases:");
            Console.WriteLine("  • Event sourcing");
            Console.WriteLine("  • Microservices communication");
            Console.WriteLine("  • Cross-cutting concerns (logging, audit, analytics)");
            Console.WriteLine("  • Multi-step business processes");
            Console.WriteLine("  • Decoupled system components");
            Console.WriteLine("  • Event-driven workflows");
        }
    }
}

