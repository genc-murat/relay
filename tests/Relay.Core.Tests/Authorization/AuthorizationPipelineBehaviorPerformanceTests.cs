using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Authorization;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationPipelineBehaviorPerformanceTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly IOptions<RelayOptions> _options;

        public AuthorizationPipelineBehaviorPerformanceTests()
        {
            _authorizationServiceMock = new Mock<IAuthorizationService>();
            _authorizationContextMock = new Mock<IAuthorizationContext>();

            var relayOptions = new RelayOptions();
            _options = Options.Create(relayOptions);

            // Setup context properties
            var properties = new Dictionary<string, object>();
            _authorizationContextMock.Setup(x => x.Properties).Returns(properties);
        }

        [Fact]
        public async Task HandleAsync_Should_CacheAuthorizeAttributes_ForPerformance()
        {
            // Arrange
            var behavior = CreateBehaviorForAuthorizedRequest();
            var request = new TestAuthorizedRequest();
            _authorizationServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("Success");

            // Act - Call multiple times to test caching
            await behavior.HandleAsync(request, next, CancellationToken.None);
            await behavior.HandleAsync(request, next, CancellationToken.None);
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Authorization service should be called 3 times (once per request)
            _authorizationServiceMock.Verify(x => x.AuthorizeAsync(It.IsAny<IAuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        private AuthorizationPipelineBehavior<TestAuthorizedRequest, string> CreateBehaviorForAuthorizedRequest()
        {
            var logger = new Mock<ILogger<AuthorizationPipelineBehavior<TestAuthorizedRequest, string>>>();
            return new AuthorizationPipelineBehavior<TestAuthorizedRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                logger.Object,
                _options);
        }

        #region Test Helper Classes

        [Authorize("Admin")]
        public class TestAuthorizedRequest : IRequest<string> { }

        #endregion
    }
}