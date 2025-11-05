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
            Assert.NotNull(relayType);
            Assert.True(relayType.IsInterface);
        }

        [Fact]
        public void IRequest_Interfaces_Should_Be_Defined()
        {
            // Arrange & Act
            var requestType = typeof(IRequest<>);
            var requestVoidType = typeof(IRequest);
            var streamRequestType = typeof(IStreamRequest<>);

            // Assert
            Assert.NotNull(requestType);
            Assert.True(requestType.IsInterface);

            Assert.NotNull(requestVoidType);
            Assert.True(requestVoidType.IsInterface);

            Assert.NotNull(streamRequestType);
            Assert.True(streamRequestType.IsInterface);
        }

        [Fact]
        public void INotification_Interface_Should_Be_Defined()
        {
            // Arrange & Act
            var notificationType = typeof(INotification);

            // Assert
            Assert.NotNull(notificationType);
            Assert.True(notificationType.IsInterface);
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
            Assert.NotNull(requestHandlerType);
            Assert.True(requestHandlerType.IsInterface);

            Assert.NotNull(requestVoidHandlerType);
            Assert.True(requestVoidHandlerType.IsInterface);

            Assert.NotNull(streamHandlerType);
            Assert.True(streamHandlerType.IsInterface);

            Assert.NotNull(notificationHandlerType);
            Assert.True(notificationHandlerType.IsInterface);
        }

        [Fact]
        public void IRelay_Should_Have_Send_Methods()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var sendMethods = relayType.GetMethods().Where(m => m.Name == "SendAsync");

            // Assert
            Assert.NotEmpty(sendMethods);
            Assert.True(sendMethods.All(m => m.ReturnType.Name.Contains("ValueTask")));
        }

        [Fact]
        public void IRelay_Should_Have_Publish_Methods()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var publishMethods = relayType.GetMethods().Where(m => m.Name == "PublishAsync");

            // Assert
            Assert.NotEmpty(publishMethods);
            Assert.True(publishMethods.All(m => m.ReturnType.Name.Contains("ValueTask")));
        }

        [Fact]
        public void IRequest_Should_Be_Generic()
        {
            // Arrange & Act
            var requestType = typeof(IRequest<>);

            // Assert
            Assert.True(requestType.IsGenericType);
            Assert.Single(requestType.GetGenericArguments());
        }

        [Fact]
        public void IRequest_Void_Should_Not_Be_Generic()
        {
            // Arrange & Act
            var requestVoidType = typeof(IRequest);

            // Assert
            Assert.False(requestVoidType.IsGenericType);
        }

        [Fact]
        public void IStreamRequest_Should_Be_Generic()
        {
            // Arrange & Act
            var streamRequestType = typeof(IStreamRequest<>);

            // Assert
            Assert.True(streamRequestType.IsGenericType);
            Assert.Single(streamRequestType.GetGenericArguments());
        }

        [Fact]
        public void IRequestHandler_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var handlerType = typeof(IRequestHandler<,>);
            var handleMethod = handlerType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(handleMethod);
            Assert.Contains("ValueTask", handleMethod.ReturnType.Name);
        }

        [Fact]
        public void IRequestHandler_Void_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var handlerType = typeof(IRequestHandler<>);
            var handleMethod = handlerType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(handleMethod);
            Assert.Contains("ValueTask", handleMethod.ReturnType.Name);
        }

        [Fact]
        public void IStreamHandler_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var streamHandlerType = typeof(IStreamHandler<,>);
            var handleMethod = streamHandlerType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(handleMethod);
            Assert.Contains("IAsyncEnumerable", handleMethod.ReturnType.Name);
        }

        [Fact]
        public void INotificationHandler_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var notificationHandlerType = typeof(INotificationHandler<>);
            var handleMethod = notificationHandlerType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(handleMethod);
            Assert.Contains("ValueTask", handleMethod.ReturnType.Name);
        }

        [Fact]
        public void IRequest_Should_Be_Public()
        {
            // Arrange & Act
            var requestType = typeof(IRequest<>);

            // Assert
            Assert.True(requestType.IsPublic);
        }

        [Fact]
        public void INotification_Should_Be_Public()
        {
            // Arrange & Act
            var notificationType = typeof(INotification);

            // Assert
            Assert.True(notificationType.IsPublic);
        }

        [Fact]
        public void IRelay_Should_Be_Public()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);

            // Assert
            Assert.True(relayType.IsPublic);
        }

        [Fact]
        public void Handler_Interfaces_Should_Be_Public()
        {
            // Arrange & Act
            var requestHandlerType = typeof(IRequestHandler<,>);
            var streamHandlerType = typeof(IStreamHandler<,>);
            var notificationHandlerType = typeof(INotificationHandler<>);

            // Assert
            Assert.True(requestHandlerType.IsPublic);
            Assert.True(streamHandlerType.IsPublic);
            Assert.True(notificationHandlerType.IsPublic);
        }

        [Fact]
        public void IRequestHandler_Should_Have_Two_Generic_Parameters()
        {
            // Arrange & Act
            var requestHandlerType = typeof(IRequestHandler<,>);

            // Assert
            Assert.Equal(2, requestHandlerType.GetGenericArguments().Length);
        }

        [Fact]
        public void IRequestHandler_Void_Should_Have_One_Generic_Parameter()
        {
            // Arrange & Act
            var requestVoidHandlerType = typeof(IRequestHandler<>);

            // Assert
            Assert.Single(requestVoidHandlerType.GetGenericArguments());
        }

        [Fact]
        public void IStreamHandler_Should_Have_Two_Generic_Parameters()
        {
            // Arrange & Act
            var streamHandlerType = typeof(IStreamHandler<,>);

            // Assert
            Assert.Equal(2, streamHandlerType.GetGenericArguments().Length);
        }

        [Fact]
        public void INotificationHandler_Should_Have_One_Generic_Parameter()
        {
            // Arrange & Act
            var notificationHandlerType = typeof(INotificationHandler<>);

            // Assert
            Assert.Single(notificationHandlerType.GetGenericArguments());
        }

        [Fact]
        public void All_Core_Interfaces_Should_Be_In_Relay_Namespace()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var requestType = typeof(IRequest<>);
            var notificationType = typeof(INotification);

            // Assert
            Assert.Equal("Relay.Core.Contracts.Core", relayType.Namespace);
            Assert.Equal("Relay.Core.Contracts.Requests", requestType.Namespace);
            Assert.Equal("Relay.Core.Contracts.Requests", notificationType.Namespace);
        }

        [Fact]
        public void IPipelineBehavior_Interface_Should_Be_Defined()
        {
            // Arrange & Act
            var pipelineBehaviorType = typeof(IPipelineBehavior<,>);

            // Assert
            Assert.NotNull(pipelineBehaviorType);
            Assert.True(pipelineBehaviorType.IsInterface);
            Assert.True(pipelineBehaviorType.IsGenericType);
        }

        [Fact]
        public void IPipelineBehavior_Should_Have_HandleAsync_Method()
        {
            // Arrange & Act
            var pipelineBehaviorType = typeof(IPipelineBehavior<,>);
            var handleMethod = pipelineBehaviorType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(handleMethod);
            Assert.Contains("ValueTask", handleMethod.ReturnType.Name);
        }

        [Fact]
        public void RequestHandlerDelegate_Should_Be_Defined()
        {
            // Arrange & Act
            var delegateType = typeof(RequestHandlerDelegate<>);

            // Assert
            Assert.NotNull(delegateType);
            Assert.True(delegateType.IsGenericType);
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
            Assert.Empty(requestType.GetMethods());
            Assert.Empty(requestVoidType.GetMethods());
            Assert.Empty(streamRequestType.GetMethods());
            Assert.Empty(notificationType.GetMethods());
        }

        [Fact]
        public void IRelay_SendAsync_Should_Accept_CancellationToken()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var sendMethods = relayType.GetMethods().Where(m => m.Name == "SendAsync");

            // Assert
            Assert.NotEmpty(sendMethods);
            Assert.True(sendMethods.All(m => m.GetParameters().Any(p => p.ParameterType.Name.Contains("CancellationToken"))));
        }

        [Fact]
        public void IRelay_PublishAsync_Should_Accept_CancellationToken()
        {
            // Arrange & Act
            var relayType = typeof(IRelay);
            var publishMethods = relayType.GetMethods().Where(m => m.Name == "PublishAsync");

            // Assert
            Assert.NotEmpty(publishMethods);
            Assert.True(publishMethods.All(m => m.GetParameters().Any(p => p.ParameterType.Name.Contains("CancellationToken"))));
        }

        [Fact]
        public void Handler_HandleAsync_Should_Accept_CancellationToken()
        {
            // Arrange & Act
            var requestHandlerType = typeof(IRequestHandler<,>);
            var handleMethod = requestHandlerType.GetMethod("HandleAsync");
            var parameters = handleMethod!.GetParameters();

            // Assert
            Assert.Contains(parameters, p => p.ParameterType.Name.Contains("CancellationToken"));
        }

        [Fact]
        public void IStreamHandler_Should_Return_IAsyncEnumerable()
        {
            // Arrange & Act
            var streamHandlerType = typeof(IStreamHandler<,>);
            var handleMethod = streamHandlerType.GetMethod("HandleAsync");

            // Assert
            Assert.Contains("IAsyncEnumerable", handleMethod!.ReturnType.Name);
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
            Assert.StartsWith("I", relayType.Name);
            Assert.StartsWith("I", requestType.Name);
            Assert.StartsWith("I", notificationType.Name);
            Assert.StartsWith("I", handlerType.Name);
        }
    }
}
