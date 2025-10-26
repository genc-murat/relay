using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Interfaces;
using Relay.MessageBroker.Saga.Services;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaMessageBrokerIntegrationTests
{
    public class MessageDrivenOrderSaga : Saga<OrderProcessingData>
    {
        private readonly TestableMessageBroker _messageBroker;

        public MessageDrivenOrderSaga(TestableMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;

            // Subscribe to order messages to start saga
            _messageBroker.SubscribeAsync<OrderMessage>(HandleOrderReceived).GetAwaiter().GetResult();

            // Define steps
            AddStep(new ValidateOrderStep(_messageBroker));
            AddStep(new ReserveInventoryStep(_messageBroker));
            AddStep(new ProcessPaymentStep(_messageBroker));
            AddStep(new SendConfirmationStep(_messageBroker));
        }

        private async ValueTask HandleOrderReceived(OrderMessage message, MessageContext context, CancellationToken cancellationToken)
        {
            // Start saga when order message is received
            var sagaData = new OrderProcessingData
            {
                OrderId = message.OrderId,
                Amount = message.Amount,
                CustomerId = message.CustomerId,
                Items = message.Items
            };

            var result = await ExecuteAsync(sagaData);
            if (!result.IsSuccess)
            {
                throw result.Exception ?? new InvalidOperationException("Saga execution failed");
            }
        }
    }

    public class TestableMessageBroker : BaseMessageBroker
    {
        public ConcurrentBag<object> PublishedMessages { get; } = new();
        public List<(Type MessageType, SubscriptionInfo SubscriptionInfo)> SubscribedMessages { get; } = new();

        public TestableMessageBroker(
            IOptions<MessageBrokerOptions> options,
            ILogger logger)
            : base(options, logger)
        {
        }

        protected override async ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            PublishedMessages.Add(message!);

            // Process message for subscribers
            if (IsStarted)
            {
                Console.WriteLine($"Processing message of type {typeof(TMessage).Name}");
                var decompressed = await DecompressMessageAsync(serializedMessage, cancellationToken);
                var deserialized = DeserializeMessage<TMessage>(decompressed);
                var context = new MessageContext();
                await ProcessMessageAsync(deserialized, typeof(TMessage), context, cancellationToken);
                Console.WriteLine($"Finished processing message of type {typeof(TMessage).Name}");
            }
            else
            {
                Console.WriteLine("Broker is not started, not processing message");
            }
        }

        protected override ValueTask SubscribeInternalAsync(
            Type messageType,
            SubscriptionInfo subscriptionInfo,
            CancellationToken cancellationToken)
        {
            SubscribedMessages.Add((messageType, subscriptionInfo));
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StartInternalAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StopInternalAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask DisposeInternalAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task MessageDrivenSaga_OrderProcessingSuccess_ShouldPublishAllEvents()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var messageBroker = new TestableMessageBroker(options, logger);

        var saga = new MessageDrivenOrderSaga(messageBroker);
        await messageBroker.StartAsync();

        var orderMessage = new OrderMessage
        {
            OrderId = "ORDER-001",
            CustomerId = "CUST-001",
            Amount = 150.00m,
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = "PROD-001", Quantity = 2, UnitPrice = 50.00m },
                new OrderItem { ProductId = "PROD-002", Quantity = 1, UnitPrice = 50.00m }
            }
        };

        // Act
        await messageBroker.PublishAsync(orderMessage);

        // Give time for async processing
        await Task.Delay(100);

        // Assert
        var publishedEvents = messageBroker.PublishedMessages.ToList();

        // Should have: OrderValidated, InventoryReserved, PaymentProcessed, OrderConfirmed
        Assert.Contains(publishedEvents, e => e is OrderValidatedEvent evt && evt.OrderId == "ORDER-001");
        Assert.Contains(publishedEvents, e => e is InventoryReservedEvent evt && evt.OrderId == "ORDER-001");
        Assert.Contains(publishedEvents, e => e is PaymentProcessedEvent evt && evt.OrderId == "ORDER-001");
        Assert.Contains(publishedEvents, e => e is OrderConfirmedEvent evt && evt.OrderId == "ORDER-001");

        // Verify saga completed
        var sagaResult = await saga.ExecuteAsync(new OrderProcessingData
        {
            OrderId = "ORDER-001",
            Amount = 150.00m,
            CustomerId = "CUST-001",
            Items = orderMessage.Items
        });

        Assert.True(sagaResult.IsSuccess);
        Assert.Equal(SagaState.Completed, sagaResult.Data.State);
    }

    [Fact]
    public async Task MessageDrivenSaga_OrderValidationFails_ShouldCompensateAndPublishFailureEvents()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var messageBroker = new TestableMessageBroker(options, logger);

        var saga = new MessageDrivenOrderSaga(messageBroker);
        await messageBroker.StartAsync();

        var invalidOrderMessage = new OrderMessage
        {
            OrderId = "ORDER-002",
            CustomerId = "CUST-002",
            Amount = -50.00m, // Invalid amount
            Items = new List<OrderItem>()
        };

        // Act
        await messageBroker.PublishAsync(invalidOrderMessage);

        // Give time for async processing
        await Task.Delay(100);

        // Assert
        var publishedEvents = messageBroker.PublishedMessages.ToList();

        // Should have validation failure event
        Assert.Contains(publishedEvents, e => e is OrderValidationFailedEvent evt && evt.OrderId == "ORDER-002");

        // Should not have success events
        Assert.DoesNotContain(publishedEvents, e => e is InventoryReservedEvent);
        Assert.DoesNotContain(publishedEvents, e => e is PaymentProcessedEvent);
        Assert.DoesNotContain(publishedEvents, e => e is OrderConfirmedEvent);
    }

    [Fact]
    public async Task MessageDrivenSaga_PaymentFails_ShouldCompensatePreviousSteps()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var messageBroker = new TestableMessageBroker(options, logger);

        // Create saga that will fail at payment
        var failingSaga = new FailingPaymentSaga(messageBroker);
        await messageBroker.StartAsync();

        var orderMessage = new OrderMessage
        {
            OrderId = "ORDER-003",
            CustomerId = "CUST-003",
            Amount = 200.00m,
            Items = new List<OrderItem> { new OrderItem { ProductId = "PROD-003", Quantity = 1, UnitPrice = 200.00m } }
        };

        // Act
        await messageBroker.PublishAsync(orderMessage);

        // Give time for async processing
        await Task.Delay(100);

        // Assert
        var publishedEvents = messageBroker.PublishedMessages.ToList();

        // Should have validation and inventory events
        Assert.Contains(publishedEvents, e => e is OrderValidatedEvent evt && evt.OrderId == "ORDER-003");
        Assert.Contains(publishedEvents, e => e is InventoryReservedEvent evt && evt.OrderId == "ORDER-003");

        // Should have compensation events
        Assert.Contains(publishedEvents, e => e is InventoryReservationCancelledEvent evt && evt.OrderId == "ORDER-003");
        Assert.Contains(publishedEvents, e => e is OrderValidationFailedEvent evt && evt.OrderId == "ORDER-003");

        // Should not have payment or confirmation
        Assert.DoesNotContain(publishedEvents, e => e is PaymentProcessedEvent);
        Assert.DoesNotContain(publishedEvents, e => e is OrderConfirmedEvent);
    }

    // Supporting classes
    public class OrderProcessingData : SagaDataBase
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public bool ValidationCompleted { get; set; }
        public bool InventoryReserved { get; set; }
        public bool PaymentProcessed { get; set; }
        public bool ConfirmationSent { get; set; }
    }

    public class OrderMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // Event classes
    public class OrderValidatedEvent { public string OrderId { get; set; } = string.Empty; }
    public class OrderValidationFailedEvent { public string OrderId { get; set; } = string.Empty; }
    public class InventoryReservedEvent { public string OrderId { get; set; } = string.Empty; public List<OrderItem> Items { get; set; } = new(); }
    public class InventoryReservationCancelledEvent { public string OrderId { get; set; } = string.Empty; }
    public class PaymentProcessedEvent { public string OrderId { get; set; } = string.Empty; public decimal Amount { get; set; } }
    public class PaymentRefundedEvent { public string OrderId { get; set; } = string.Empty; public decimal Amount { get; set; } }
    public class OrderConfirmedEvent { public string OrderId { get; set; } = string.Empty; }
    public class OrderConfirmationCancelledEvent { public string OrderId { get; set; } = string.Empty; }

    // Saga that fails at payment step
    public class FailingPaymentSaga : Saga<OrderProcessingData>
    {
        public FailingPaymentSaga(TestableMessageBroker messageBroker)
        {
            // Subscribe to order messages to start saga
            messageBroker.SubscribeAsync<OrderMessage>(HandleOrderReceived).GetAwaiter().GetResult();

            // Define steps
            AddStep(new ValidateOrderStep(messageBroker));
            AddStep(new ReserveInventoryStep(messageBroker));
            AddStep(new FailingProcessPaymentStep(messageBroker));
        }

        private async ValueTask HandleOrderReceived(OrderMessage message, MessageContext context, CancellationToken cancellationToken)
        {
            // Start saga when order message is received
            var sagaData = new OrderProcessingData
            {
                OrderId = message.OrderId,
                Amount = message.Amount,
                CustomerId = message.CustomerId,
                Items = message.Items
            };

            var result = await ExecuteAsync(sagaData);
            if (!result.IsSuccess)
            {
                throw result.Exception ?? new InvalidOperationException("Saga execution failed");
            }
        }
    }

    // Step implementations
    public class ValidateOrderStep : ISagaStep<OrderProcessingData>
    {
        private readonly TestableMessageBroker _messageBroker;

        public ValidateOrderStep(TestableMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public string Name => "ValidateOrder";

        public async ValueTask ExecuteAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            // Simulate validation
            if (data.Amount <= 0)
            {
                await _messageBroker.PublishAsync(new OrderValidationFailedEvent { OrderId = data.OrderId });
                throw new InvalidOperationException("Invalid order amount");
            }

            data.ValidationCompleted = true;
            await _messageBroker.PublishAsync(new OrderValidatedEvent { OrderId = data.OrderId });
        }

        public async ValueTask CompensateAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            data.ValidationCompleted = false;
            await _messageBroker.PublishAsync(new OrderValidationFailedEvent { OrderId = data.OrderId });
        }
    }

    public class ReserveInventoryStep : ISagaStep<OrderProcessingData>
    {
        private readonly TestableMessageBroker _messageBroker;

        public ReserveInventoryStep(TestableMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public string Name => "ReserveInventory";

        public async ValueTask ExecuteAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            // Simulate inventory reservation
            data.InventoryReserved = true;
            await _messageBroker.PublishAsync(new InventoryReservedEvent { OrderId = data.OrderId, Items = data.Items });
        }

        public async ValueTask CompensateAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            data.InventoryReserved = false;
            await _messageBroker.PublishAsync(new InventoryReservationCancelledEvent { OrderId = data.OrderId });
        }
    }

    public class ProcessPaymentStep : ISagaStep<OrderProcessingData>
    {
        private readonly TestableMessageBroker _messageBroker;

        public ProcessPaymentStep(TestableMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public string Name => "ProcessPayment";

        public async ValueTask ExecuteAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            // Simulate payment processing
            data.PaymentProcessed = true;
            await _messageBroker.PublishAsync(new PaymentProcessedEvent { OrderId = data.OrderId, Amount = data.Amount });
        }

        public async ValueTask CompensateAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            data.PaymentProcessed = false;
            await _messageBroker.PublishAsync(new PaymentRefundedEvent { OrderId = data.OrderId, Amount = data.Amount });
        }
    }

    public class FailingProcessPaymentStep : ISagaStep<OrderProcessingData>
    {
        private readonly TestableMessageBroker _messageBroker;

        public FailingProcessPaymentStep(TestableMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public string Name => "ProcessPayment";

        public ValueTask ExecuteAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Payment processing failed");
        }

        public async ValueTask CompensateAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            data.PaymentProcessed = false;
            await _messageBroker.PublishAsync(new PaymentRefundedEvent { OrderId = data.OrderId, Amount = data.Amount });
        }
    }

    public class SendConfirmationStep : ISagaStep<OrderProcessingData>
    {
        private readonly TestableMessageBroker _messageBroker;

        public SendConfirmationStep(TestableMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public string Name => "SendConfirmation";

        public async ValueTask ExecuteAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            // Send final confirmation
            data.ConfirmationSent = true;
            await _messageBroker.PublishAsync(new OrderConfirmedEvent { OrderId = data.OrderId });
        }

        public async ValueTask CompensateAsync(OrderProcessingData data, CancellationToken cancellationToken = default)
        {
            data.ConfirmationSent = false;
            await _messageBroker.PublishAsync(new OrderConfirmationCancelledEvent { OrderId = data.OrderId });
        }
    }
}