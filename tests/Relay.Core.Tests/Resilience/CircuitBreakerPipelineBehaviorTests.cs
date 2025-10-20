using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Resilience;
using Relay.Core.Resilience.CircuitBreaker;
using Xunit;

namespace Relay.Core.Tests.Resilience
{
    public class CircuitBreakerPipelineBehaviorTests
    {
        private readonly Mock<ILogger<CircuitBreakerPipelineBehavior<TestRequest, TestResponse>>> _mockLogger;
        private readonly Mock<IOptions<CircuitBreakerOptions>> _mockOptions;

        public CircuitBreakerPipelineBehaviorTests()
        {
            _mockLogger = new Mock<ILogger<CircuitBreakerPipelineBehavior<TestRequest, TestResponse>>>();
            _mockOptions = new Mock<IOptions<CircuitBreakerOptions>>();
            
            var options = new CircuitBreakerOptions
            {
                FailureThreshold = 0.5,
                MinimumThroughput = 3,
                OpenCircuitDuration = TimeSpan.FromMilliseconds(100)
            };
            _mockOptions.Setup(x => x.Value).Returns(options);
        }

        [Fact]
        public async Task HandleAsync_ShouldExecuteSuccessfully_WhenCircuitIsClosed()
        {
            // Arrange
            var behavior = new CircuitBreakerPipelineBehavior<TestRequest, TestResponse>(_mockLogger.Object, _mockOptions.Object);
            var request = new TestRequest { Value = "test" };
            var expectedResponse = new TestResponse { Result = "result" };
            var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);
 
            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task HandleAsync_ShouldOpenCircuit_WhenFailureThresholdExceeded()
        {
            // Arrange
            var behavior = new CircuitBreakerPipelineBehavior<TestRequest, TestResponse>(_mockLogger.Object, _mockOptions.Object);
            var request = new TestRequest { Value = "test" };

            // Act - Generate enough failures to open the circuit
            for (int i = 0; i < 3; i++)
            {
                var next = new RequestHandlerDelegate<TestResponse>(() => throw new InvalidOperationException("Test failure"));
                try
                {
                    await behavior.HandleAsync(request, next, CancellationToken.None);
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }
            }

            // Assert - Next request should be rejected immediately
            var finalNext = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(new TestResponse()));
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
                await behavior.HandleAsync(request, finalNext, CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_ShouldTransitionToHalfOpen_AfterTimeout()
        {
            // Arrange
            var behavior = new CircuitBreakerPipelineBehavior<TestRequest, TestResponse>(_mockLogger.Object, _mockOptions.Object);
            var request = new TestRequest { Value = "test" };

            // Open the circuit
            for (int i = 0; i < 3; i++)
            {
                var failNext = new RequestHandlerDelegate<TestResponse>(() => throw new InvalidOperationException("Test failure"));
                try
                {
                    await behavior.HandleAsync(request, failNext, CancellationToken.None);
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }
            }

            // Wait for timeout
            await Task.Delay(150);
 
            // Act - Should transition to half-open and allow request
            var expectedResponse = new TestResponse { Result = "result" };
            var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);
 
            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task HandleAsync_ShouldCloseCircuit_AfterSuccessfulHalfOpenTrial()
        {
            // Arrange
            var behavior = new CircuitBreakerPipelineBehavior<TestRequest, TestResponse>(_mockLogger.Object, _mockOptions.Object);
            var request = new TestRequest { Value = "test" };
            var expectedResponse = new TestResponse { Result = "result" };

            // Open the circuit
            for (int i = 0; i < 3; i++)
            {
                var failNext = new RequestHandlerDelegate<TestResponse>(() => throw new InvalidOperationException("Test failure"));
                try
                {
                    await behavior.HandleAsync(request, failNext, CancellationToken.None);
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }
            }

            // Wait for timeout
            await Task.Delay(150);

            // Act - Execute enough successful requests to close the circuit
            for (int i = 0; i < 3; i++)
            {
                var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));
                var result = await behavior.HandleAsync(request, next, CancellationToken.None);
                Assert.Equal(expectedResponse, result);
            }

            // Assert - Circuit should be closed, request should succeed
            var finalNext = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));
            var finalResult = await behavior.HandleAsync(request, finalNext, CancellationToken.None);
            Assert.Equal(expectedResponse, finalResult);
        }

        [Fact]
        public void CircuitBreakerState_ShouldInitializeWithClosedState()
        {
            // Arrange
            var options = new CircuitBreakerOptions();

            // Act
            var state = new CircuitBreakerState(options);
 
            // Assert
            Assert.Equal(CircuitState.Closed, state.State);
        }

        [Fact]
        public void CircuitBreakerState_ShouldRecordSuccess()
        {
            // Arrange
            var options = new CircuitBreakerOptions { MinimumThroughput = 5, FailureThreshold = 0.5 };
            var state = new CircuitBreakerState(options);

            // Act
            state.RecordSuccess();
 
            // Assert
            Assert.False(state.ShouldOpenCircuit());
        }

        [Fact]
        public void CircuitBreakerState_ShouldRecordFailure()
        {
            // Arrange
            var options = new CircuitBreakerOptions { MinimumThroughput = 2, FailureThreshold = 0.5 };
            var state = new CircuitBreakerState(options);

            // Act
            state.RecordFailure();
            state.RecordFailure();

            // Assert
            Assert.True(state.ShouldOpenCircuit());
        }

        [Fact]
        public void CircuitBreakerState_ShouldNotOpenCircuit_BelowMinimumThroughput()
        {
            // Arrange
            var options = new CircuitBreakerOptions { MinimumThroughput = 10, FailureThreshold = 0.5 };
            var state = new CircuitBreakerState(options);

            // Act
            state.RecordFailure();
            state.RecordFailure();
 
            // Assert
            Assert.False(state.ShouldOpenCircuit());
        }

        [Fact]
        public void CircuitBreakerState_ShouldTransitionToOpen()
        {
            // Arrange
            var options = new CircuitBreakerOptions();
            var state = new CircuitBreakerState(options);

            // Act
            state.TransitionToOpen();
 
            // Assert
            Assert.Equal(CircuitState.Open, state.State);
        }

        [Fact]
        public void CircuitBreakerState_ShouldTransitionToHalfOpen()
        {
            // Arrange
            var options = new CircuitBreakerOptions();
            var state = new CircuitBreakerState(options);

            // Act
            state.TransitionToHalfOpen();
 
            // Assert
            Assert.Equal(CircuitState.HalfOpen, state.State);
        }

        [Fact]
        public void CircuitBreakerState_ShouldTransitionToClosed()
        {
            // Arrange
            var options = new CircuitBreakerOptions();
            var state = new CircuitBreakerState(options);
            state.TransitionToOpen();

            // Act
            state.TransitionToClosed();
 
            // Assert
            Assert.Equal(CircuitState.Closed, state.State);
        }

        [Fact]
        public void CircuitBreakerOpenException_ShouldContainRequestType()
        {
            // Arrange
            var requestType = "TestRequest";

            // Act
            var exception = new CircuitBreakerOpenException(requestType);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Contains(requestType, exception.Message);
        }

        public class TestRequest : IRequest<TestResponse>
        {
            public string Value { get; set; } = string.Empty;
        }

        public class TestResponse
        {
            public string Result { get; set; } = string.Empty;
        }
    }
}
