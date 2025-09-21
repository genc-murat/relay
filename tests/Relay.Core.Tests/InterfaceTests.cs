using FluentAssertions;
using Relay.Core;
using Xunit;

namespace Relay.Core.Tests
{
    public class InterfaceTests
    {
        [Fact]
        public void IRelay_Interface_Should_Be_Defined()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);

            // Assert
            relayType.Should().NotBeNull();
            relayType.IsInterface.Should().BeTrue();
        }

        [Fact]
        public void IRequest_Interfaces_Should_Be_Defined()
        {
            // Arrange & Act
            var requestType = typeof(IRequest<>);
            var requestVoidType = typeof(IRequest);
            var streamRequestType = typeof(IStreamRequest<>);

            // Assert
            requestType.Should().NotBeNull();
            requestType.IsInterface.Should().BeTrue();
            
            requestVoidType.Should().NotBeNull();
            requestVoidType.IsInterface.Should().BeTrue();
            
            streamRequestType.Should().NotBeNull();
            streamRequestType.IsInterface.Should().BeTrue();
        }

        [Fact]
        public void INotification_Interface_Should_Be_Defined()
        {
            // Arrange & Act
            var notificationType = typeof(INotification);

            // Assert
            notificationType.Should().NotBeNull();
            notificationType.IsInterface.Should().BeTrue();
        }

        [Fact]
        public void Handler_Interfaces_Should_Be_Defined()
        {
            // Arrange & Act
            var requestHandlerType = typeof(IRequestHandler<,>);
            var requestVoidHandlerType = typeof(IRequestHandler<>);
            var streamHandlerType = typeof(IStreamHandler<,>);
            var notificationHandlerType = typeof(INotificationHandler<>);

            // Assert
            requestHandlerType.Should().NotBeNull();
            requestHandlerType.IsInterface.Should().BeTrue();
            
            requestVoidHandlerType.Should().NotBeNull();
            requestVoidHandlerType.IsInterface.Should().BeTrue();
            
            streamHandlerType.Should().NotBeNull();
            streamHandlerType.IsInterface.Should().BeTrue();
            
            notificationHandlerType.Should().NotBeNull();
            notificationHandlerType.IsInterface.Should().BeTrue();
        }
    }
}