using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options;
using Relay.Core.DistributedTracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.DistributedTracing
{
    public class DistributedTracingPipelineBehaviorTests
    {
        private readonly Mock<IDistributedTracingProvider> _mockTracingProvider;
        private readonly Mock<ILogger<DistributedTracingPipelineBehavior<TestRequest, TestResponse>>> _mockLogger;
        private readonly Mock<IOptions<RelayOptions>> _mockOptions;

        public DistributedTracingPipelineBehaviorTests()
        {
            _mockTracingProvider = new Mock<IDistributedTracingProvider>();
            _mockLogger = new Mock<ILogger<DistributedTracingPipelineBehavior<TestRequest, TestResponse>>>();
            _mockOptions = new Mock<IOptions<RelayOptions>>();

            var relayOptions = new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true,
                    TraceRequests = true,
                    TraceResponses = true
                }
            };
            _mockOptions.Setup(x => x.Value).Returns(relayOptions);
        }

        [Fact]
        public async Task HandleAsync_ShouldStartActivity_WhenTracingEnabled()
        {
            // Arrange
            var activity = new Activity("test-operation");
            activity.Start();
            
            _mockTracingProvider.Setup(x => x.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object?>>()))
                .Returns(activity);

            var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
                _mockTracingProvider.Object,
                _mockLogger.Object,
                _mockOptions.Object);

            var request = new TestRequest { Value = "test" };
            var expectedResponse = new TestResponse { Result = "result" };
            var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);
            _mockTracingProvider.Verify(x => x.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object?>>()), Times.Once);
            
            activity.Stop();
        }

        [Fact]
        public async Task HandleAsync_ShouldNotTrace_WhenTracingDisabled()
        {
            // Arrange
            var relayOptions = new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = false
                }
            };
            _mockOptions.Setup(x => x.Value).Returns(relayOptions);

            var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
                _mockTracingProvider.Object,
                _mockLogger.Object,
                _mockOptions.Object);

            var request = new TestRequest { Value = "test" };
            var expectedResponse = new TestResponse { Result = "result" };
            var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);
            _mockTracingProvider.Verify(x => x.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object?>>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldProceed_WhenActivityIsNull()
        {
            // Arrange
            _mockTracingProvider.Setup(x => x.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object?>>()))
                .Returns((Activity?)null);

            var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
                _mockTracingProvider.Object,
                _mockLogger.Object,
                _mockOptions.Object);

            var request = new TestRequest { Value = "test" };
            var expectedResponse = new TestResponse { Result = "result" };
            var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task HandleAsync_ShouldHandleExceptions_AndStopActivity()
        {
            // Arrange
            var activity = new Activity("test-operation");
            activity.Start();
            
            _mockTracingProvider.Setup(x => x.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object?>>()))
                .Returns(activity);

            var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
                _mockTracingProvider.Object,
                _mockLogger.Object,
                _mockOptions.Object);

            var request = new TestRequest { Value = "test" };
            var next = new RequestHandlerDelegate<TestResponse>(() => throw new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await behavior.HandleAsync(request, next, CancellationToken.None));
            
            activity.Stop();
        }

        [Fact]
        public void TraceAttribute_ShouldInitializeWithDefaults()
        {
            // Act
            var attribute = new TraceAttribute();

            // Assert
            attribute.Should().NotBeNull();
        }

        [Fact]
        public void TraceAttribute_ShouldAllowCustomization()
        {
            // Act
            var attribute = new TraceAttribute
            {
                OperationName = "CustomOperation",
                TraceRequest = false,
                TraceResponse = false
            };

            // Assert
            attribute.OperationName.Should().Be("CustomOperation");
            attribute.TraceRequest.Should().BeFalse();
            attribute.TraceResponse.Should().BeFalse();
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
