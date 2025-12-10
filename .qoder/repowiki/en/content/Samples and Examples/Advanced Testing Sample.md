# Advanced Testing Sample

<cite>
**Referenced Files in This Document**   
- [Relay.Core.Testing.csproj](file://src/Relay.Core.Testing/Relay.Core.Testing.csproj)
- [README.md](file://src/Relay.Core.Testing/README.md)
- [TestRelay.cs](file://src/Relay.Core.Testing/Core/TestRelay.cs)
- [RelayTestBase.cs](file://src/Relay.Core.Testing/Core/RelayTestBase.cs)
- [RelayTestFramework.cs](file://src/Relay.Core.Testing/Core/RelayTestFramework.cs)
- [LoadTestConfiguration.cs](file://src/Relay.Core.Testing/Configuration/LoadTestConfiguration.cs)
- [LoadTestResult.cs](file://src/Relay.Core.Testing/Results/LoadTestResult.cs)
- [TestScenarioBuilder.cs](file://src/Relay.Core.Testing/Builders/TestScenarioBuilder.cs)
- [ScenarioTemplate.cs](file://src/Relay.Core.Testing/Scenarios/ScenarioTemplate.cs)
- [UserTests.cs](file://samples/Relay.Core.Testing.Sample/UserTests.cs)
- [ScenarioTests.cs](file://samples/Relay.Core.Testing.Sample/ScenarioTests.cs)
- [Program.cs](file://samples/Relay.Core.Testing.Sample/Program.cs)
- [README.md](file://samples/Relay.Core.Testing.Sample/README.md)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Core Testing Components](#core-testing-components)
3. [Test Organization and Structure](#test-organization-and-structure)
4. [Mocking and Dependency Management](#mocking-and-dependency-management)
5. [Scenario-Based Testing](#scenario-based-testing)
6. [Performance and Load Testing](#performance-and-load-testing)
7. [Error Handling and Recovery Testing](#error-handling-and-recovery-testing)
8. [Advanced Testing Utilities](#advanced-testing-utilities)
9. [Best Practices and Patterns](#best-practices-and-patterns)
10. [Conclusion](#conclusion)

## Introduction

The Relay.Core.Testing framework provides a comprehensive suite of tools for testing Relay-based applications, enabling developers to create robust, maintainable, and high-performance tests. This document explores the advanced testing capabilities demonstrated in the Relay.Core.Testing.Sample application, focusing on sophisticated testing patterns, scenario-based testing, performance validation, and error handling verification.

The framework supports multiple testing frameworks (xUnit, NUnit, MSTest) and offers specialized utilities for async testing, test isolation, dependency mocking, and performance profiling. The sample application showcases real-world testing scenarios, from basic unit tests to complex integration tests that validate complete workflows and system behavior under load.

**Section sources**
- [README.md](file://samples/Relay.Core.Testing.Sample/README.md#L1-L179)
- [Program.cs](file://samples/Relay.Core.Testing.Sample/Program.cs#L1-L20)

## Core Testing Components

The Relay.Core.Testing framework is built around several core components that work together to provide a seamless testing experience. At the heart of the framework is the `TestRelay` class, which implements the `IRelay` interface and provides test-specific functionality for capturing requests, notifications, and responses.

The `TestRelay` class maintains collections of sent requests and published notifications, allowing tests to verify that the correct messages were sent through the system. It also supports setting up custom handlers for specific request types, enabling precise control over test behavior and response generation.

```mermaid
classDiagram
class TestRelay {
+IReadOnlyCollection<object> PublishedNotifications
+IReadOnlyCollection<object> SentRequests
+ValueTask<TResponse> SendAsync[TResponse](IRequest[TResponse] request, CancellationToken cancellationToken)
+ValueTask SendAsync(IRequest request, CancellationToken cancellationToken)
+IAsyncEnumerable<TResponse> StreamAsync[TResponse](IStreamRequest[TResponse] request, CancellationToken cancellationToken)
+ValueTask PublishAsync[TNotification](TNotification notification, CancellationToken cancellationToken)
+void SetupRequestHandler[TRequest, TResponse](Func[TRequest, CancellationToken, ValueTask[TResponse]] handler)
+void SetupRequestHandler[TRequest](Func[TRequest, CancellationToken, ValueTask] handler)
+void SetupStreamHandler[TRequest, TResponse](Func[TRequest, CancellationToken, IAsyncEnumerable[TResponse]] handler)
+void SetupNotificationHandler[TNotification](Func[TNotification, CancellationToken, ValueTask] handler)
+void Clear()
+void ClearHandlers()
+IEnumerable[T] GetPublishedNotifications[T]()
+IEnumerable[T] GetSentRequests[T]()
}
class RelayTestBase {
+TestRelay TestRelay
+IServiceProvider Services
+Task InitializeAsync()
+Task DisposeAsync()
+Task<ScenarioResult> RunScenarioAsync(string scenarioName, Action[TestScenarioBuilder] configureScenario)
+void AssertScenarioSuccess(ScenarioResult result)
+void AssertScenarioFailure[TException](ScenarioResult result)
}
TestRelay --> RelayTestBase : "used by"
```

**Diagram sources**
- [TestRelay.cs](file://src/Relay.Core.Testing/Core/TestRelay.cs#L1-L234)
- [RelayTestBase.cs](file://src/Relay.Core.Testing/Core/RelayTestBase.cs#L1-L178)

**Section sources**
- [TestRelay.cs](file://src/Relay.Core.Testing/Core/TestRelay.cs#L1-L234)
- [RelayTestBase.cs](file://src/Relay.Core.Testing/Core/RelayTestBase.cs#L1-L178)

## Test Organization and Structure

The Relay.Core.Testing framework promotes a structured approach to test organization through the use of base classes and inheritance hierarchies. The `RelayTestBase` class serves as the foundation for all tests, implementing `IAsyncLifetime` to ensure proper initialization and cleanup of test resources.

Tests are organized into logical groups based on functionality, with each test class inheriting from `RelayTestBase` and focusing on a specific domain area. The sample application demonstrates this pattern with test classes like `UserTests` and `ScenarioTests`, each containing multiple test methods that validate different aspects of the system.

The framework supports both xUnit and other testing frameworks through conditional compilation, allowing developers to choose their preferred testing approach. Test methods are typically annotated with framework-specific attributes like `[Fact]` or `[Test]` to indicate executable test cases.

```mermaid
classDiagram
class RelayTestBase {
<<abstract>>
+TestRelay TestRelay
+IServiceProvider Services
+Task InitializeAsync()
+Task DisposeAsync()
+Task<ScenarioResult> RunScenarioAsync(string scenarioName, Action[TestScenarioBuilder] configureScenario)
+void AssertScenarioSuccess(ScenarioResult result)
+void AssertScenarioFailure[TException](ScenarioResult result)
}
class UserTests {
+Task CreateUser_ShouldCreateUserAndSendWelcomeEmail()
+Task UpdateUser_ShouldUpdateUserAndSendNotificationEmail()
+Task GetUserById_ShouldReturnUser()
+Task GetAllUsers_ShouldReturnAllUsers()
+Task CreateUser_ShouldHandleExceptionFromRepository()
+Task UpdateUser_ShouldHandleUserNotFound()
}
class ScenarioTests {
+Task UserLifecycleScenario_ShouldHandleCompleteUserWorkflow()
+Task ProductCreationScenario_ShouldCreateProductAndVerify()
+Task ErrorHandlingScenario_ShouldHandleAndRecoverFromErrors()
+Task PerformanceScenario_ShouldMeetPerformanceRequirements()
}
RelayTestBase <|-- UserTests
RelayTestBase <|-- ScenarioTests
```

**Diagram sources**
- [RelayTestBase.cs](file://src/Relay.Core.Testing/Core/RelayTestBase.cs#L1-L178)
- [UserTests.cs](file://samples/Relay.Core.Testing.Sample/UserTests.cs#L1-L254)
- [ScenarioTests.cs](file://samples/Relay.Core.Testing.Sample/ScenarioTests.cs#L1-L207)

**Section sources**
- [RelayTestBase.cs](file://src/Relay.Core.Testing/Core/RelayTestBase.cs#L1-L178)
- [UserTests.cs](file://samples/Relay.Core.Testing.Sample/UserTests.cs#L1-L254)
- [ScenarioTests.cs](file://samples/Relay.Core.Testing.Sample/ScenarioTests.cs#L1-L207)

## Mocking and Dependency Management

The Relay.Core.Testing framework provides robust support for dependency injection and mocking, allowing tests to isolate components and verify interactions. The `Services` property in `RelayTestBase` exposes an `IServiceProvider` that can be used to register mock implementations of dependencies.

The sample application demonstrates this approach by using Moq to create mock instances of repositories and services, which are then registered with the test's service provider. This enables tests to control the behavior of dependencies and verify that they are called with the expected parameters.

```mermaid
sequenceDiagram
participant Test as "UserTests"
participant ServiceProvider as "IServiceProvider"
participant MockRepo as "Mock<IUserRepository>"
participant MockEmail as "Mock<IEmailService>"
participant Relay as "TestRelay"
Test->>MockRepo : Setup(x => x.CreateAsync(It.IsAny<User>()))
Test->>MockEmail : Setup(x => x.SendWelcomeEmailAsync(email, name))
Test->>ServiceProvider : AddSingleton(mockUserRepo.Object)
Test->>ServiceProvider : AddSingleton(mockEmailService.Object)
Test->>Relay : SendCommand(createCommand)
Relay->>MockRepo : CreateAsync(user)
Relay->>MockEmail : SendWelcomeEmailAsync(email, name)
MockRepo-->>Relay : Returns created user
MockEmail-->>Relay : Task.CompletedTask
Relay-->>Test : Returns result
Test->>MockRepo : Verify(x => x.CreateAsync(It.Is<User>(u => ...)), Times.Once)
Test->>MockEmail : Verify(x => x.SendWelcomeEmailAsync(email, name), Times.Once)
```

**Diagram sources**
- [UserTests.cs](file://samples/Relay.Core.Testing.Sample/UserTests.cs#L1-L254)
- [RelayTestBase.cs](file://src/Relay.Core.Testing/Core/RelayTestBase.cs#L1-L178)

**Section sources**
- [UserTests.cs](file://samples/Relay.Core.Testing.Sample/UserTests.cs#L1-L254)

## Scenario-Based Testing

The Relay.Core.Testing framework excels at scenario-based testing, allowing developers to define and execute complex workflows that span multiple operations and system components. The `RunScenarioAsync` method in `RelayTestBase` provides a fluent API for building test scenarios with clear separation of setup, execution, and verification steps.

Scenarios are constructed using the `TestScenarioBuilder` class, which supports various step types including sending requests, publishing notifications, verifying conditions, and waiting for asynchronous operations to complete. This approach makes tests more readable and maintainable by clearly expressing the intended workflow.

```mermaid
flowchart TD
Start([Scenario Start]) --> Given["Given - Setup initial state"]
Given --> When1["When - Execute user lifecycle steps"]
When1 --> CreateUser["Create user"]
CreateUser --> UpdateUser["Update user"]
UpdateUser --> RetrieveUser["Retrieve user"]
RetrieveUser --> Then["Then - Verify results"]
Then --> Assert["Assert scenario success"]
Assert --> End([Scenario Complete])
style Start fill:#f9f,stroke:#333
style End fill:#f9f,stroke:#333
style Given fill:#bbf,stroke:#333,color:#fff
style When1 fill:#bbf,stroke:#333,color:#fff
style Then fill:#bbf,stroke:#333,color:#fff
```

**Diagram sources**
- [ScenarioTests.cs](file://samples/Relay.Core.Testing.Sample/ScenarioTests.cs#L1-L207)
- [RelayTestBase.cs](file://src/Relay.Core.Testing/Core/RelayTestBase.cs#L1-L178)
- [TestScenarioBuilder.cs](file://src/Relay.Core.Testing/Builders/TestScenarioBuilder.cs#L1-L94)

**Section sources**
- [ScenarioTests.cs](file://samples/Relay.Core.Testing.Sample/ScenarioTests.cs#L1-L207)

## Performance and Load Testing

The Relay.Core.Testing framework includes comprehensive support for performance and load testing, enabling developers to validate system behavior under realistic conditions. The `LoadTestConfiguration` class provides a rich set of options for configuring load tests, including total requests, maximum concurrency, ramp-up time, and monitoring options.

The `RelayTestFramework` class exposes the `RunLoadTestAsync` method, which executes load tests against specified request types and returns detailed performance metrics. These metrics include response time percentiles (P95, P99), throughput (requests per second), error rates, and memory usage statistics.

```mermaid
classDiagram
class LoadTestConfiguration {
+int TotalRequests
+int MaxConcurrency
+int RampUpDelayMs
+TimeSpan Duration
+int ConcurrentUsers
+TimeSpan RampUpTime
+TimeSpan RequestInterval
+bool MonitorMemoryUsage
+bool CollectDetailedTiming
+TimeSpan WarmUpDuration
}
class LoadTestResult {
+string RequestType
+DateTime StartedAt
+DateTime? CompletedAt
+TimeSpan TotalDuration
+LoadTestConfiguration Configuration
+int SuccessfulRequests
+int FailedRequests
+List[TimeSpan] ResponseTimes
+TimeSpan AverageResponseTime
+TimeSpan MedianResponseTime
+TimeSpan P95ResponseTime
+TimeSpan P99ResponseTime
+long PeakMemoryUsage
+long AverageMemoryUsage
+bool MemoryLeakDetected
+double RequestsPerSecond
+double ErrorRate
+double SuccessRate
}
class RelayTestFramework {
+TestScenarioBuilder Scenario(string name)
+Task[TestRunResult] RunAllScenariosAsync(CancellationToken cancellationToken)
+Task[LoadTestResult] RunLoadTestAsync[TRequest](TRequest request, LoadTestConfiguration config, CancellationToken cancellationToken)
+Task[ScenarioResult] RunScenarioAsync(TestScenario scenario, CancellationToken cancellationToken)
}
RelayTestFramework --> LoadTestConfiguration : "uses"
RelayTestFramework --> LoadTestResult : "returns"
LoadTestConfiguration --> LoadTestResult : "configures"
```

**Diagram sources**
- [LoadTestConfiguration.cs](file://src/Relay.Core.Testing/Configuration/LoadTestConfiguration.cs#L1-L106)
- [LoadTestResult.cs](file://src/Relay.Core.Testing/Results/LoadTestResult.cs#L1-L45)
- [RelayTestFramework.cs](file://src/Relay.Core.Testing/Core/RelayTestFramework.cs#L31-L178)

**Section sources**
- [LoadTestConfiguration.cs](file://src/Relay.Core.Testing/Configuration/LoadTestConfiguration.cs#L1-L106)
- [LoadTestResult.cs](file://src/Relay.Core.Testing/Results/LoadTestResult.cs#L1-L45)

## Error Handling and Recovery Testing

The Relay.Core.Testing framework provides robust support for testing error conditions and recovery scenarios. Tests can verify that the system handles exceptions correctly, maintains data consistency, and provides appropriate error responses to clients.

The sample application demonstrates several error handling patterns, including testing repository exceptions, handling user not found scenarios, and validating error recovery mechanisms. The framework's assertion methods make it easy to verify that specific exception types are thrown and that error messages contain expected content.

```mermaid
sequenceDiagram
participant Test as "UserTests"
participant MockRepo as "Mock<IUserRepository>"
participant Relay as "TestRelay"
Test->>MockRepo : Setup(x => x.CreateAsync(It.IsAny<User>())).ThrowsAsync(exception)
Test->>Relay : SendCommand(createCommand)
Relay->>MockRepo : CreateAsync(user)
MockRepo-->>Relay : Throws InvalidOperationException
Relay-->>Test : Throws InvalidOperationException
Test->>Test : Assert.ThrowsAsync<InvalidOperationException>()
Test->>Test : Assert.Equal("Database error", exception.Message)
Test->>MockRepo : Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once)
```

**Diagram sources**
- [UserTests.cs](file://samples/Relay.Core.Testing.Sample/UserTests.cs#L1-L254)

**Section sources**
- [UserTests.cs](file://samples/Relay.Core.Testing.Sample/UserTests.cs#L1-L254)

## Advanced Testing Utilities

The Relay.Core.Testing framework includes a variety of advanced utilities that enhance the testing experience and enable sophisticated test scenarios. These utilities include performance assertions, scenario templates, and comprehensive assertion helpers.

The framework provides specialized assertion methods for validating performance characteristics, such as `ShouldMeetPerformanceExpectations`, `ShouldHaveThroughputOf`, and `ShouldCompleteWithin`. These methods make it easy to enforce performance requirements in tests and ensure that the system meets its SLAs.

```mermaid
classDiagram
class PerformanceAssertions {
+void ShouldMeetPerformanceExpectations(this LoadTestResult result, TimeSpan? maxAverageResponseTime, double? maxErrorRate, double? minRequestsPerSecond)
+void ShouldHaveThroughputOf(this LoadTestResult result, double minRequestsPerSecond)
+void ShouldHaveLowErrorRate(this LoadTestResult result, double maxErrorRate)
+void ShouldCompleteWithin(this LoadTestResult result, TimeSpan maxDuration)
+void ShouldHaveResponseTimePercentile(this LoadTestResult result, TimeSpan maxResponseTime, string percentile)
}
class ScenarioTemplate {
<<abstract>>
+string ScenarioName
+TestScenario Scenario
+IRelay Relay
+ScenarioTemplate WithSetup(Func[Task] setupAction)
+ScenarioTemplate WithTeardown(Func[Task] teardownAction)
+abstract void BuildScenario()
+virtual void CustomizeScenario(TestScenarioBuilder builder)
}
class CqrsScenarioTemplate {
+CqrsScenarioTemplate SendCommand[TCommand](TCommand command, string stepName)
+CqrsScenarioTemplate SendQuery[TQuery, TResponse](TQuery query, string stepName)
+CqrsScenarioTemplate VerifyState(Func[Task[bool]] verification, string stepName)
+CqrsScenarioTemplate WaitForProcessing(TimeSpan duration, string stepName)
+override void BuildScenario()
}
class EventDrivenScenarioTemplate {
+EventDrivenScenarioTemplate SendRequest[TRequest, TResponse](TRequest request, string stepName)
+EventDrivenScenarioTemplate PublishNotification[TNotification](TNotification notification, string stepName)
+EventDrivenScenarioTemplate WaitForEvent[TEvent](TimeSpan timeout, string stepName)
+override void BuildScenario()
}
ScenarioTemplate <|-- CqrsScenarioTemplate
ScenarioTemplate <|-- EventDrivenScenarioTemplate
PerformanceAssertions --> LoadTestResult : "extends"
```

**Diagram sources**
- [PerformanceAssertions.cs](file://src/Relay.Core.Testing/Assertions/PerformanceAssertions.cs#L173-L282)
- [ScenarioTemplate.cs](file://src/Relay.Core.Testing/Scenarios/ScenarioTemplate.cs#L1-L109)
- [CqrsScenarioTemplate.cs](file://src/Relay.Core.Testing/Scenarios/CqrsScenarioTemplate.cs#L67-L110)
- [EventDrivenScenarioTemplate.cs](file://src/Relay.Core.Testing/Scenarios/EventDrivenScenarioTemplate.cs#L103-L130)

**Section sources**
- [PerformanceAssertions.cs](file://src/Relay.Core.Testing/Assertions/PerformanceAssertions.cs#L173-L282)
- [ScenarioTemplate.cs](file://src/Relay.Core.Testing/Scenarios/ScenarioTemplate.cs#L1-L109)

## Best Practices and Patterns

The Relay.Core.Testing framework encourages several best practices and patterns that lead to more effective and maintainable tests. These include test isolation, clear naming conventions, scenario organization, and comprehensive error coverage.

Key patterns demonstrated in the sample application include:
- **Test Isolation**: Each test is independent with proper cleanup, ensuring that tests do not interfere with each other.
- **Mock Verification**: Exact behavior of dependencies is verified, ensuring that components interact correctly.
- **Scenario Organization**: Related operations are grouped into scenarios, making complex workflows easier to understand and maintain.
- **Error Coverage**: Both success and failure paths are tested, ensuring robust error handling.
- **Performance Awareness**: Performance requirements are included in tests, ensuring that the system meets its performance goals.
- **Maintainable Tests**: Clear naming and organization conventions make tests easy to read and understand.

The framework also supports advanced patterns like setup and teardown actions, custom scenario templates, and reusable test components, enabling teams to build a comprehensive testing library that can be shared across projects.

**Section sources**
- [README.md](file://samples/Relay.Core.Testing.Sample/README.md#L1-L179)

## Conclusion

The Relay.Core.Testing framework provides a powerful and flexible foundation for testing Relay-based applications. By combining robust mocking capabilities, scenario-based testing, performance validation, and comprehensive error handling, the framework enables developers to create high-quality tests that ensure system reliability and performance.

The sample application demonstrates how to effectively use these capabilities to test complex workflows, validate performance characteristics, and verify error recovery mechanisms. By following the patterns and best practices shown in the sample, teams can build a comprehensive test suite that provides confidence in their application's correctness and reliability.

The framework's support for multiple testing frameworks, extensible architecture, and rich feature set make it an excellent choice for teams looking to improve their testing practices and ensure the quality of their Relay-based applications.