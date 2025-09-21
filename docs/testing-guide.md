# Relay Testing Guide

This guide covers the comprehensive testing utilities provided by Relay, including the fluent test builder API, specialized assertions, and enhanced test harness capabilities.

## Overview

Relay's testing framework provides:
- **RelayTestBuilder**: Fluent API for easy test setup and configuration
- **RelayAssertions**: Specialized assertion methods for Relay-specific testing scenarios
- **Enhanced RelayTestHarness**: Advanced test harness with trace capture and performance measurement
- **Mock Handler Support**: Type-safe mock handlers for isolated testing
- **Pipeline Testing**: Tools for testing pipeline behavior and ordering

## Getting Started

### Basic Test Setup

```csharp
[TestFixture]
public class OrderHandlerTests
{
    [Test]
    public async Task Should_Create_Order_Successfully()
    {
        // Arrange
        var relay = new RelayTestBuilder()
            .WithHandler<CreateOrderRequest, OrderResponse>((request, ct) =>
                ValueTask.FromResult(new OrderResponse 
                { 
                    OrderId = Guid.NewGuid(),
                    Status = "Created" 
                }))
            .Build();
        
        // Act
        var response = await relay.Send(new CreateOrderRequest 
        { 
            CustomerId = 123,
            Items = new[] { new OrderItem { ProductId = 1, Quantity = 2 } }
        });
        
        // Assert
        Assert.That(response.Status, Is.EqualTo("Created"));
        Assert.That(response.OrderId, Is.Not.EqualTo(Guid.Empty));
    }
}
```

## RelayTestBuilder

The RelayTestBuilder provides a fluent API for configuring test scenarios.

### Handler Registration

```csharp
[Test]
public async Task Should_Register_Multiple_Handlers()
{
    var relay = new RelayTestBuilder()
        // Register with lambda
        .WithHandler<CreateOrderRequest, OrderResponse>((req, ct) => 
            ValueTask.FromResult(new OrderResponse { OrderId = Guid.NewGuid() }))
        
        // Register with instance
        .WithHandler(new OrderQueryHandler())
        
        // Register with type
        .WithHandler<OrderUpdateHandler>()
        
        // Register mock handler
        .WithMockHandler<DeleteOrderRequest, DeleteOrderResponse>()
        
        .Build();
    
    // Test multiple operations
    var createResponse = await relay.Send(new CreateOrderRequest());
    var queryResponse = await relay.Send(new GetOrderRequest { OrderId = createResponse.OrderId });
    
    Assert.That(createResponse.OrderId, Is.Not.EqualTo(Guid.Empty));
    Assert.That(queryResponse.Order, Is.Not.Null);
}
```

### Pipeline Configuration

```csharp
[Test]
public async Task Should_Execute_Pipelines_In_Order()
{
    var executionOrder = new List<string>();
    
    var relay = new RelayTestBuilder()
        .WithHandler<TestRequest, TestResponse>((req, ct) =>
        {
            executionOrder.Add("Handler");
            return ValueTask.FromResult(new TestResponse());
        })
        .WithPipeline<ValidationPipeline>(pipeline =>
        {
            pipeline.OnExecute = () => executionOrder.Add("Validation");
        })
        .WithPipeline<LoggingPipeline>(pipeline =>
        {
            pipeline.OnExecute = () => executionOrder.Add("Logging");
        })
        .Build();
    
    await relay.Send(new TestRequest());
    
    Assert.That(executionOrder, Is.EqualTo(new[] { "Validation", "Logging", "Handler" }));
}
```

### Service Dependencies

```csharp
[Test]
public async Task Should_Inject_Dependencies()
{
    var mockRepository = new Mock<IOrderRepository>();
    mockRepository.Setup(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Order { Id = Guid.NewGuid() });
    
    var relay = new RelayTestBuilder()
        .WithService(mockRepository.Object)
        .WithService<ILogger<OrderHandler>>(Mock.Of<ILogger<OrderHandler>>())
        .WithHandler<OrderHandler>()
        .Build();
    
    var response = await relay.Send(new CreateOrderRequest());
    
    mockRepository.Verify(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

### Configuration Options

```csharp
[Test]
public async Task Should_Configure_Relay_Options()
{
    var relay = new RelayTestBuilder()
        .WithOptions(options =>
        {
            options.EnableDiagnostics = true;
            options.ThrowOnUnhandledRequest = false;
            options.DefaultTimeout = TimeSpan.FromSeconds(30);
        })
        .WithHandler<TestHandler>()
        .Build();
    
    // Test with configured options
    var response = await relay.Send(new TestRequest());
    
    Assert.That(response, Is.Not.Null);
}
```

## RelayAssertions

Specialized assertion methods for common Relay testing patterns.

### Handler Execution Assertions

```csharp
[Test]
public async Task Should_Execute_Handler_Successfully()
{
    var relay = new RelayTestBuilder()
        .WithHandler<TestRequest, TestResponse>(handler)
        .Build();
    
    await relay.Send(new TestRequest());
    
    // Assert handler was executed
    RelayAssertions.AssertHandlerExecuted<TestRequest>(relay);
    
    // Assert handler was executed with specific parameters
    RelayAssertions.AssertHandlerExecuted<TestRequest>(relay, 
        request => request.Id == 123);
}
```

### Pipeline Order Assertions

```csharp
[Test]
public async Task Should_Execute_Pipelines_In_Correct_Order()
{
    var relay = new RelayTestBuilder()
        .WithHandler<TestRequest, TestResponse>(handler)
        .WithPipeline<ValidationPipeline>()
        .WithPipeline<AuthorizationPipeline>()
        .WithPipeline<LoggingPipeline>()
        .Build();
    
    await relay.Send(new TestRequest());
    
    // Assert pipeline execution order
    RelayAssertions.AssertPipelineOrder(relay,
        typeof(AuthorizationPipeline),
        typeof(ValidationPipeline),
        typeof(LoggingPipeline));
}
```

### Performance Assertions

```csharp
[Test]
public async Task Should_Complete_Within_Performance_Threshold()
{
    var relay = new RelayTestBuilder()
        .WithHandler<TestRequest, TestResponse>(handler)
        .Build();
    
    // Assert execution time
    await RelayAssertions.AssertPerformance(
        () => relay.Send(new TestRequest()),
        maxDuration: TimeSpan.FromMilliseconds(100));
    
    // Assert memory allocations
    await RelayAssertions.AssertPerformance(
        () => relay.Send(new TestRequest()),
        maxAllocations: 1024);
    
    // Assert both duration and allocations
    await RelayAssertions.AssertPerformance(
        () => relay.Send(new TestRequest()),
        maxDuration: TimeSpan.FromMilliseconds(100),
        maxAllocations: 1024);
}
```

### Exception Assertions

```csharp
[Test]
public async Task Should_Handle_Exceptions_Correctly()
{
    var relay = new RelayTestBuilder()
        .WithHandler<TestRequest, TestResponse>((req, ct) =>
            throw new ValidationException("Invalid request"))
        .Build();
    
    // Assert specific exception type
    await RelayAssertions.AssertThrows<ValidationException>(
        () => relay.Send(new TestRequest()));
    
    // Assert exception with message
    await RelayAssertions.AssertThrows<ValidationException>(
        () => relay.Send(new TestRequest()),
        ex => ex.Message.Contains("Invalid request"));
}
```

## Enhanced RelayTestHarness

The enhanced test harness provides advanced testing capabilities including trace capture and performance measurement.

### Basic Usage

```csharp
[Test]
public async Task Should_Process_Request_With_Harness()
{
    using var harness = new RelayTestHarness();
    
    harness.RegisterHandler<TestRequest, TestResponse>((req, ct) =>
        ValueTask.FromResult(new TestResponse { Message = "Success" }));
    
    var response = await harness.Send(new TestRequest());
    
    Assert.That(response.Message, Is.EqualTo("Success"));
}
```

### Trace Capture

```csharp
[Test]
public async Task Should_Capture_Execution_Trace()
{
    using var harness = new RelayTestHarness();
    
    harness.RegisterHandler<TestRequest, TestResponse>((req, ct) =>
    {
        harness.Tracer.RecordStep("Processing started");
        // Simulate work
        Thread.Sleep(10);
        harness.Tracer.RecordStep("Processing completed");
        
        return ValueTask.FromResult(new TestResponse());
    });
    
    await harness.Send(new TestRequest());
    
    var trace = harness.GetExecutionTrace();
    
    Assert.That(trace.Steps, Has.Count.EqualTo(4)); // Entry, custom steps, exit
    Assert.That(trace.Steps.Any(s => s.Name == "Processing started"), Is.True);
    Assert.That(trace.TotalDuration, Is.GreaterThan(TimeSpan.FromMilliseconds(10)));
}
```

### Performance Measurement

```csharp
[Test]
public async Task Should_Measure_Performance_Accurately()
{
    using var harness = new RelayTestHarness();
    
    harness.RegisterHandler<TestRequest, TestResponse>((req, ct) =>
    {
        // Simulate some work
        var data = new byte[1024]; // Allocate some memory
        Thread.Sleep(5); // Simulate processing time
        
        return ValueTask.FromResult(new TestResponse());
    });
    
    var metrics = await harness.MeasurePerformance(
        () => harness.Send(new TestRequest()),
        iterations: 10);
    
    Assert.That(metrics.AverageExecutionTime, Is.GreaterThan(TimeSpan.FromMilliseconds(5)));
    Assert.That(metrics.AverageExecutionTime, Is.LessThan(TimeSpan.FromMilliseconds(50)));
    Assert.That(metrics.TotalAllocatedBytes, Is.GreaterThan(1024));
    Assert.That(metrics.IterationCount, Is.EqualTo(10));
}
```

### Benchmark Testing

```csharp
[Test]
public async Task Should_Run_Performance_Benchmark()
{
    using var harness = new RelayTestHarness();
    
    harness.RegisterHandler<TestRequest, TestResponse>(handler);
    
    var benchmark = await harness.RunBenchmark(
        () => harness.Send(new TestRequest()),
        duration: TimeSpan.FromSeconds(5));
    
    Assert.That(benchmark.TotalOperations, Is.GreaterThan(100));
    Assert.That(benchmark.OperationsPerSecond, Is.GreaterThan(20));
    Assert.That(benchmark.AverageLatency, Is.LessThan(TimeSpan.FromMilliseconds(50)));
}
```

## Mock Handlers

Create type-safe mock handlers for isolated testing.

### Simple Mock Handlers

```csharp
[Test]
public async Task Should_Use_Mock_Handler()
{
    var relay = new RelayTestBuilder()
        .WithMockHandler<GetUserRequest, UserResponse>(mock =>
        {
            mock.Setup(req => req.UserId == 123)
                .Returns(new UserResponse { Name = "John Doe", Email = "john@example.com" });
                
            mock.Setup(req => req.UserId == 456)
                .Returns(new UserResponse { Name = "Jane Smith", Email = "jane@example.com" });
        })
        .Build();
    
    var user1 = await relay.Send(new GetUserRequest { UserId = 123 });
    var user2 = await relay.Send(new GetUserRequest { UserId = 456 });
    
    Assert.That(user1.Name, Is.EqualTo("John Doe"));
    Assert.That(user2.Name, Is.EqualTo("Jane Smith"));
}
```

### Advanced Mock Scenarios

```csharp
[Test]
public async Task Should_Handle_Complex_Mock_Scenarios()
{
    var relay = new RelayTestBuilder()
        .WithMockHandler<ProcessOrderRequest, ProcessOrderResponse>(mock =>
        {
            // Success scenario
            mock.Setup(req => req.OrderId == 1)
                .Returns(new ProcessOrderResponse { Success = true, Message = "Order processed" });
            
            // Failure scenario
            mock.Setup(req => req.OrderId == 2)
                .Returns(new ProcessOrderResponse { Success = false, Message = "Insufficient inventory" });
            
            // Exception scenario
            mock.Setup(req => req.OrderId == 3)
                .Throws(new InvalidOperationException("Order not found"));
            
            // Async delay scenario
            mock.Setup(req => req.OrderId == 4)
                .Returns(async () =>
                {
                    await Task.Delay(100);
                    return new ProcessOrderResponse { Success = true, Message = "Delayed processing" };
                });
        })
        .Build();
    
    // Test success
    var response1 = await relay.Send(new ProcessOrderRequest { OrderId = 1 });
    Assert.That(response1.Success, Is.True);
    
    // Test failure
    var response2 = await relay.Send(new ProcessOrderRequest { OrderId = 2 });
    Assert.That(response2.Success, Is.False);
    
    // Test exception
    Assert.ThrowsAsync<InvalidOperationException>(
        () => relay.Send(new ProcessOrderRequest { OrderId = 3 }));
    
    // Test async delay
    var stopwatch = Stopwatch.StartNew();
    var response4 = await relay.Send(new ProcessOrderRequest { OrderId = 4 });
    stopwatch.Stop();
    
    Assert.That(response4.Success, Is.True);
    Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThan(90));
}
```

## Integration Testing

### Testing with Real Dependencies

```csharp
[TestFixture]
public class OrderIntegrationTests
{
    private IServiceProvider _serviceProvider;
    
    [OneTimeSetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Register real services
        services.AddDbContext<OrderContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddLogging();
        
        // Register Relay
        services.AddRelay()
            .AddDiagnostics();
            
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Test]
    public async Task Should_Create_Order_End_To_End()
    {
        var relay = new RelayTestBuilder()
            .WithServiceProvider(_serviceProvider)
            .WithHandler<CreateOrderHandler>()
            .WithHandler<GetOrderHandler>()
            .Build();
        
        // Create order
        var createResponse = await relay.Send(new CreateOrderRequest
        {
            CustomerId = 123,
            Items = new[] { new OrderItem { ProductId = 1, Quantity = 2 } }
        });
        
        // Verify order was created
        var getResponse = await relay.Send(new GetOrderRequest { OrderId = createResponse.OrderId });
        
        Assert.That(getResponse.Order, Is.Not.Null);
        Assert.That(getResponse.Order.CustomerId, Is.EqualTo(123));
        Assert.That(getResponse.Order.Items, Has.Count.EqualTo(1));
    }
}
```

### Testing Pipeline Integration

```csharp
[Test]
public async Task Should_Test_Complete_Pipeline()
{
    var relay = new RelayTestBuilder()
        .WithHandler<OrderHandler>()
        .WithPipeline<ValidationPipeline>()
        .WithPipeline<AuthorizationPipeline>()
        .WithPipeline<AuditPipeline>()
        .WithPipeline<CachingPipeline>()
        .Build();
    
    var request = new CreateOrderRequest { CustomerId = 123 };
    var response = await relay.Send(request);
    
    // Verify all pipelines executed
    RelayAssertions.AssertPipelineOrder(relay,
        typeof(AuthorizationPipeline),
        typeof(ValidationPipeline),
        typeof(CachingPipeline),
        typeof(AuditPipeline));
    
    // Verify handler executed
    RelayAssertions.AssertHandlerExecuted<CreateOrderRequest>(relay);
    
    Assert.That(response, Is.Not.Null);
}
```

## Best Practices

### Test Organization

1. **Use descriptive test names**: Follow the "Should_ExpectedBehavior_When_StateUnderTest" pattern
2. **Arrange-Act-Assert**: Structure tests clearly with distinct sections
3. **One assertion per test**: Focus each test on a single behavior
4. **Use test categories**: Group related tests with `[Category]` attributes

### Performance Testing

1. **Baseline measurements**: Establish performance baselines for critical paths
2. **Realistic data**: Use representative data sizes and complexity
3. **Multiple iterations**: Run performance tests multiple times for accuracy
4. **Environment consistency**: Ensure consistent test environments

### Mock Usage

1. **Isolate dependencies**: Use mocks to isolate the system under test
2. **Verify interactions**: Use mock verification to ensure correct behavior
3. **Realistic responses**: Make mock responses realistic and consistent
4. **Edge cases**: Test both success and failure scenarios

### Integration Testing

1. **Test boundaries**: Focus on testing integration points and boundaries
2. **Data cleanup**: Ensure proper test data cleanup between tests
3. **Transaction isolation**: Use database transactions or in-memory databases
4. **Configuration testing**: Test different configuration scenarios

## Common Testing Patterns

### Testing Validation

```csharp
[Test]
public async Task Should_Validate_Request_Parameters()
{
    var relay = new RelayTestBuilder()
        .WithHandler<CreateOrderRequest, OrderResponse>(handler)
        .WithPipeline<ValidationPipeline>()
        .Build();
    
    var invalidRequest = new CreateOrderRequest(); // Missing required fields
    
    await RelayAssertions.AssertThrows<ValidationException>(
        () => relay.Send(invalidRequest));
}
```

### Testing Error Handling

```csharp
[Test]
public async Task Should_Handle_Repository_Errors()
{
    var mockRepository = new Mock<IOrderRepository>();
    mockRepository.Setup(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new DatabaseException("Connection failed"));
    
    var relay = new RelayTestBuilder()
        .WithService(mockRepository.Object)
        .WithHandler<OrderHandler>()
        .Build();
    
    await RelayAssertions.AssertThrows<DatabaseException>(
        () => relay.Send(new CreateOrderRequest()));
}
```

### Testing Async Behavior

```csharp
[Test]
public async Task Should_Handle_Concurrent_Requests()
{
    var relay = new RelayTestBuilder()
        .WithHandler<TestRequest, TestResponse>(handler)
        .Build();
    
    var tasks = Enumerable.Range(1, 10)
        .Select(i => relay.Send(new TestRequest { Id = i }))
        .ToArray();
    
    var responses = await Task.WhenAll(tasks);
    
    Assert.That(responses, Has.Length.EqualTo(10));
    Assert.That(responses.All(r => r != null), Is.True);
}
```

For more testing examples and patterns, see the [Examples](examples/README.md) documentation.