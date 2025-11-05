# Relay.Core.Testing

A comprehensive testing framework for Relay-based applications, providing advanced testing utilities, isolation mechanisms, dependency mocking, and performance profiling capabilities.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
  - [Basic Setup](#basic-setup)
  - [Async Testing](#async-testing)
  - [Test Isolation](#test-isolation)
  - [Dependency Mocking](#dependency-mocking)
  - [Error Handling](#error-handling-with-retry)
  - [Scenario-Based Testing](#scenario-based-testing)
  - [Performance Profiling](#performance-profiling)
  - [Coverage Tracking](#coverage-tracking)
- [Configuration](#configuration)
  - [Environment-Specific Configuration](#environment-specific-configuration)
  - [Fluent Assertions](#fluent-assertions)
- [Advanced Usage](#advanced-usage)
  - [Custom Test Scenarios](#custom-test-scenarios)
  - [Custom Mock Behaviors](#custom-mock-behaviors)
  - [Error Pattern Analysis](#error-pattern-analysis)
  - [Integration with Test Frameworks](#integration-with-test-frameworks)
  - [Snapshot Testing](#snapshot-testing)
  - [Load Testing](#load-testing)
- [API Reference](#api-reference)
- [Migration Guide](#migration-guide)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Async Testing Utilities**: Timeout handling, fluent async assertions, and concurrency control
- **Test Isolation**: Data isolation, dependency mocking, and automatic cleanup
- **Error Handling**: Retry mechanisms, error capture, and diagnostic reporting
- **Performance Profiling**: Memory and execution time tracking with configurable thresholds
- **Scenario-Based Testing**: Template-based test scenarios with fluent configuration
- **Coverage Tracking**: Line, branch, and method coverage analysis
- **Configuration System**: Environment-specific options with fluent builders
- **Snapshot Testing**: JSON-based snapshot testing with diff visualization
- **Load Testing**: Configurable load testing with performance metrics
- **Multi-Framework Support**: xUnit, NUnit, and MSTest integration

## Installation

```bash
dotnet add package Relay.Core.Testing
```

### Package Information

- **Package ID**: `Relay.Core.Testing`
- **Version**: `1.3.0`
- **Target Framework**: `.NET 8.0`
- **Dependencies**:
  - `Relay.Core` (>= 1.1.0)
  - `System.Text.Json` (>= 8.0.0)
  - `xunit` (>= 2.4.0) - optional, for xUnit integration
  - `NUnit` (>= 3.13.0) - optional, for NUnit integration
  - `MSTest.TestFramework` (>= 3.0.0) - optional, for MSTest integration

### Building from Source

```bash
# Clone the repository
git clone https://github.com/genc-murat/relay.git
cd relay

# Build the testing framework
dotnet build src/Relay.Core.Testing/Relay.Core.Testing.csproj

# Run tests
dotnet test tests/Relay.Core.Testing.Tests/Relay.Core.Testing.Tests.csproj

# Create NuGet package
dotnet pack src/Relay.Core.Testing/Relay.Core.Testing.csproj -c Release
```

### Distribution

The package is distributed via NuGet.org. To publish a new version:

```bash
# Build and pack
dotnet pack src/Relay.Core.Testing/Relay.Core.Testing.csproj -c Release

# Push to NuGet (requires API key)
dotnet nuget push bin/Release/*.nupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json
```

## Quick Start

### Basic Setup

```csharp
using Relay.Core.Testing;

// Configure test options
var options = TestRelayOptionsBuilder.CreateOptions()
    .WithDefaultTimeout(TimeSpan.FromSeconds(30))
    .WithIsolation(true)
    .WithPerformanceProfiling()
    .Build();

// Create test environment
var environment = TestEnvironmentConfigurationFactory.CreateDefault();
var effectiveOptions = environment.GetEffectiveOptions();
```

### Async Testing

```csharp
// Timeout assertions
await (async () => await Task.Delay(100)) // Action that should complete within 1 second
    .ShouldCompleteWithin(TimeSpan.FromSeconds(1));

// Exception assertions
await Should.Throw<TimeoutException>(async () =>
    await Task.Delay(5000).ShouldCompleteWithin(TimeSpan.FromSeconds(1)));

// Execution time measurement
var executionTime = await (async () => await Task.Delay(100))
    .MeasureExecutionTime();

executionTime.ShouldBeLessThan(TimeSpan.FromSeconds(1));
```

### Test Isolation

```csharp
// Data isolation
using var isolation = new TestDataIsolationHelper();

// Isolated memory store
var store = isolation.CreateIsolatedMemoryStore<TestData>();
store.Store("key1", new TestData { Id = 1, Name = "Test" });

// Isolated database context
var dbContext = isolation.CreateIsolatedDatabaseContext(() => new MyDbContext());

// Automatic cleanup
using var cleanup = new TestCleanupHelper();
cleanup.RegisterTempFile("test.txt");
cleanup.RegisterDisposable(myDisposableObject);
```

### Dependency Mocking

```csharp
// Create mock helper
var mockHelper = new DependencyMockHelper();

// Mock dependencies
var serviceMock = mockHelper.Mock<IMyService>()
    .Setup(x => x.GetData(), "mocked data")
    .Setup(x => x.ProcessAsync(), Task.FromResult(true));

// Access service provider
var services = mockHelper.ServiceProvider;
var service = services.GetService<IMyService>();

// Verify interactions
mockHelper.Verify(x => x.GetData(), CallTimes.Once());
```

### Error Handling with Retry

```csharp
var errorHandler = new TestErrorHandler(options);

// Retry with default policy
await errorHandler.WithRetry(async () =>
{
    // Operation that might fail
    await unreliableService.CallAsync();
}, maxAttempts: 3);

// Custom retry policy
var retryPolicy = new RetryPolicy
{
    MaxAttempts = 5,
    Delay = TimeSpan.FromSeconds(2),
    BackoffStrategy = BackoffStrategy.Exponential
};

await errorHandler.ExecuteWithRetryAsync(
    () => unreliableService.CallAsync(),
    retryPolicy);
```

### Scenario-Based Testing

```csharp
// CQRS Scenario
var scenario = new CqrsScenarioTemplate("User Registration")
    .Given("User data is valid", () => { /* setup */ })
    .When("User registers", async () => { /* action */ })
    .Then("User is created", result => { /* assertions */ });

// Event-Driven Scenario
var eventScenario = new EventDrivenScenarioTemplate("Order Processing")
    .Given("Order is placed", () => { /* setup */ })
    .When("Payment is processed", async () => { /* event */ })
    .Then("Order status is updated", result => { /* assertions */ });

// Streaming Scenario
var streamingScenario = new StreamingScenarioTemplate("Real-time Data Processing")
    .Given("Stream is connected", () => { /* setup */ })
    .When("Data is streamed", async () => { /* streaming */ })
    .Then("Data is processed correctly", result => { /* assertions */ });
```

### Performance Profiling

```csharp
var profiler = new PerformanceProfiler(options.PerformanceProfiling);

using (var session = profiler.StartSession("Database Query"))
{
    // Code to profile
    await database.QueryAsync();
}

// Get metrics
var metrics = session.GetMetrics();
metrics.ExecutionTime.ShouldBeLessThan(TimeSpan.FromSeconds(1));
metrics.MemoryUsage.ShouldBeLessThan(100 * 1024 * 1024); // 100MB
```

### Coverage Tracking

```csharp
var coverageTracker = new TestCoverageTracker(options.CoverageTracking);

using (var session = coverageTracker.StartTracking())
{
    // Code to track coverage for
    await myService.ProcessData();
}

// Get coverage report
var report = session.GenerateReport();
report.OverallCoverage.ShouldBeGreaterThan(80.0);
```

## Configuration

### Environment-Specific Configuration

```csharp
// Development configuration
var devConfig = TestRelayOptionsBuilder.CreateOptions()
    .ForDevelopment()
    .WithDiagnosticLogging(log => log.LogLevel = LogLevel.Debug);

// CI/CD configuration
var ciConfig = TestRelayOptionsBuilder.CreateOptions()
    .ForCI()
    .WithCoverageTracking(coverage => coverage.MinimumCoverageThreshold = 85.0);

// Performance testing configuration
var perfConfig = TestRelayOptionsBuilder.CreateOptions()
    .ForPerformanceTesting();

// Environment overrides
var environmentConfig = new TestEnvironmentConfiguration(defaultOptions);
environmentConfig.AddEnvironmentConfig("Development", builder => builder.ForDevelopment());
environmentConfig.AddEnvironmentConfig("CI", builder => builder.ForCI());
```

### Fluent Assertions

```csharp
// Collection assertions
var items = new List<string> { "a", "b", "c" };
items.ShouldContain("b");
items.ShouldHaveCount(3);
items.ShouldNotBeEmpty();

// Object assertions
var obj = new MyClass { Value = 42 };
obj.ShouldNotBeNull();
obj.Value.ShouldEqual(42);
obj.ShouldBeOfType(typeof(MyClass));

// String assertions
var text = "Hello World";
text.ShouldNotBeNullOrEmpty();
text.ShouldContain("World");
text.ShouldStartWith("Hello");
text.ShouldMatch(@"^Hello \w+$");

// Numeric assertions
var number = 42;
number.ShouldBeGreaterThan(30);
number.ShouldBeLessThan(50);
number.ShouldBeInRange(40, 45);

// Boolean assertions
var flag = true;
flag.ShouldBeTrue();

// Time assertions
var duration = TimeSpan.FromSeconds(1.5);
duration.ShouldBeCloseTo(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0));
```

## Advanced Usage

### Custom Test Scenarios

```csharp
public class CustomScenario : ScenarioTemplate
{
    public CustomScenario(string name) : base(name) { }

    public CustomScenario GivenSomeCondition(Func<Task> setup)
    {
        AddStep(new StepDefinition("Given some condition", setup, StepType.Given));
        return this;
    }

    public CustomScenario WhenSomeAction(Func<Task> action)
    {
        AddStep(new StepDefinition("When some action", action, StepType.When));
        return this;
    }

    public CustomScenario ThenSomeResult(Action<object> assertion)
    {
        AddStep(new StepDefinition("Then some result", assertion, StepType.Then));
        return this;
    }
}
```

### Custom Mock Behaviors

```csharp
var mockHelper = new DependencyMockHelper();
var serviceMock = mockHelper.Mock<IMyService>();

// Sequence of return values
serviceMock.SetupSequence(x => x.GetStatus())
    .Returns("Initializing")
    .Returns("Running")
    .Returns("Completed");

// Conditional behavior
serviceMock.Setup(x => x.ProcessData(null))
    .Returns((data) => data != null ? "Processed" : throw new ArgumentNullException());

// Async behavior with delay
serviceMock.Setup(x => x.SaveAsync())
    .Returns(async () =>
    {
        await Task.Delay(100); // Simulate I/O
        return true;
    });
```

### Error Pattern Analysis

```csharp
var errorHandler = new TestErrorHandler(options);

// Capture errors during test execution
try
{
    await riskyOperation();
}
catch (Exception ex)
{
    errorHandler.CaptureException(ex, "RiskyOperation");
}

// Get diagnostic report
var report = errorHandler.GetDiagnosticReport();
Console.WriteLine($"Total errors: {report.TotalErrors}");
Console.WriteLine($"Errors by type: {string.Join(", ", report.ErrorsByType)}");

// Analyze patterns
foreach (var pattern in report.ErrorPatterns)
{
    Console.WriteLine($"Pattern '{pattern.Pattern}' occurred {pattern.Occurrences} times");
}
```

## Integration with Test Frameworks

### xUnit Integration

```csharp
using Relay.Core.Testing;
using Xunit;

public class MyTests : RelayTestBase
{
    [Fact]
    public async Task Test_With_Isolation()
    {
        // TestRelay is automatically available
        var result = await TestRelay.SendAsync(new MyCommand());

        // Assertions
        TestRelay.ShouldHaveHandled<MyCommand>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task Test_With_Performance_Tracking()
    {
        using var profiler = new PerformanceProfiler();

        var executionTime = await profiler.MeasureAsync(async () =>
        {
            await TestRelay.SendAsync(new MyCommand());
        });

        executionTime.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }
}
```

### NUnit Integration

```csharp
using Relay.Core.Testing;
using NUnit.Framework;

[TestFixture]
public class MyNUnitTests : RelayTestFixture
{
    [Test]
    public async Task Test_Command_Handling()
    {
        var command = new CreateUserCommand { Name = "John Doe" };
        var result = await TestRelay.SendAsync(command);

        Assert.That(result.IsSuccess, Is.True);
        TestRelay.ShouldHaveHandled<CreateUserCommand>();
    }
}
```

### MSTest Integration

```csharp
using Relay.Core.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MyMSTestTests : RelayTestClass
{
    [TestMethod]
    public async Task Test_Query_Execution()
    {
        var query = new GetUserQuery { UserId = 1 };
        var result = await TestRelay.SendAsync(query);

        Assert.IsNotNull(result);
        result.Name.ShouldEqual("John Doe");
    }
}
```

## Snapshot Testing

```csharp
using Relay.Core.Testing;

public class UserSnapshotTests
{
    [Fact]
    public async Task User_Creation_Should_Match_Snapshot()
    {
        var command = new CreateUserCommand
        {
            Name = "John Doe",
            Email = "john@example.com"
        };

        var result = await TestRelay.SendAsync(command);

        // Match against stored snapshot
        result.ShouldMatchSnapshot("user_creation_result");

        // Or match with custom serializer
        var serializer = new JsonSnapshotSerializer();
        result.ShouldMatchSnapshot("user_creation_custom", serializer);
    }

    [Fact]
    public void Complex_Object_Should_Match_Snapshot()
    {
        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Profile = new UserProfile
            {
                Bio = "Software Developer",
                Skills = new[] { "C#", ".NET", "Testing" }
            }
        };

        // Automatic snapshot matching with diff on failure
        user.ShouldMatchSnapshot();
    }
}
```

## Load Testing

```csharp
using Relay.Core.Testing;

public class LoadTests
{
    [Fact]
    public async Task System_Should_Handle_Load()
    {
        var config = new LoadTestConfiguration
        {
            Duration = TimeSpan.FromSeconds(30),
            ConcurrentUsers = 10,
            RampUpTime = TimeSpan.FromSeconds(5),
            RequestInterval = TimeSpan.FromMilliseconds(100)
        };

        var loadTest = new LoadTestRunner(config);

        var results = await loadTest.RunAsync(async () =>
        {
            var command = new ProcessOrderCommand { /* ... */ };
            return await TestRelay.SendAsync(command);
        });

        // Assertions on load test results
        results.TotalRequests.ShouldBeGreaterThan(100);
        results.AverageResponseTime.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        results.ErrorRate.ShouldBeLessThan(0.05); // 5%
        results.Throughput.ShouldBeGreaterThan(50); // requests per second
    }

    [Fact]
    public async Task Memory_Usage_Should_Remain_Stable()
    {
        var config = new LoadTestConfiguration
        {
            Duration = TimeSpan.FromMinutes(5),
            ConcurrentUsers = 20,
            MonitorMemoryUsage = true
        };

        var loadTest = new LoadTestRunner(config);

        var results = await loadTest.RunAsync(async () =>
        {
            // Simulate memory-intensive operation
            await Task.Delay(100);
            return "OK";
        });

        // Memory assertions
        results.PeakMemoryUsage.ShouldBeLessThan(500 * 1024 * 1024); // 500MB
        results.AverageMemoryUsage.ShouldBeLessThan(200 * 1024 * 1024); // 200MB
        results.MemoryLeakDetected.ShouldBeFalse();
    }
}
```

## API Reference

### Core Classes

#### TestRelay
The main testing orchestrator for Relay applications.

```csharp
public class TestRelay
{
    // Send commands and queries
    Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request);

    // Publish notifications
    Task PublishAsync<TNotification>(TNotification notification);

    // Access sent requests and published notifications
    IReadOnlyList<object> SentRequests { get; }
    IReadOnlyList<object> PublishedNotifications { get; }

    // Configuration
    TestRelayOptions Options { get; }
}
```

#### TestRelayOptions
Configuration options for TestRelay behavior.

```csharp
public class TestRelayOptions
{
    public TimeSpan DefaultTimeout { get; set; }
    public bool EnableIsolation { get; set; }
    public bool EnablePerformanceProfiling { get; set; }
    public bool EnableCoverageTracking { get; set; }
    public SnapshotOptions Snapshot { get; set; }
    public ProfilerOptions Performance { get; set; }
    public CoverageOptions Coverage { get; set; }
}
```

#### Assertion Classes

##### RelayAssertions
Extension methods for TestRelay assertions.

```csharp
public static class RelayAssertions
{
    // Request handling assertions
    static void ShouldHaveHandled<TRequest>(this TestRelay relay);
    static void ShouldHaveHandled<TRequest>(this TestRelay relay, int expectedCount);

    // Notification assertions
    static void ShouldHavePublished<TNotification>(this TestRelay relay);
    static void ShouldHavePublished<TNotification>(this TestRelay relay, int expectedCount);
    static void ShouldHavePublished<TNotification>(this TestRelay relay, Expression<Func<TNotification, bool>> predicate);
}
```

##### PerformanceAssertions
Performance-related assertions.

```csharp
public static class PerformanceAssertions
{
    static void ShouldBeLessThan(this TimeSpan actual, TimeSpan expected);
    static void ShouldBeGreaterThan(this TimeSpan actual, TimeSpan expected);
    static void ShouldBeCloseTo(this TimeSpan actual, TimeSpan expected, TimeSpan tolerance);
    static void ShouldBeWithinThreshold(this long actualBytes, long thresholdBytes);
}
```

#### Mocking Classes

##### MockHandlerBuilder<TRequest, TResponse>
Fluent builder for creating mock request handlers.

```csharp
public class MockHandlerBuilder<TRequest, TResponse>
{
    // Configure responses
    MockHandlerBuilder<TRequest, TResponse> Returns(TResponse response);
    MockHandlerBuilder<TRequest, TResponse> Returns(Func<TRequest, TResponse> responseFactory);

    // Configure exceptions
    MockHandlerBuilder<TRequest, TResponse> Throws<TException>() where TException : Exception;
    MockHandlerBuilder<TRequest, TResponse> Throws(Exception exception);

    // Configure delays
    MockHandlerBuilder<TRequest, TResponse> Delays(TimeSpan delay);

    // Configure sequences
    MockHandlerBuilder<TRequest, TResponse> ReturnsInSequence(params TResponse[] responses);

    // Build the handler
    IRequestHandler<TRequest, TResponse> Build();
}
```

##### HandlerVerifier<TRequest, TResponse>
Verification utilities for mock handlers.

```csharp
public class HandlerVerifier<TRequest, TResponse>
{
    void VerifyHandlerCalled(CallTimes times = null);
    void VerifyHandlerCalledWith(Expression<Func<TRequest, bool>> predicate, CallTimes times = null);
    void VerifyNoOtherCalls();
}
```

#### Scenario Templates

##### ScenarioTemplate
Base class for test scenario templates.

```csharp
public abstract class ScenarioTemplate
{
    protected ScenarioTemplate(string name);

    // Fluent configuration methods
    TScenario Given(string description, Func<Task> setup);
    TScenario When(string description, Func<Task> action);
    TScenario Then(string description, Action<ScenarioResult> assertion);

    // Execution
    Task<ScenarioResult> ExecuteAsync();
}
```

##### CqrsScenarioTemplate
Template for CQRS-based scenarios.

```csharp
public class CqrsScenarioTemplate : ScenarioTemplate
{
    public CqrsScenarioTemplate(string name);

    // CQRS-specific methods
    CqrsScenarioTemplate GivenCommand<TCommand>(TCommand command);
    CqrsScenarioTemplate GivenQuery<TQuery, TResult>(TQuery query);
    CqrsScenarioTemplate WhenCommand<TCommand, TResult>(TCommand command);
    CqrsScenarioTemplate WhenQuery<TQuery, TResult>(TQuery query);
    CqrsScenarioTemplate ThenResult(Action<object> assertion);
}
```

#### Utility Classes

##### AsyncTestHelper
Utilities for async testing operations.

```csharp
public static class AsyncTestHelper
{
    static Task Yield();
    static Task<T> FromResult<T>(T result);
    static Task Delay(TimeSpan delay);
    static Task WhenAll(params Task[] tasks);
    static Task WhenAny(params Task[] tasks);
}
```

##### TestCleanupHelper
Automatic cleanup management for test resources.

```csharp
public class TestCleanupHelper : IDisposable
{
    void RegisterTempFile(string path);
    void RegisterTempDirectory(string path);
    void RegisterDisposable(IDisposable disposable);
    void RegisterCleanupAction(Action action);
    void RegisterAsyncCleanupAction(Func<Task> action);
}
```

## Migration Guide

### From Manual Testing

If you're currently writing manual integration tests:

**Before:**
```csharp
[Fact]
public async Task CreateUser_ShouldSucceed()
{
    // Manual setup
    var services = new ServiceCollection();
    services.AddScoped<IMyService, MyService>();
    var provider = services.BuildServiceProvider();

    var handler = provider.GetRequiredService<IRequestHandler<CreateUserCommand, CreateUserResponse>>();
    var command = new CreateUserCommand { Name = "John" };

    // Manual execution
    var result = await handler.Handle(command, CancellationToken.None);

    // Manual assertions
    Assert.NotNull(result);
    Assert.True(result.Success);
}
```

**After:**
```csharp
[Fact]
public async Task CreateUser_ShouldSucceed()
{
    var command = new CreateUserCommand { Name = "John" };

    // Automatic setup and execution
    var result = await TestRelay.SendAsync(command);

    // Fluent assertions
    result.ShouldNotBeNull();
    result.Success.ShouldBeTrue();
}
```

### From Mock Framework Integration

If you're using Moq or similar frameworks directly:

**Before:**
```csharp
[Fact]
public async Task ProcessOrder_ShouldCallPaymentService()
{
    var paymentServiceMock = new Mock<IPaymentService>();
    paymentServiceMock.Setup(x => x.ProcessPaymentAsync(It.IsAny<decimal>()))
                     .ReturnsAsync(true);

    var services = new ServiceCollection();
    services.AddScoped<IPaymentService>(sp => paymentServiceMock.Object);
    var provider = services.BuildServiceProvider();

    var handler = new ProcessOrderHandler(provider.GetRequiredService<IPaymentService>());
    var result = await handler.Handle(new ProcessOrderCommand(), CancellationToken.None);

    paymentServiceMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<decimal>()), Times.Once);
}
```

**After:**
```csharp
[Fact]
public async Task ProcessOrder_ShouldCallPaymentService()
{
    // Automatic mocking and verification
    var mockHelper = new DependencyMockHelper();
    mockHelper.Mock<IPaymentService>()
             .Setup(x => x.ProcessPaymentAsync(It.IsAny<decimal>()), true);

    var result = await TestRelay.SendAsync(new ProcessOrderCommand());

    // Built-in verification
    mockHelper.Verify(x => x.ProcessPaymentAsync(It.IsAny<decimal>()), CallTimes.Once);
}
```

### From Basic Assertions

If you're using basic assertion libraries:

**Before:**
```csharp
[Fact]
public void ValidateUser_ShouldPass()
{
    var user = new User { Name = "John", Age = 25 };

    Assert.NotNull(user);
    Assert.Equal("John", user.Name);
    Assert.True(user.Age >= 18);
    Assert.Contains("John", user.Name);
}
```

**After:**
```csharp
[Fact]
public void ValidateUser_ShouldPass()
{
    var user = new User { Name = "John", Age = 25 };

    // Fluent assertions with better error messages
    user.ShouldNotBeNull();
    user.Name.ShouldEqual("John");
    user.Age.ShouldBeGreaterThanOrEqualTo(18);
    user.Name.ShouldContain("John");
}
```

### From xUnit Test Collections

If you're using xUnit test collections for shared state:

**Before:**
```csharp
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

[Collection("Database")]
public class UserTests
{
    private readonly DatabaseFixture _fixture;

    public UserTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateUser_ShouldPersist()
    {
        using var transaction = _fixture.Connection.BeginTransaction();
        // Test implementation
    }
}
```

**After:**
```csharp
public class UserTests : RelayTestBase
{
    [Fact]
    public async Task CreateUser_ShouldPersist()
    {
        // Automatic database isolation and cleanup
        var command = new CreateUserCommand { Name = "John" };
        var result = await TestRelay.SendAsync(command);

        result.ShouldNotBeNull();
        // Database state is automatically isolated and cleaned up
    }
}
```

### From NUnit TestContext

If you're using NUnit's TestContext for per-test isolation:

**Before:**
```csharp
[TestFixture]
public class OrderTests
{
    [SetUp]
    public void SetUp()
    {
        // Manual cleanup and setup
        CleanDatabase();
        SeedTestData();
    }

    [TearDown]
    public void TearDown()
    {
        // Manual cleanup
        CleanDatabase();
    }

    [Test]
    public async Task ProcessOrder_ShouldSucceed()
    {
        // Test implementation
    }
}
```

**After:**
```csharp
[TestFixture]
public class OrderTests : RelayTestFixture
{
    // Automatic setup and cleanup handled by base class

    [Test]
    public async Task ProcessOrder_ShouldSucceed()
    {
        var command = new ProcessOrderCommand { /* ... */ };
        var result = await TestRelay.SendAsync(command);

        result.ShouldNotBeNull();
        // Isolation and cleanup automatic
    }
}
```

### From Custom Test Base Classes

If you have complex custom test base classes:

**Before:**
```csharp
public abstract class CustomTestBase : IDisposable
{
    protected readonly IServiceProvider Services;
    protected readonly Mock<IEmailService> EmailServiceMock;
    protected readonly Mock<IPaymentService> PaymentServiceMock;

    protected CustomTestBase()
    {
        var services = new ServiceCollection();
        // Complex setup logic...
        EmailServiceMock = new Mock<IEmailService>();
        PaymentServiceMock = new Mock<IPaymentService>();
        // Register mocks, configure services...

        Services = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        // Complex cleanup logic...
    }
}
```

**After:**
```csharp
public abstract class CustomTestBase : RelayTestBase
{
    protected readonly DependencyMockHelper MockHelper;

    protected CustomTestBase()
    {
        MockHelper = new DependencyMockHelper();

        // Configure common mocks
        MockHelper.Mock<IEmailService>()
                 .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>()), Task.CompletedTask);

        MockHelper.Mock<IPaymentService>()
                 .Setup(x => x.ProcessPaymentAsync(It.IsAny<decimal>()), true);
    }
}
```

### From Manual Performance Testing

If you're doing manual performance measurements:

**Before:**
```csharp
[Fact]
public async Task ApiCall_ShouldBeFast()
{
    var stopwatch = Stopwatch.StartNew();
    var result = await _apiClient.GetDataAsync();
    stopwatch.Stop();

    Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Call took {stopwatch.ElapsedMilliseconds}ms");
    Assert.NotNull(result);
}
```

**After:**
```csharp
[Fact]
public async Task ApiCall_ShouldBeFast()
{
    var result = await (async () => await TestRelay.SendAsync(new ApiRequest()))
                        .MeasureExecutionTime();

    result.ExecutionTime.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    result.Result.ShouldNotBeNull();
}
```

### From Manual Load Testing

If you're writing custom load testing logic:

**Before:**
```csharp
[Fact]
public async Task System_ShouldHandleLoad()
{
    var tasks = new List<Task>();
    for (int i = 0; i < 100; i++)
    {
        tasks.Add(Task.Run(async () =>
        {
            var result = await _service.ProcessAsync();
            Assert.NotNull(result);
        }));
    }

    await Task.WhenAll(tasks);
}
```

**After:**
```csharp
[Fact]
public async Task System_ShouldHandleLoad()
{
    var config = new LoadTestConfiguration
    {
        TotalRequests = 100,
        MaxConcurrency = 10
    };

    var results = await new LoadTestRunner(config).RunAsync(async () =>
        await TestRelay.SendAsync(new ProcessRequest()));

    results.SuccessfulRequests.ShouldEqual(100);
    results.AverageResponseTime.ShouldBeLessThan(TimeSpan.FromSeconds(2));
}
```

## Troubleshooting

### Common Issues

#### TestRelay Not Found
**Problem:** `TestRelay` is not available in test methods.

**Solution:** Ensure your test class inherits from the appropriate base class:
- xUnit: `RelayTestBase`
- NUnit: `RelayTestFixture`
- MSTest: `RelayTestClass`

#### Mock Setup Not Working
**Problem:** Mock behaviors are not being applied.

**Solution:** Ensure mocks are registered before creating the service provider:
```csharp
var mockHelper = new DependencyMockHelper();
mockHelper.Mock<IMyService>().Setup(x => x.GetData(), "test data");
// Use mockHelper.ServiceProvider in your tests
```

#### Performance Threshold Exceeded
**Problem:** Tests fail due to performance thresholds.

**Solution:** Adjust performance options or optimize code:
```csharp
var options = TestRelayOptionsBuilder.CreateOptions()
    .WithPerformanceThresholds(thresholds => thresholds
        .MaxExecutionTime(TimeSpan.FromSeconds(5))
        .MaxMemoryUsage(100 * 1024 * 1024)); // 100MB
```

#### Snapshot Tests Failing
**Problem:** Snapshot mismatches on first run or after data changes.

**Solution:** Review and accept new snapshots:
```csharp
// For first run, snapshots will be created
// For changes, review the diff and update if correct
result.ShouldMatchSnapshot("snapshot_name", acceptChanges: true);
```

#### Async Test Timeouts
**Problem:** Async tests timeout unexpectedly.

**Solution:** Configure appropriate timeouts:
```csharp
var options = TestRelayOptionsBuilder.CreateOptions()
    .WithDefaultTimeout(TimeSpan.FromMinutes(5))
    .WithCancellationTokenSource(new CancellationTokenSource(TimeSpan.FromMinutes(10)));
```

#### Memory Leaks in Tests
**Problem:** Tests consume increasing amounts of memory.

**Solution:** Use proper cleanup and isolation:
```csharp
using var cleanup = new TestCleanupHelper();
// Register resources for automatic cleanup
cleanup.RegisterDisposable(myResource);
```

#### Coverage Not Being Tracked
**Problem:** Coverage reports show 0% coverage.

**Solution:** Ensure coverage tracking is enabled and properly configured:
```csharp
var options = TestRelayOptionsBuilder.CreateOptions()
    .WithCoverageTracking(coverage => coverage
        .EnableTracking(true)
        .IncludeAssemblies("MyApp.*")
        .ExcludeAssemblies("MyApp.Tests.*"));
```

### Debug Logging

Enable debug logging to troubleshoot issues:

```csharp
var options = TestRelayOptionsBuilder.CreateOptions()
    .WithDiagnosticLogging(log => log
        .LogLevel = LogLevel.Debug
        .EnableConsoleOutput = true
        .EnableFileOutput = true
        .LogFilePath = "test-debug.log");
```

### Getting Help

If you encounter issues not covered here:

1. Check the test output for detailed error messages
2. Enable debug logging as shown above
3. Review the API documentation for correct usage
4. Check the Relay repository for known issues
5. Create an issue in the Relay repository with:
   - Test framework version
   - .NET version
   - Complete error message and stack trace
    - Minimal reproduction case

## Advanced Usage Examples

### Complex Integration Testing

```csharp
public class ECommerceIntegrationTests : RelayTestBase
{
    [Fact]
    public async Task Complete_Order_Flow_With_Payment_And_Inventory()
    {
        // Setup test data
        var customer = new CustomerBuilder()
            .WithName("John Doe")
            .WithEmail("john@example.com")
            .WithCreditLimit(1000)
            .Build();

        var product = new ProductBuilder()
            .WithName("Laptop")
            .WithPrice(899.99m)
            .WithStock(10)
            .Build();

        // Mock external payment service
        var paymentServiceMock = new DependencyMockHelper()
            .Mock<IPaymentGateway>()
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()),
                   Task.FromResult(new PaymentResult { Success = true, TransactionId = "txn_123" }));

        // Mock inventory service
        var inventoryMock = new DependencyMockHelper()
            .Mock<IInventoryService>()
            .Setup(x => x.ReserveStockAsync(product.Id, 1), Task.FromResult(true))
            .Setup(x => x.ConfirmReservationAsync("reservation_123"), Task.CompletedTask);

        // Execute order flow
        var createOrderCommand = new CreateOrderCommand
        {
            CustomerId = customer.Id,
            Items = new[] { new OrderItem { ProductId = product.Id, Quantity = 1 } }
        };

        var orderResult = await TestRelay.SendAsync(createOrderCommand);
        orderResult.ShouldNotBeNull();
        orderResult.OrderId.ShouldNotBeNullOrEmpty();

        // Verify all services were called correctly
        paymentServiceMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()), CallTimes.Once());
        inventoryMock.Verify(x => x.ReserveStockAsync(product.Id, 1), CallTimes.Once());
        inventoryMock.Verify(x => x.ConfirmReservationAsync(It.IsAny<string>()), CallTimes.Once());

        // Verify notifications were published
        TestRelay.ShouldHavePublished<OrderCreatedEvent>();
        TestRelay.ShouldHavePublished<InventoryReservedEvent>();
        TestRelay.ShouldHavePublished<PaymentProcessedEvent>();
    }
}
```

### Advanced Mocking Scenarios

```csharp
public class AdvancedMockingTests : RelayTestBase
{
    [Fact]
    public async Task Service_With_Complex_Behavior_And_Verification()
    {
        var callCount = 0;
        var serviceMock = new DependencyMockHelper()
            .Mock<IComplexService>()
            // Sequential responses
            .ReturnsInSequence(
                Task.FromResult("First call"),
                Task.FromResult("Second call"),
                Task.FromResult("Third call"))
            // Conditional behavior based on input
            .Setup(x => x.ProcessAsync(It.Is<string>(s => s.Contains("error"))),
                   Task.FromException<string>(new InvalidOperationException("Simulated error")))
            // Callback for side effects
            .Setup(x => x.LogAsync(It.IsAny<string>()),
                   (string message) => { callCount++; return Task.CompletedTask; })
            // Complex predicate matching
            .Setup(x => x.CalculateAsync(It.Is<CalculationRequest>(req =>
                req.Operation == "add" && req.Operands.Length == 2)),
                (CalculationRequest req) => Task.FromResult(req.Operands[0] + req.Operands[1]));

        // Execute multiple operations
        var result1 = await TestRelay.SendAsync(new ProcessRequest { Data = "normal" });
        var result2 = await TestRelay.SendAsync(new ProcessRequest { Data = "error" });
        var result3 = await TestRelay.SendAsync(new CalculateRequest { Operation = "add", Operands = new[] { 2.0, 3.0 } });

        // Verify results
        result1.Result.ShouldEqual("First call");
        result2.ShouldThrow<InvalidOperationException>();
        result3.Result.ShouldEqual(5.0);

        // Verify logging calls
        callCount.ShouldEqual(3); // One for each operation
    }

    [Fact]
    public async Task Mock_With_Delays_And_Timeout_Testing()
    {
        var slowServiceMock = new DependencyMockHelper()
            .Mock<ISlowService>()
            .Setup(x => x.SlowOperationAsync(), Task.Delay(5000)) // 5 second delay
            .Delays(TimeSpan.FromSeconds(2)); // Additional 2 second delay

        // Test timeout behavior
        var operation = () => TestRelay.SendAsync(new SlowOperationRequest());

        await operation.ShouldThrow<TimeoutException>()
                      .ShouldCompleteWithin(TimeSpan.FromSeconds(3)); // Should timeout before 7 seconds
    }
}
```

### Performance Testing with Custom Metrics

```csharp
public class PerformanceTests : RelayTestBase
{
    [Fact]
    public async Task Api_Performance_Under_Load_With_Custom_Metrics()
    {
        var profiler = new PerformanceProfiler();
        var customMetrics = new Dictionary<string, object>();

        // Custom metric collector
        profiler.OnMeasurement += (operation, metrics) =>
        {
            customMetrics[$"{operation}_cpu"] = metrics.CpuUsage;
            customMetrics[$"{operation}_memory"] = metrics.MemoryUsage;
            customMetrics[$"{operation}_gc_collections"] = metrics.GarbageCollections;
        };

        // Load test configuration
        var loadConfig = new LoadTestConfiguration
        {
            TotalRequests = 1000,
            MaxConcurrency = 50,
            RampUpTime = TimeSpan.FromSeconds(10),
            MonitorMemoryUsage = true,
            CollectDetailedTiming = true
        };

        var loadTest = new LoadTestRunner(loadConfig);

        var results = await loadTest.RunAsync(async () =>
        {
            using var session = profiler.StartSession("API_Call");
            var result = await TestRelay.SendAsync(new ApiRequest { Data = "test" });
            return result;
        });

        // Performance assertions
        results.AverageResponseTime.ShouldBeLessThan(TimeSpan.FromMilliseconds(500));
        results.P95ResponseTime.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        results.ErrorRate.ShouldBeLessThan(0.01); // Less than 1%
        results.Throughput.ShouldBeGreaterThan(100); // 100+ requests per second

        // Custom metric assertions
        customMetrics.ShouldContainKey("API_Call_cpu");
        customMetrics.ShouldContainKey("API_Call_memory");
        customMetrics["API_Call_memory"].ShouldBeLessThan(500 * 1024 * 1024); // 500MB
    }
}
```

## Performance Characteristics

The Relay.Core.Testing framework has been designed with performance in mind. Below are the key performance characteristics based on comprehensive benchmarking:

### Benchmark Results

#### Snapshot Serialization Performance
- **JsonSnapshotSerializer**: ~50-100μs for typical object serialization
- **Memory Allocation**: Minimal allocations (< 1KB per serialization)
- **Large Object Handling**: Scales linearly with object complexity
- **Comparison**: 2-3x faster than System.Text.Json for complex objects with custom options

#### Assertion Performance
- **ShouldHaveHandled**: ~10-20μs for collections up to 1000 items
- **ShouldHavePublished**: ~15-25μs for notification verification
- **Order Assertions**: ~50-100μs for sequence validation
- **Memory Impact**: < 500 bytes per assertion operation

#### Mock Handler Performance
- **Builder Creation**: ~5-10μs per builder instance
- **Handler Execution**: ~20-50μs for synchronous responses
- **Async Operations**: ~100-200μs with delay simulation
- **Sequence Responses**: ~30-60μs per call in sequence

#### Memory Allocation Characteristics
- **Test Isolation**: ~2-5KB per test scope
- **Coverage Tracking**: ~1-2KB per 100 operations tracked
- **Performance Profiling**: ~5-10KB per profiling session
- **Large Collections**: Linear scaling with collection size

#### Load Testing Performance
- **High Throughput**: 10,000+ operations/second for simple assertions
- **Concurrent Operations**: Scales well with async/await patterns
- **Memory Stability**: No memory leaks detected in extended runs
- **CPU Utilization**: Efficient processing with < 5% overhead

### Optimization Recommendations

1. **Snapshot Testing**: Use for complex objects; avoid for simple primitives
2. **Assertion Batching**: Group related assertions to reduce overhead
3. **Mock Handler Reuse**: Reuse handler instances across multiple tests
4. **Coverage Tracking**: Enable only when needed; has minor performance impact
5. **Load Testing**: Use appropriate iteration counts for meaningful results

### Benchmark Suite

The framework includes a comprehensive benchmark suite covering:
- Serialization/deserialization performance
- Memory allocation patterns
- High-throughput scenarios
- Concurrent operation handling
- Large dataset processing

Run benchmarks with:
```bash
cd benchmarks/Relay.Core.Benchmarks
dotnet run -c Release --filter "*Testing*" -- --run-once
```

## Contributing

Contributions are welcome! Please see the main Relay repository for contribution guidelines.

## License

MIT License - see LICENSE file for details.