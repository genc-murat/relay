using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Pipeline.Behaviors;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for ConditionalServiceBehavior pipeline behavior.
    /// </summary>
    public class ConditionalServiceBehaviorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ServiceFactory_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ConditionalServiceBehavior<TestRequest, string>(null!));
        }

        [Fact]
        public async Task HandleAsync_Should_Call_Auditor_When_Available()
        {
            // Arrange
            var auditorMock = new Mock<IRequestAuditor>();
            var enricherMock = new Mock<IResponseEnricher<string>>();

            // Create a service factory that returns the auditor when requested
            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IRequestAuditor))
                    return auditorMock.Object;
                if (type == typeof(IResponseEnricher<string>))
                    return null; // Not available for this test
                return null;
            };

            var behavior = new ConditionalServiceBehavior<TestRequest, string>(serviceFactory);
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
            auditorMock.Verify(x => x.AuditRequestAsync(typeof(TestRequest).Name, request, cancellationToken), Times.Once);
            enricherMock.Verify(x => x.EnrichAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.True(nextCalled);
            Assert.Equal("response", result);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Call_Auditor_When_Not_Available()
        {
            // Arrange
            var auditorMock = new Mock<IRequestAuditor>();
            var enricherMock = new Mock<IResponseEnricher<string>>();

            // Create a service factory that returns null for auditor
            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IRequestAuditor))
                    return null; // Not available
                if (type == typeof(IResponseEnricher<string>))
                    return null; // Not available
                return null;
            };

            var behavior = new ConditionalServiceBehavior<TestRequest, string>(serviceFactory);
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
            auditorMock.Verify(x => x.AuditRequestAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
            enricherMock.Verify(x => x.EnrichAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.True(nextCalled);
            Assert.Equal("response", result);
        }

        [Fact]
        public async Task HandleAsync_Should_Enrich_Response_When_Enricher_Is_Available()
        {
            // Arrange
            var auditorMock = new Mock<IRequestAuditor>();
            var enricherMock = new Mock<IResponseEnricher<string>>();
            enricherMock
                .Setup(x => x.EnrichAsync("original", It.IsAny<CancellationToken>()))
                .ReturnsAsync("enriched");

            // Create a service factory that returns the enricher when requested
            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IRequestAuditor))
                    return null; // Not available for this test
                if (type == typeof(IResponseEnricher<string>))
                    return enricherMock.Object;
                return null;
            };

            var behavior = new ConditionalServiceBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("original");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            auditorMock.Verify(x => x.AuditRequestAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
            enricherMock.Verify(x => x.EnrichAsync("original", cancellationToken), Times.Once);
            Assert.Equal("enriched", result);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Enrich_Response_When_Enricher_Is_Not_Available()
        {
            // Arrange
            var auditorMock = new Mock<IRequestAuditor>();
            var enricherMock = new Mock<IResponseEnricher<string>>();

            // Create a service factory that returns null for enricher
            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IRequestAuditor))
                    return null; // Not available
                if (type == typeof(IResponseEnricher<string>))
                    return null; // Not available
                return null;
            };

            var behavior = new ConditionalServiceBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("original");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            auditorMock.Verify(x => x.AuditRequestAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
            enricherMock.Verify(x => x.EnrichAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Equal("original", result);
        }

        [Fact]
        public async Task HandleAsync_Should_Always_Call_Next_Delegate()
        {
            // Arrange
            var auditorMock = new Mock<IRequestAuditor>();
            var enricherMock = new Mock<IResponseEnricher<string>>();

            // Create a service factory that returns null for all services
            ServiceFactory serviceFactory = type => null;

            var behavior = new ConditionalServiceBehavior<TestRequest, string>(serviceFactory);
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
            auditorMock.Verify(x => x.AuditRequestAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
            enricherMock.Verify(x => x.EnrichAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_All_Async_Operations()
        {
            // Arrange
            var auditorMock = new Mock<IRequestAuditor>();
            var enricherMock = new Mock<IResponseEnricher<string>>();
            enricherMock
                .Setup(x => x.EnrichAsync("original", It.IsAny<CancellationToken>()))
                .ReturnsAsync("enriched");

            // Create a service factory that returns both services
            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(IRequestAuditor))
                    return auditorMock.Object;
                if (type == typeof(IResponseEnricher<string>))
                    return enricherMock.Object;
                return null;
            };

            var behavior = new ConditionalServiceBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var cancellationToken = new CancellationToken(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("original");

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            auditorMock.Verify(x => x.AuditRequestAsync(It.IsAny<string>(), It.IsAny<object>(), cancellationToken), Times.Once);
            enricherMock.Verify(x => x.EnrichAsync(It.IsAny<string>(), cancellationToken), Times.Once);
        }

        // Test request class
        public class TestRequest : IRequest<string> { }
    }
}
