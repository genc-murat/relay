using System;
using Xunit;
using Relay.Core;

namespace Relay.Core.Tests.Core
{
    public class AttributesTests
    {
        [Fact]
        public void HandleAttribute_DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var attribute = new HandleAttribute();

            // Assert
            Assert.Null(attribute.Name);
            Assert.Equal(0, attribute.Priority);
        }

        [Fact]
        public void HandleAttribute_Name_CanBeSet()
        {
            // Arrange
            var attribute = new HandleAttribute();
            var expectedName = "TestHandler";

            // Act
            attribute.Name = expectedName;

            // Assert
            Assert.Equal(expectedName, attribute.Name);
        }

        [Fact]
        public void HandleAttribute_Priority_CanBeSet()
        {
            // Arrange
            var attribute = new HandleAttribute();
            var expectedPriority = 10;

            // Act
            attribute.Priority = expectedPriority;

            // Assert
            Assert.Equal(expectedPriority, attribute.Priority);
        }

        [Fact]
        public void NotificationAttribute_DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var attribute = new NotificationAttribute();

            // Assert
            Assert.Equal(NotificationDispatchMode.Parallel, attribute.DispatchMode);
            Assert.Equal(0, attribute.Priority);
        }

        [Fact]
        public void NotificationAttribute_DispatchMode_CanBeSet()
        {
            // Arrange
            var attribute = new NotificationAttribute();

            // Act
            attribute.DispatchMode = NotificationDispatchMode.Sequential;

            // Assert
            Assert.Equal(NotificationDispatchMode.Sequential, attribute.DispatchMode);
        }

        [Fact]
        public void NotificationAttribute_Priority_CanBeSet()
        {
            // Arrange
            var attribute = new NotificationAttribute();
            var expectedPriority = 5;

            // Act
            attribute.Priority = expectedPriority;

            // Assert
            Assert.Equal(expectedPriority, attribute.Priority);
        }

        [Fact]
        public void PipelineAttribute_DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var attribute = new PipelineAttribute();

            // Assert
            Assert.Equal(0, attribute.Order);
            Assert.Equal(PipelineScope.All, attribute.Scope);
        }

        [Fact]
        public void PipelineAttribute_Order_CanBeSet()
        {
            // Arrange
            var attribute = new PipelineAttribute();
            var expectedOrder = 10;

            // Act
            attribute.Order = expectedOrder;

            // Assert
            Assert.Equal(expectedOrder, attribute.Order);
        }

        [Fact]
        public void PipelineAttribute_Scope_CanBeSet()
        {
            // Arrange
            var attribute = new PipelineAttribute();

            // Act
            attribute.Scope = PipelineScope.Requests;

            // Assert
            Assert.Equal(PipelineScope.Requests, attribute.Scope);
        }

        [Fact]
        public void ExposeAsEndpointAttribute_DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var attribute = new ExposeAsEndpointAttribute();

            // Assert
            Assert.Null(attribute.Route);
            Assert.Equal("POST", attribute.HttpMethod);
            Assert.Null(attribute.Version);
        }

        [Fact]
        public void ExposeAsEndpointAttribute_Route_CanBeSet()
        {
            // Arrange
            var attribute = new ExposeAsEndpointAttribute();
            var expectedRoute = "/api/test";

            // Act
            attribute.Route = expectedRoute;

            // Assert
            Assert.Equal(expectedRoute, attribute.Route);
        }

        [Fact]
        public void ExposeAsEndpointAttribute_HttpMethod_CanBeSet()
        {
            // Arrange
            var attribute = new ExposeAsEndpointAttribute();
            var expectedMethod = "GET";

            // Act
            attribute.HttpMethod = expectedMethod;

            // Assert
            Assert.Equal(expectedMethod, attribute.HttpMethod);
        }

        [Fact]
        public void ExposeAsEndpointAttribute_Version_CanBeSet()
        {
            // Arrange
            var attribute = new ExposeAsEndpointAttribute();
            var expectedVersion = "v1";

            // Act
            attribute.Version = expectedVersion;

            // Assert
            Assert.Equal(expectedVersion, attribute.Version);
        }

        [Fact]
        public void NotificationDispatchMode_ShouldHaveParallelValue()
        {
            // Assert
            Assert.Equal(0, (int)NotificationDispatchMode.Parallel);
        }

        [Fact]
        public void NotificationDispatchMode_ShouldHaveSequentialValue()
        {
            // Assert
            Assert.Equal(1, (int)NotificationDispatchMode.Sequential);
        }

        [Fact]
        public void PipelineScope_ShouldHaveAllValue()
        {
            // Assert
            Assert.Equal(0, (int)PipelineScope.All);
        }

        [Fact]
        public void PipelineScope_ShouldHaveRequestsValue()
        {
            // Assert
            Assert.Equal(1, (int)PipelineScope.Requests);
        }

        [Fact]
        public void PipelineScope_ShouldHaveStreamsValue()
        {
            // Assert
            Assert.Equal(2, (int)PipelineScope.Streams);
        }

        [Fact]
        public void PipelineScope_ShouldHaveNotificationsValue()
        {
            // Assert
            Assert.Equal(3, (int)PipelineScope.Notifications);
        }

        [Fact]
        public void HandleAttribute_CanBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.TestHandler));

            // Act
            var attributes = method?.GetCustomAttributes(typeof(HandleAttribute), false);

            // Assert
            Assert.NotNull(attributes);
            Assert.Single(attributes);
        }

        [Fact]
        public void NotificationAttribute_CanBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.TestNotificationHandler));

            // Act
            var attributes = method?.GetCustomAttributes(typeof(NotificationAttribute), false);

            // Assert
            Assert.NotNull(attributes);
            Assert.Single(attributes);
        }

        [Fact]
        public void PipelineAttribute_CanBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.TestPipeline));

            // Act
            var attributes = method?.GetCustomAttributes(typeof(PipelineAttribute), false);

            // Assert
            Assert.NotNull(attributes);
            Assert.Single(attributes);
        }

        [Fact]
        public void ExposeAsEndpointAttribute_CanBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.TestEndpoint));

            // Act
            var attributes = method?.GetCustomAttributes(typeof(ExposeAsEndpointAttribute), false);

            // Assert
            Assert.NotNull(attributes);
            Assert.Single(attributes);
        }

        // Test class with attributed methods
        private class TestClass
        {
            [Handle(Name = "TestHandler", Priority = 5)]
            public void TestHandler() { }

            [Notification(DispatchMode = NotificationDispatchMode.Sequential, Priority = 3)]
            public void TestNotificationHandler() { }

            [Pipeline(Order = 1, Scope = PipelineScope.Requests)]
            public void TestPipeline() { }

            [ExposeAsEndpoint(Route = "/test", HttpMethod = "GET", Version = "v1")]
            public void TestEndpoint() { }
        }
    }
}
