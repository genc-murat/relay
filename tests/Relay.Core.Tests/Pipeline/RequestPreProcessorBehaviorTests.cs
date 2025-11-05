using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Pipeline.Behaviors;
using Relay.Core.Pipeline.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for RequestPreProcessorBehavior pipeline behavior.
    /// </summary>
    public class RequestPreProcessorBehaviorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ServiceProvider_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RequestPreProcessorBehavior<TestRequest, string>(null!));
        }

        [Fact]
        public async Task HandleAsync_Should_Execute_PreProcessors_When_Available()
        {
            // Arrange
            var preProcessor1Mock = new Mock<IRequestPreProcessor<TestRequest>>();
            var preProcessor2Mock = new Mock<IRequestPreProcessor<TestRequest>>();
            var preProcessors = new[] { preProcessor1Mock.Object, preProcessor2Mock.Object };

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPreProcessor<TestRequest>>)))
                .Returns(preProcessors);

            var loggerMock = new Mock<ILogger<RequestPreProcessorBehavior<TestRequest, string>>>();

            var behavior = new RequestPreProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("response", result);

            preProcessor1Mock.Verify(v => v.ProcessAsync(request, cancellationToken), Times.Once);
            preProcessor2Mock.Verify(v => v.ProcessAsync(request, cancellationToken), Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Executing 2 pre-processor(s)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("All pre-processors completed successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Empty_PreProcessors_Collection()
        {
            // Arrange
            var preProcessors = Array.Empty<IRequestPreProcessor<TestRequest>>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPreProcessor<TestRequest>>)))
                .Returns(preProcessors);

            var loggerMock = new Mock<ILogger<RequestPreProcessorBehavior<TestRequest, string>>>();

            var behavior = new RequestPreProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("response", result);

            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Propagate_Exception_From_PreProcessor()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Pre-processing failed");
            var preProcessorMock = new Mock<IRequestPreProcessor<TestRequest>>();
            preProcessorMock
                .Setup(v => v.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var preProcessors = new[] { preProcessorMock.Object };

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPreProcessor<TestRequest>>)))
                .Returns(preProcessors);

            var loggerMock = new Mock<ILogger<RequestPreProcessorBehavior<TestRequest, string>>>();

            var behavior = new RequestPreProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            Assert.Equal(expectedException, exception);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Pre-processor")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_PreProcessors()
        {
            // Arrange
            var preProcessorMock = new Mock<IRequestPreProcessor<TestRequest>>();
            var preProcessors = new[] { preProcessorMock.Object };

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPreProcessor<TestRequest>>)))
                .Returns(preProcessors);

            var loggerMock = new Mock<ILogger<RequestPreProcessorBehavior<TestRequest, string>>>();

            var behavior = new RequestPreProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = new CancellationToken(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            preProcessorMock.Verify(v => v.ProcessAsync(request, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Always_Call_Next_Delegate_After_PreProcessing()
        {
            // Arrange
            var preProcessorMock = new Mock<IRequestPreProcessor<TestRequest>>();
            var preProcessors = new[] { preProcessorMock.Object };

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPreProcessor<TestRequest>>)))
                .Returns(preProcessors);

            var loggerMock = new Mock<ILogger<RequestPreProcessorBehavior<TestRequest, string>>>();

            var behavior = new RequestPreProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Work_Without_Logger()
        {
            // Arrange
            var preProcessorMock = new Mock<IRequestPreProcessor<TestRequest>>();
            var preProcessors = new[] { preProcessorMock.Object };

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPreProcessor<TestRequest>>)))
                .Returns(preProcessors);

            var behavior = new RequestPreProcessorBehavior<TestRequest, string>(serviceProviderMock.Object);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("response", result);

            preProcessorMock.Verify(v => v.ProcessAsync(request, cancellationToken), Times.Once);
        }

        // Test request class
        public class TestRequest : IRequest<string> { }
    }
}
