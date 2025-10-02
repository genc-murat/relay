using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;

namespace Relay.WorkflowDemo.Example
{
    // Simple workflow simulation using Relay requests
    public record ProcessOrderCommand(int OrderId, decimal Amount, string CustomerEmail) : IRequest<OrderResult>;
    public record OrderResult(int OrderId, string Status, List<string> Steps);

    public class OrderWorkflowService
    {
        [Handle]
        public async ValueTask<OrderResult> ProcessOrder(ProcessOrderCommand command, CancellationToken cancellationToken)
        {
            Console.WriteLine($"\n🛒 Processing Order #{command.OrderId} - Amount: ${command.Amount}");
            Console.WriteLine("Executing multi-step workflow...");

            var steps = new List<string>();

            // Step 1: Validate Order
            Console.WriteLine("  ✓ Step 1: Validating order...");
            await Task.Delay(200, cancellationToken);
            if (command.Amount <= 0)
            {
                return new OrderResult(command.OrderId, "Failed", new List<string> { "Invalid amount" });
            }
            steps.Add("Order validated");

            // Step 2: Process Payment
            Console.WriteLine("  ✓ Step 2: Processing payment...");
            await Task.Delay(500, cancellationToken);
            var transactionId = $"TXN-{Guid.NewGuid():N}"[..16];
            steps.Add($"Payment processed: {transactionId}");

            // Step 3: Reserve Inventory
            Console.WriteLine("  ✓ Step 3: Reserving inventory...");
            await Task.Delay(300, cancellationToken);
            var reservationId = $"RSV-{Guid.NewGuid():N}"[..16];
            steps.Add($"Inventory reserved: {reservationId}");

            // Step 4: Send Confirmation
            Console.WriteLine("  ✓ Step 4: Sending confirmation email...");
            await Task.Delay(200, cancellationToken);
            Console.WriteLine($"    Email sent to: {command.CustomerEmail}");
            steps.Add("Confirmation email sent");

            // Step 5: Finalize
            Console.WriteLine("  ✓ Step 5: Finalizing order...");
            await Task.Delay(100, cancellationToken);
            steps.Add("Order finalized");

            return new OrderResult(command.OrderId, "Completed", steps);
        }
    }

    // Refund workflow with compensation
    public record ProcessRefundCommand(int OrderId, decimal Amount) : IRequest<RefundResult>;
    public record RefundResult(bool Success, string Message, List<string> Steps);

    public class RefundWorkflowService
    {
        [Handle]
        public async ValueTask<RefundResult> ProcessRefund(ProcessRefundCommand command, CancellationToken cancellationToken)
        {
            Console.WriteLine($"\n💰 Processing Refund for Order #{command.OrderId}");
            var steps = new List<string>();

            try
            {
                // Step 1: Validate Refund
                Console.WriteLine("  ✓ Validating refund eligibility...");
                await Task.Delay(200, cancellationToken);
                steps.Add("Refund validated");

                // Step 2: Process Refund Payment
                Console.WriteLine("  ✓ Processing refund payment...");
                await Task.Delay(500, cancellationToken);

                // Simulate occasional failure (20% chance)
                if (new Random().Next(100) < 20)
                {
                    throw new Exception("Payment gateway timeout");
                }

                steps.Add($"Refund processed: ${command.Amount}");

                // Step 3: Update Inventory
                Console.WriteLine("  ✓ Updating inventory...");
                await Task.Delay(300, cancellationToken);
                steps.Add("Inventory updated");

                // Step 4: Notify Customer
                Console.WriteLine("  ✓ Notifying customer...");
                await Task.Delay(200, cancellationToken);
                steps.Add("Customer notified");

                return new RefundResult(true, "Refund processed successfully", steps);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Workflow failed: {ex.Message}");
                Console.WriteLine("  ↺ Rolling back changes...");
                
                // Compensation logic would go here
                steps.Add($"Failed: {ex.Message}");
                steps.Add("Rolled back successfully");

                return new RefundResult(false, $"Refund failed: {ex.Message}", steps);
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Relay Workflow Demo Sample");
            Console.WriteLine("=" + new string('=', 60));
            Console.WriteLine();
            Console.WriteLine("⚠️  Note: This demonstrates workflow CONCEPTS using Relay");
            Console.WriteLine("    Real workflow engine requires IWorkflowEngine implementation");
            Console.WriteLine();

            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddRelayConfiguration();
                services.AddTransient<OrderWorkflowService>();
                services.AddTransient<RefundWorkflowService>();
            });

            var host = builder.Build();
            var relay = host.Services.GetRequiredService<IRelay>();

            // Example 1: Simple order processing workflow
            Console.WriteLine("📦 Example 1: Order Processing Workflow");
            Console.WriteLine("-" + new string('-', 60));

            var orderCommand = new ProcessOrderCommand(
                OrderId: 12345,
                Amount: 99.99m,
                CustomerEmail: "customer@example.com"
            );

            var orderResult = await relay.SendAsync(orderCommand);

            Console.WriteLine($"\n✅ Order #{orderResult.OrderId} - Status: {orderResult.Status}");
            Console.WriteLine("Workflow Steps Completed:");
            foreach (var step in orderResult.Steps)
            {
                Console.WriteLine($"  • {step}");
            }

            Console.WriteLine();

            // Example 2: Process multiple orders concurrently
            Console.WriteLine("📦 Example 2: Concurrent Order Processing");
            Console.WriteLine("-" + new string('-', 60));

            var orders = new[]
            {
                new ProcessOrderCommand(1001, 49.99m, "user1@example.com"),
                new ProcessOrderCommand(1002, 149.99m, "user2@example.com"),
                new ProcessOrderCommand(1003, 79.99m, "user3@example.com")
            };

            var tasks = orders.Select(order => relay.SendAsync(order).AsTask()).ToArray();
            var results = await Task.WhenAll(tasks);

            Console.WriteLine($"\n✅ Processed {results.Length} orders concurrently");
            foreach (var result in results)
            {
                Console.WriteLine($"  Order #{result.OrderId}: {result.Status}");
            }

            Console.WriteLine();

            // Example 3: Refund workflow with compensation
            Console.WriteLine("💰 Example 3: Refund Workflow (with potential failure)");
            Console.WriteLine("-" + new string('-', 60));

            // Try multiple refunds to see success and failure scenarios
            for (int i = 1; i <= 3; i++)
            {
                var refundCommand = new ProcessRefundCommand(
                    OrderId: 10000 + i,
                    Amount: 50.00m
                );

                var refundResult = await relay.SendAsync(refundCommand);

                if (refundResult.Success)
                {
                    Console.WriteLine($"✅ Refund #{10000 + i}: {refundResult.Message}");
                }
                else
                {
                    Console.WriteLine($"⚠️  Refund #{10000 + i}: {refundResult.Message}");
                }

                foreach (var step in refundResult.Steps)
                {
                    Console.WriteLine($"    - {step}");
                }

                await Task.Delay(500);
            }

            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 60));
            Console.WriteLine("✅ All workflow examples completed!");
            Console.WriteLine();
            Console.WriteLine("Key Concepts Demonstrated:");
            Console.WriteLine("  • Sequential multi-step processes");
            Console.WriteLine("  • Request/Response workflow pattern");
            Console.WriteLine("  • Error handling and compensation");
            Console.WriteLine("  • Concurrent workflow execution");
            Console.WriteLine("  • Business process orchestration");
            Console.WriteLine();
            Console.WriteLine("💡 For advanced workflows:");
            Console.WriteLine("  • Implement IWorkflowEngine interface");
            Console.WriteLine("  • Add parallel step execution");
            Console.WriteLine("  • Implement conditional branching");
            Console.WriteLine("  • Add workflow state persistence");
            Console.WriteLine("  • Support Saga pattern with compensation");
        }
    }
}
