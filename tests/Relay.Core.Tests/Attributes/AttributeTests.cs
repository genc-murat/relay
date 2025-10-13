using System;
using System.Reflection;
using Xunit;

namespace Relay.Core.Tests
{
    public class AttributeTests
    {
        public class HandleAttributeTests
        {
            [Fact]
            public void HandleAttribute_ShouldHaveDefaultValues()
            {
                // Arrange & Act
                var attribute = new HandleAttribute();

                // Assert
                Assert.Null(attribute.Name);
                Assert.Equal(0, attribute.Priority);
            }

            [Fact]
            public void HandleAttribute_ShouldAllowSettingName()
            {
                // Arrange
                const string expectedName = "TestHandler";

                // Act
                var attribute = new HandleAttribute { Name = expectedName };

                // Assert
                Assert.Equal(expectedName, attribute.Name);
            }

            [Fact]
            public void HandleAttribute_ShouldAllowSettingPriority()
            {
                // Arrange
                const int expectedPriority = 100;

                // Act
                var attribute = new HandleAttribute { Priority = expectedPriority };

                // Assert
                Assert.Equal(expectedPriority, attribute.Priority);
            }

            [Fact]
            public void HandleAttribute_ShouldBeApplicableToMethods()
            {
                // Arrange
                var attributeType = typeof(HandleAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                Assert.NotNull(usage);
                Assert.Equal(AttributeTargets.Method, usage.ValidOn);
            }

            [Fact]
            public void HandleAttribute_ShouldAllowMultipleOnSameTarget()
            {
                // Arrange
                var attributeType = typeof(HandleAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                Assert.NotNull(usage);
                Assert.False(usage.AllowMultiple); // Default behavior
            }
        }

        public class NotificationAttributeTests
        {
            [Fact]
            public void NotificationAttribute_ShouldHaveDefaultValues()
            {
                // Arrange & Act
                var attribute = new NotificationAttribute();

                // Assert
                Assert.Equal(NotificationDispatchMode.Parallel, attribute.DispatchMode);
                Assert.Equal(0, attribute.Priority);
            }

            [Fact]
            public void NotificationAttribute_ShouldAllowSettingDispatchMode()
            {
                // Arrange
                const NotificationDispatchMode expectedMode = NotificationDispatchMode.Sequential;

                // Act
                var attribute = new NotificationAttribute { DispatchMode = expectedMode };

                // Assert
                Assert.Equal(expectedMode, attribute.DispatchMode);
            }

            [Fact]
            public void NotificationAttribute_ShouldAllowSettingPriority()
            {
                // Arrange
                const int expectedPriority = 50;

                // Act
                var attribute = new NotificationAttribute { Priority = expectedPriority };

                // Assert
                Assert.Equal(expectedPriority, attribute.Priority);
            }

            [Fact]
            public void NotificationAttribute_ShouldBeApplicableToMethods()
            {
                // Arrange
                var attributeType = typeof(NotificationAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                Assert.NotNull(usage);
                Assert.Equal(AttributeTargets.Method, usage.ValidOn);
            }
        }

        public class PipelineAttributeTests
        {
            [Fact]
            public void PipelineAttribute_ShouldHaveDefaultValues()
            {
                // Arrange & Act
                var attribute = new PipelineAttribute();

                // Assert
                Assert.Equal(0, attribute.Order);
                Assert.Equal(PipelineScope.All, attribute.Scope);
            }

            [Fact]
            public void PipelineAttribute_ShouldAllowSettingOrder()
            {
                // Arrange
                const int expectedOrder = -100;

                // Act
                var attribute = new PipelineAttribute { Order = expectedOrder };

                // Assert
                Assert.Equal(expectedOrder, attribute.Order);
            }

            [Fact]
            public void PipelineAttribute_ShouldAllowSettingScope()
            {
                // Arrange
                const PipelineScope expectedScope = PipelineScope.Requests;

                // Act
                var attribute = new PipelineAttribute { Scope = expectedScope };

                // Assert
                Assert.Equal(expectedScope, attribute.Scope);
            }

            [Fact]
            public void PipelineAttribute_ShouldBeApplicableToMethods()
            {
                // Arrange
                var attributeType = typeof(PipelineAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                Assert.NotNull(usage);
                Assert.Equal(AttributeTargets.Method, usage.ValidOn);
            }
        }

        public class NotificationDispatchModeTests
        {
            [Fact]
            public void NotificationDispatchMode_ShouldHaveExpectedValues()
            {
                // Assert
                var expectedValues = new[] { NotificationDispatchMode.Parallel, NotificationDispatchMode.Sequential };
                var actualValues = Enum.GetValues<NotificationDispatchMode>();
                foreach (var value in expectedValues)
                {
                    Assert.Contains(value, actualValues);
                }
            }

            [Fact]
            public void NotificationDispatchMode_ParallelShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(0, (int)NotificationDispatchMode.Parallel);
            }

            [Fact]
            public void NotificationDispatchMode_SequentialShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(1, (int)NotificationDispatchMode.Sequential);
            }
        }

        public class PipelineScopeTests
        {
            [Fact]
            public void PipelineScope_ShouldHaveExpectedValues()
            {
                // Assert
                var expectedValues = new[] { PipelineScope.All, PipelineScope.Requests, PipelineScope.Streams, PipelineScope.Notifications };
                var actualValues = Enum.GetValues<PipelineScope>();
                foreach (var value in expectedValues)
                {
                    Assert.Contains(value, actualValues);
                }
            }

            [Fact]
            public void PipelineScope_AllShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(0, (int)PipelineScope.All);
            }

            [Fact]
            public void PipelineScope_RequestsShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(1, (int)PipelineScope.Requests);
            }

            [Fact]
            public void PipelineScope_StreamsShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(2, (int)PipelineScope.Streams);
            }

            [Fact]
            public void PipelineScope_NotificationsShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(3, (int)PipelineScope.Notifications);
            }
        }

        public class ExposeAsEndpointAttributeTests
        {
            [Fact]
            public void ExposeAsEndpointAttribute_ShouldHaveDefaultValues()
            {
                // Arrange & Act
                var attribute = new ExposeAsEndpointAttribute();

                // Assert
                Assert.Null(attribute.Route);
                Assert.Equal("POST", attribute.HttpMethod);
                Assert.Null(attribute.Version);
            }

            [Fact]
            public void ExposeAsEndpointAttribute_ShouldAllowSettingRoute()
            {
                // Arrange
                const string expectedRoute = "/api/users";

                // Act
                var attribute = new ExposeAsEndpointAttribute { Route = expectedRoute };

                // Assert
                Assert.Equal(expectedRoute, attribute.Route);
            }

            [Fact]
            public void ExposeAsEndpointAttribute_ShouldAllowSettingHttpMethod()
            {
                // Arrange
                const string expectedMethod = "GET";

                // Act
                var attribute = new ExposeAsEndpointAttribute { HttpMethod = expectedMethod };

                // Assert
                Assert.Equal(expectedMethod, attribute.HttpMethod);
            }

            [Fact]
            public void ExposeAsEndpointAttribute_ShouldAllowSettingVersion()
            {
                // Arrange
                const string expectedVersion = "v2";

                // Act
                var attribute = new ExposeAsEndpointAttribute { Version = expectedVersion };

                // Assert
                Assert.Equal(expectedVersion, attribute.Version);
            }

            [Fact]
            public void ExposeAsEndpointAttribute_ShouldBeApplicableToMethods()
            {
                // Arrange
                var attributeType = typeof(ExposeAsEndpointAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                Assert.NotNull(usage);
                Assert.Equal(AttributeTargets.Method, usage.ValidOn);
            }
        }
    }

    // Test classes to validate attribute usage
    public class TestHandlerClass
    {
        [Handle(Name = "TestHandler", Priority = 10)]
        public void TestHandleMethod() { }

        [Notification(DispatchMode = NotificationDispatchMode.Sequential, Priority = 5)]
        public void TestNotificationMethod() { }

        [Pipeline(Order = -100, Scope = PipelineScope.Requests)]
        public void TestPipelineMethod() { }

        [ExposeAsEndpoint(Route = "/api/test", HttpMethod = "GET", Version = "v1")]
        public void TestEndpointMethod() { }
    }

    public class AttributeUsageValidationTests
    {
        [Fact]
        public void HandleAttribute_ShouldBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestHandlerClass).GetMethod(nameof(TestHandlerClass.TestHandleMethod));

            // Act
            var attribute = method!.GetCustomAttribute<HandleAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("TestHandler", attribute.Name);
            Assert.Equal(10, attribute.Priority);
        }

        [Fact]
        public void NotificationAttribute_ShouldBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestHandlerClass).GetMethod(nameof(TestHandlerClass.TestNotificationMethod));

            // Act
            var attribute = method!.GetCustomAttribute<NotificationAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal(NotificationDispatchMode.Sequential, attribute.DispatchMode);
            Assert.Equal(5, attribute.Priority);
        }

        [Fact]
        public void PipelineAttribute_ShouldBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestHandlerClass).GetMethod(nameof(TestHandlerClass.TestPipelineMethod));

            // Act
            var attribute = method!.GetCustomAttribute<PipelineAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal(-100, attribute.Order);
            Assert.Equal(PipelineScope.Requests, attribute.Scope);
        }

        [Fact]
        public void ExposeAsEndpointAttribute_ShouldBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestHandlerClass).GetMethod(nameof(TestHandlerClass.TestEndpointMethod));

            // Act
            var attribute = method!.GetCustomAttribute<ExposeAsEndpointAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("/api/test", attribute.Route);
            Assert.Equal("GET", attribute.HttpMethod);
            Assert.Equal("v1", attribute.Version);
        }
    }
}