# Saga Pattern Sample

This sample demonstrates how to use the **Relay Saga Pattern** to manage distributed transactions with automatic compensation in case of failures.

## What is Saga Pattern?

The Saga pattern is a design pattern for managing data consistency across microservices in distributed transaction scenarios. Instead of a traditional ACID transaction, a saga executes a sequence of local transactions where each transaction updates data within a single service. If a step fails, the saga executes compensating transactions to undo the changes made by preceding steps.

## Scenario: E-Commerce Order Processing

This sample implements a complete order processing workflow with the following steps:

1. **Create Order** - Initialize order in the system
2. **Reserve Inventory** - Reserve products from inventory
3. **Process Payment** - Charge customer's payment method
4. **Ship Order** - Initiate shipping
5. **Send Notification** - Notify customer of successful order

If any step fails, the saga automatically executes compensation logic to roll back previous steps.

## Features Demonstrated

- ✅ Multi-step distributed transaction
- ✅ Automatic compensation on failure
- ✅ State persistence (In-Memory and Database)
- ✅ Detailed logging and monitoring
- ✅ Timeout handling
- ✅ Saga status tracking

## Project Structure

```
SagaPatternSample/
├── Commands/           # Command definitions
├── Controllers/        # API controllers
├── Data/              # Database context
├── Handlers/          # Command handlers
├── Models/            # DTOs and models
├── Sagas/             # Saga definitions
└── Services/          # Business services (Inventory, Payment, Shipping)
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Running the Sample

1. Navigate to the project directory:
   ```bash
   cd samples/SagaPatternSample/SagaPatternSample
   ```

2. Build and run:
   ```bash
   dotnet build
   dotnet run
   ```

3. Open Swagger UI:
   ```
   https://localhost:7xxx/swagger
   ```

## API Endpoints

### 1. Create Order (Success Scenario)

**POST** `/api/orders/create`

Creates a new order that will successfully complete all saga steps.

Request body:
```json
{
  "orderId": "ORDER-001",
  "customerId": "CUST-001",
  "items": [
    {
      "productId": "PROD-001",
      "quantity": 2,
      "price": 50.00
    }
  ],
  "totalAmount": 100.00
}
```

Response:
```json
{
  "orderId": "ORDER-001",
  "success": true,
  "message": "Order created successfully",
  "sagaStatus": "Completed"
}
```

### 2. Create Order (Inventory Failure)

**POST** `/api/orders/create-inventory-fail`

Triggers a failure at the inventory reservation step to demonstrate compensation.

Response:
```json
{
  "orderId": "generated-guid",
  "success": false,
  "message": "Inventory reservation failed - compensation triggered",
  "sagaStatus": "Compensated"
}
```

### 3. Create Order (Payment Failure)

**POST** `/api/orders/create-payment-fail`

Triggers a failure at the payment processing step to demonstrate compensation of multiple steps.

Response:
```json
{
  "orderId": "generated-guid",
  "success": false,
  "message": "Payment failed - compensation triggered",
  "sagaStatus": "Compensated"
}
```

### 4. Get Saga Status

**GET** `/api/orders/{sagaId}/status`

Retrieves the current state of a saga execution.

Response:
```json
{
  "id": 1,
  "sagaId": "saga-guid",
  "status": "Completed",
  "data": "{...}",
  "createdAt": "2025-01-03T10:00:00Z",
  "updatedAt": "2025-01-03T10:00:05Z",
  "currentStep": "SendNotification",
  "errorMessage": null
}
```

## Testing the Compensation Flow

### Scenario 1: Inventory Failure

1. Call `POST /api/orders/create-inventory-fail`
2. Observe logs showing:
   - ✅ Step 1: Order Created
   - ❌ Step 2: Inventory Reservation Failed
   - ↩️ Compensation: Order Deleted

### Scenario 2: Payment Failure

1. Call `POST /api/orders/create-payment-fail`
2. Observe logs showing:
   - ✅ Step 1: Order Created
   - ✅ Step 2: Inventory Reserved
   - ❌ Step 3: Payment Failed
   - ↩️ Compensation: Inventory Released
   - ↩️ Compensation: Order Deleted

## Configuration

### Database Options

The sample supports two persistence modes:

#### 1. In-Memory Database (Default)
```csharp
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("SagaDemo"));
```

#### 2. SQLite Database
```csharp
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite("Data Source=saga.db"));
```

### Saga Options

Configure saga behavior in `Program.cs`:

```csharp
builder.Services.AddRelaySaga<OrderDbContext>(options =>
{
    options.DefaultTimeout = TimeSpan.FromMinutes(5);
    options.EnableDetailedLogging = true;
});
```

## Understanding the Code

### Saga Definition

The `OrderSaga` class defines the workflow:

```csharp
public class OrderSaga : SagaDefinition<OrderSagaData>
{
    public OrderSaga(...)
    {
        // Define steps with execute and compensate logic
        Step("CreateOrder")
            .Execute(async (data, ct) => { /* ... */ })
            .Compensate(async (data, ct) => { /* ... */ });
        
        Step("ReserveInventory")
            .Execute(async (data, ct) => { /* ... */ })
            .Compensate(async (data, ct) => { /* ... */ });
        
        // ... more steps
    }
}
```

### Saga Execution

The saga is executed through the coordinator:

```csharp
var result = await _sagaCoordinator.ExecuteAsync<OrderSaga, OrderSagaData>(
    sagaData, 
    CancellationToken.None);
```

## Key Concepts

### 1. Saga Steps
Each step has:
- **Execute**: Forward logic (normal flow)
- **Compensate**: Rollback logic (error flow)

### 2. Saga Data
`OrderSagaData` carries state through all steps:
```csharp
public class OrderSagaData
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Step tracking
    public bool OrderCreated { get; set; }
    public bool InventoryReserved { get; set; }
    public bool PaymentProcessed { get; set; }
    // ...
}
```

### 3. Compensation
When a step fails:
1. Execution stops
2. Compensation runs for all completed steps
3. Compensation executes in reverse order
4. Saga status becomes "Compensated"

## Benefits

1. **Consistency**: Maintains data consistency across distributed services
2. **Resilience**: Automatic recovery from failures
3. **Visibility**: Clear audit trail of saga execution
4. **Flexibility**: Easy to add/modify steps
5. **Testing**: Simple failure simulation for testing

## Production Considerations

1. **Idempotency**: Ensure compensating actions are idempotent
2. **Timeouts**: Configure appropriate timeouts for long-running operations
3. **Monitoring**: Monitor saga execution metrics
4. **Error Handling**: Handle partial failures gracefully
5. **Database**: Use persistent storage in production

## Related Samples

- **MessageBroker.Sample**: Event-driven saga orchestration
- **EventSourcingSample**: Event sourcing with sagas
- **DistributedTracingSample**: Distributed tracing for sagas

## Learn More

- [Saga Pattern Documentation](../../docs/patterns/saga-pattern.md)
- [Relay Core Documentation](../../docs/README.md)
- [Microservices Patterns](https://microservices.io/patterns/data/saga.html)

## License

This sample is part of the Relay project and follows the same license.
