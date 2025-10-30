# Chaos Engineering Tests

This directory contains chaos engineering tests for the Relay.MessageBroker component. These tests validate system resilience under various failure conditions and stress scenarios.

## Test Categories

### Circuit Breaker Chaos Tests (`CircuitBreakerChaosTests.cs`)

Tests circuit breaker behavior under failure conditions and sustained load:

- **Opens after failure threshold under sustained load** - Validates circuit opens when failure threshold is reached during high load
- **Transitions to half-open after timeout** - Verifies circuit transitions to half-open state after configured timeout
- **Closes after success threshold in half-open state** - Confirms circuit closes after successful operations in half-open state
- **Behaves correctly under sustained load** - Tests circuit breaker with mixed success/failure operations
- **Handles rapid state transitions** - Validates circuit breaker during rapid open/close cycles
- **Maintains correct metrics under concurrent load** - Ensures metrics accuracy during concurrent operations
- **Recovers properly after extended outage** - Tests recovery after prolonged failure period
- **Handles intermittent failures under load** - Validates behavior with sporadic failures

### Broker Chaos Tests (`BrokerChaosTests.cs`)

Tests message broker resilience under various chaos conditions:

#### Connection Failures
- **Broker connection failure during publish** - Handles connection loss during message publishing
- **Broker connection failure during consume** - Recovers from connection failures during message consumption

#### Network Conditions
- **Network latency injection** (100ms, 500ms, 1000ms) - Tests performance impact of network latency
- **Network packet loss** (5%, 10%, 20%) - Validates retry logic under packet loss conditions

#### Resource Exhaustion
- **Memory pressure throttles operations** - Tests throttling under memory constraints
- **Connection pool depletion** - Handles connection pool exhaustion gracefully
- **CPU pressure degrades performance** - Validates behavior under CPU-intensive conditions

#### Combined Chaos
- **Multiple failure types** - Tests system resilience with combined chaos conditions (connection failures, latency, packet loss)

### Message Delivery Guarantees Chaos Tests (`MessageDeliveryGuaranteesChaosTests.cs`)

Tests message delivery guarantees under failure conditions:

#### At-Least-Once Delivery
- **With broker failures** - Ensures all messages are delivered despite broker failures

#### Message Ordering
- **With network partitions** - Maintains message order within partitions during network issues

#### Deduplication
- **With duplicate deliveries** - Processes each message exactly once despite duplicates

#### Poison Message Handling
- **With repeatedly failing messages** - Moves failing messages to poison queue after threshold

#### High Concurrency
- **Under high concurrency** - Maintains delivery guarantees during concurrent operations
- **With transient failures** - Eventually succeeds despite transient failures

## Running the Tests

Run all chaos tests:
```bash
dotnet test --filter "Category=Chaos"
```

Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~CircuitBreakerChaosTests"
```

Run specific test:
```bash
dotnet test --filter "FullyQualifiedName~CircuitBreakerChaosTests.CircuitBreaker_OpensAfterFailureThreshold_UnderSustainedLoad"
```

## Test Patterns

### Simulated Failures

Tests use custom broker implementations that simulate various failure conditions:

- `UnstableConnectionBroker` - Simulates connection failures with configurable failure rate
- `LatencyInjectionBroker` - Adds artificial latency to operations
- `PacketLossBroker` - Simulates packet loss
- `ResourceConstrainedBroker` - Limits concurrent operations
- `ConnectionPoolExhaustedBroker` - Simulates connection pool exhaustion
- `CPUPressureBroker` - Adds CPU-intensive operations
- `CombinedChaosBroker` - Combines multiple chaos conditions

### Retry Logic

Most tests implement retry logic to validate resilience:

```csharp
var retryCount = 0;
while (!success && retryCount < maxRetries)
{
    try
    {
        await operation();
        success = true;
    }
    catch (TransientException)
    {
        retryCount++;
        await Task.Delay(backoffDelay);
    }
}
```

### Metrics Validation

Tests validate system metrics under chaos conditions:

```csharp
var metrics = circuitBreaker.Metrics;
Assert.Equal(expectedTotal, metrics.TotalCalls);
Assert.True(metrics.SuccessfulCalls > 0);
```

## Requirements Coverage

These tests cover the following requirements from the design document:

- **Requirement 18.1**: Circuit breaker behavior under failure conditions
- **Requirement 18.2**: Broker connection failures during publish and consume
- **Requirement 18.3**: Network latency and packet loss scenarios
- **Requirement 18.4**: Resource exhaustion (memory, CPU, connections)
- **Requirement 18.5**: Message delivery guarantees during failures

## Test Statistics

- **Total Tests**: 26
- **Circuit Breaker Tests**: 8
- **Broker Chaos Tests**: 11
- **Message Delivery Tests**: 7
- **Average Execution Time**: ~27 seconds

## Notes

- Tests use in-memory implementations for fast execution
- Chaos conditions are simulated rather than using real infrastructure failures
- Tests are designed to be deterministic and repeatable
- Some tests include configurable delays to allow for async operations to complete
- All tests include output logging for debugging purposes
