using FluentAssertions;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System.Linq;
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

        [Fact]
        public void IRelay_Should_Have_Send_Methods()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var sendMethods = relayType.GetMethods().Where(m => m.Name == "SendAsync");

            // Assert
            sendMethods.Should().NotBeEmpty();
            sendMethods.All(m => m.ReturnType.Name.Contains("ValueTask")).Should().BeTrue();
        }

        [Fact]
        public void IRelay_Should_Have_Publish_Methods()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var publishMethods = relayType.GetMethods().Where(m => m.Name == "PublishAsync");

            // Assert
            publishMethods.Should().NotBeEmpty();
            publishMethods.All(m => m.ReturnType.Name.Contains("ValueTask")).Should().BeTrue();
        }

        [Fact]
        public void IRequest_Should_Be_Generic()
        {
            // Arrange & Act
            var requestType = typeof(IRequest<>);

            // Assert
            requestType.IsGenericType.Should().BeTrue();
            requestType.GetGenericArguments().Should().HaveCount(1);
        }

        [Fact]
        public void IRequest_Void_Should_Not_Be_Generic()
        {
            // Arrange & Act
            var requestVoidType = typeof(IRequest);

            // Assert
            requestVoidType.IsGenericType.Should().BeFalse();
        }

        [Fact]
        public void IStreamRequest_Should_Be_Generic()
        {
            // Arrange & Act
            var streamRequestType = typeof(IStreamRequest<>);

            // Assert
            streamRequestType.IsGenericType.Should().BeTrue();
            streamRequestType.GetGenericArguments().Should().HaveCount(1);
        }

        [Fact]
        public void IRequestHandler_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var handlerType = typeof(IRequestHandler<,>);
            var handleMethod = handlerType.GetMethod("HandleAsync");

            // Assert
            handleMethod.Should().NotBeNull();
            handleMethod!.ReturnType.Name.Should().Contain("ValueTask");
        }

        [Fact]
        public void IRequestHandler_Void_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var handlerType = typeof(IRequestHandler<>);
            var handleMethod = handlerType.GetMethod("HandleAsync");

            // Assert
            handleMethod.Should().NotBeNull();
            handleMethod!.ReturnType.Name.Should().Contain("ValueTask");
        }

        [Fact]
        public void IStreamHandler_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var streamHandlerType = typeof(IStreamHandler<,>);
            var handleMethod = streamHandlerType.GetMethod("HandleAsync");

            // Assert
            handleMethod.Should().NotBeNull();
            handleMethod!.ReturnType.Name.Should().Contain("IAsyncEnumerable");
        }

        [Fact]
        public void INotificationHandler_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var notificationHandlerType = typeof(INotificationHandler<>);
            var handleMethod = notificationHandlerType.GetMethod("HandleAsync");

            // Assert
            handleMethod.Should().NotBeNull();
            handleMethod!.ReturnType.Name.Should().Contain("ValueTask");
        }

        [Fact]
        public void IRequest_Should_Be_Public()
        {
            // Arrange & Act
            var requestType = typeof(IRequest<>);

            // Assert
            requestType.IsPublic.Should().BeTrue();
        }

        [Fact]
        public void INotification_Should_Be_Public()
        {
            // Arrange & Act
            var notificationType = typeof(INotification);

            // Assert
            notificationType.IsPublic.Should().BeTrue();
        }

        [Fact]
        public void IRelay_Should_Be_Public()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);

            // Assert
            relayType.IsPublic.Should().BeTrue();
        }

        [Fact]
        public void Handler_Interfaces_Should_Be_Public()
        {
            // Arrange & Act
            var requestHandlerType = typeof(IRequestHandler<,>);
            var streamHandlerType = typeof(IStreamHandler<,>);
            var notificationHandlerType = typeof(INotificationHandler<>);

            // Assert
            requestHandlerType.IsPublic.Should().BeTrue();
            streamHandlerType.IsPublic.Should().BeTrue();
            notificationHandlerType.IsPublic.Should().BeTrue();
        }

        [Fact]
        public void IRequestHandler_Should_Have_Two_Generic_Parameters()
        {
            // Arrange & Act
            var requestHandlerType = typeof(IRequestHandler<,>);

            // Assert
            requestHandlerType.GetGenericArguments().Should().HaveCount(2);
        }

        [Fact]
        public void IRequestHandler_Void_Should_Have_One_Generic_Parameter()
        {
            // Arrange & Act
            var requestVoidHandlerType = typeof(IRequestHandler<>);

            // Assert
            requestVoidHandlerType.GetGenericArguments().Should().HaveCount(1);
        }

        [Fact]
        public void IStreamHandler_Should_Have_Two_Generic_Parameters()
        {
            // Arrange & Act
            var streamHandlerType = typeof(IStreamHandler<,>);

            // Assert
            streamHandlerType.GetGenericArguments().Should().HaveCount(2);
        }

        [Fact]
        public void INotificationHandler_Should_Have_One_Generic_Parameter()
        {
            // Arrange & Act
            var notificationHandlerType = typeof(INotificationHandler<>);

            // Assert
            notificationHandlerType.GetGenericArguments().Should().HaveCount(1);
        }

        [Fact]
        public void All_Core_Interfaces_Should_Be_In_Relay_Namespace()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var requestType = typeof(IRequest<>);
            var notificationType = typeof(INotification);

            // Assert
            relayType.Namespace.Should().Be("Relay.Core.Contracts.Core");
            requestType.Namespace.Should().Be("Relay.Core.Contracts.Requests");
            notificationType.Namespace.Should().Be("Relay.Core.Contracts.Requests");
        }

        [Fact]
        public void IPipelineBehavior_Interface_Should_Be_Defined()
        {
            // Arrange & Act
            var pipelineBehaviorType = typeof(IPipelineBehavior<,>);

            // Assert
            pipelineBehaviorType.Should().NotBeNull();
            pipelineBehaviorType.IsInterface.Should().BeTrue();
            pipelineBehaviorType.IsGenericType.Should().BeTrue();
        }

        [Fact]
        public void IPipelineBehavior_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var pipelineBehaviorType = typeof(IPipelineBehavior<,>);
            var handleMethod = pipelineBehaviorType.GetMethod("HandleAsync");

            // Assert
            handleMethod.Should().NotBeNull();
            handleMethod!.ReturnType.Name.Should().Contain("ValueTask");
        }

        [Fact]
        public void RequestHandlerDelegate_Should_Be_Defined()
        {
            // Arrange & Act
            var delegateType = typeof(RequestHandlerDelegate<>);

            // Assert
            delegateType.Should().NotBeNull();
            delegateType.IsGenericType.Should().BeTrue();
        }

        [Fact]
        public void All_Request_Interfaces_Should_Be_Marker_Interfaces()
        {
            // Arrange & Act
            var requestType = typeof(IRequest<>);
            var requestVoidType = typeof(IRequest);
            var streamRequestType = typeof(IStreamRequest<>);
            var notificationType = typeof(INotification);

            // Assert
            requestType.GetMethods().Should().BeEmpty();
            requestVoidType.GetMethods().Should().BeEmpty();
            streamRequestType.GetMethods().Should().BeEmpty();
            notificationType.GetMethods().Should().BeEmpty();
        }

        [Fact]
        public void IRelay_SendAsync_Should_Accept_CancellationToken()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var sendMethods = relayType.GetMethods().Where(m => m.Name == "SendAsync");

            // Assert
            sendMethods.Should().NotBeEmpty();
            sendMethods.All(m => m.GetParameters().Any(p => p.ParameterType.Name.Contains("CancellationToken"))).Should().BeTrue();
        }

        [Fact]
        public void IRelay_PublishAsync_Should_Accept_CancellationToken()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var publishMethods = relayType.GetMethods().Where(m => m.Name == "PublishAsync");

            // Assert
            publishMethods.Should().NotBeEmpty();
            publishMethods.All(m => m.GetParameters().Any(p => p.ParameterType.Name.Contains("CancellationToken"))).Should().BeTrue();
        }

        [Fact]
        public void Handler_HandleAsync_Should_Accept_CancellationToken()
        {
            // Arrange & Act
            var requestHandlerType = typeof(IRequestHandler<,>);
            var handleMethod = requestHandlerType.GetMethod("HandleAsync");
            var parameters = handleMethod!.GetParameters();

            // Assert
            parameters.Should().Contain(p => p.ParameterType.Name.Contains("CancellationToken"));
        }

        [Fact]
        public void IStreamHandler_Should_Return_IAsyncEnumerable()
        {
            // Arrange & Act
            var streamHandlerType = typeof(IStreamHandler<,>);
            var handleMethod = streamHandlerType.GetMethod("HandleAsync");

            // Assert
            handleMethod!.ReturnType.Name.Should().Contain("IAsyncEnumerable");
        }

        [Fact]
        public void Interface_Names_Should_Follow_Naming_Convention()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var requestType = typeof(IRequest<>);
            var notificationType = typeof(INotification);
            var handlerType = typeof(IRequestHandler<,>);

            // Assert
            relayType.Name.Should().StartWith("I");
            requestType.Name.Should().StartWith("I");
            notificationType.Name.Should().StartWith("I");
            handlerType.Name.Should().StartWith("I");
        }
    }
}