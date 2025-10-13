using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline.Behaviors;
using Relay.Core.Pipeline.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for RequestPostProcessorBehavior pipeline behavior.
    /// </summary>
    public class RequestPostProcessorBehaviorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ServiceProvider_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RequestPostProcessorBehavior<TestRequest, string>(null!));
        }

        [Fact]
        public async Task HandleAsync_Should_Return_Response_When_No_PostProcessors_Are_Registered()
        {
            // Arrange
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
                .Returns(Array.Empty<IRequestPostProcessor<TestRequest, string>>());

            var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

            var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.Equal("response", result);
            loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_PostProcessors()
        {
            // Arrange
            var postProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
            postProcessorMock
                .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask());

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
                .Returns(new IRequestPostProcessor<TestRequest, string>[] { postProcessorMock.Object });

            var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

            var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = new CancellationToken(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            postProcessorMock.Verify(x => x.ProcessAsync(request, "response", cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Log_PostProcessor_Execution()
        {
            // Arrange
            var postProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
            postProcessorMock
                .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask());

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
                .Returns(new IRequestPostProcessor<TestRequest, string>[] { postProcessorMock.Object });

            var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

            var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            loggerMock.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
            loggerMock.Verify(x => x.Log(LogLevel.Trace, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
        }

        // Test request class
        public class TestRequest { }
    }
}