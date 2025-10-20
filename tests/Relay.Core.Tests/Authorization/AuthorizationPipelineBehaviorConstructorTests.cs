using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Authorization;
using Relay.Core.Configuration.Options;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationPipelineBehaviorConstructorTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IAuthorizationContext> _authorizationContextMock;
        private readonly Mock<ILogger<AuthorizationPipelineBehavior<TestRequest, string>>> _loggerMock;
        private readonly IOptions<RelayOptions> _options;

        public AuthorizationPipelineBehaviorConstructorTests()
        {
            _authorizationServiceMock = new Mock<IAuthorizationService>();
            _authorizationContextMock = new Mock<IAuthorizationContext>();
            _loggerMock = new Mock<ILogger<AuthorizationPipelineBehavior<TestRequest, string>>>();

            var relayOptions = new RelayOptions();
            _options = Options.Create(relayOptions);

            // Setup context properties
            var properties = new Dictionary<string, object>();
            _authorizationContextMock.Setup(x => x.Properties).Returns(properties);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenAuthorizationServiceIsNull()
        {
            // Act
            Action act = () => new AuthorizationPipelineBehavior<TestRequest, string>(
                null!,
                _authorizationContextMock.Object,
                _loggerMock.Object,
                _options);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(() => act());
            Assert.Equal("authorizationService", ex.ParamName);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenAuthorizationContextIsNull()
        {
            // Act
            Action act = () => new AuthorizationPipelineBehavior<TestRequest, string>(
                _authorizationServiceMock.Object,
                null!,
                _loggerMock.Object,
                _options);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(() => act());
            Assert.Equal("authorizationContext", ex.ParamName);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenLoggerIsNull()
        {
            // Act
            Action act = () => new AuthorizationPipelineBehavior<TestRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                null!,
                _options);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(() => act());
            Assert.Equal("logger", ex.ParamName);
        }

        [Fact]
        public void Constructor_Should_ThrowException_WhenOptionsIsNull()
        {
            // Act
            Action act = () => new AuthorizationPipelineBehavior<TestRequest, string>(
                _authorizationServiceMock.Object,
                _authorizationContextMock.Object,
                _loggerMock.Object,
                null!);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(() => act());
            Assert.Equal("options", ex.ParamName);
        }

        #region Test Helper Classes

        public class TestRequest : IRequest<string> { }

        #endregion
    }
}