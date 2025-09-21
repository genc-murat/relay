using System;
using System.Reflection;
using FluentAssertions;
using Relay.Core;
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
                attribute.Name.Should().BeNull();
                attribute.Priority.Should().Be(0);
            }

            [Fact]
            public void HandleAttribute_ShouldAllowSettingName()
            {
                // Arrange
                const string expectedName = "TestHandler";

                // Act
                var attribute = new HandleAttribute { Name = expectedName };

                // Assert
                attribute.Name.Should().Be(expectedName);
            }

            [Fact]
            public void HandleAttribute_ShouldAllowSettingPriority()
            {
                // Arrange
                const int expectedPriority = 100;

                // Act
                var attribute = new HandleAttribute { Priority = expectedPriority };

                // Assert
                attribute.Priority.Should().Be(expectedPriority);
            }

            [Fact]
            public void HandleAttribute_ShouldBeApplicableToMethods()
            {
                // Arrange
                var attributeType = typeof(HandleAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                usage.Should().NotBeNull();
                usage!.ValidOn.Should().Be(AttributeTargets.Method);
            }

            [Fact]
            public void HandleAttribute_ShouldAllowMultipleOnSameTarget()
            {
                // Arrange
                var attributeType = typeof(HandleAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                usage.Should().NotBeNull();
                usage!.AllowMultiple.Should().BeFalse(); // Default behavior
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
                attribute.DispatchMode.Should().Be(NotificationDispatchMode.Parallel);
                attribute.Priority.Should().Be(0);
            }

            [Fact]
            public void NotificationAttribute_ShouldAllowSettingDispatchMode()
            {
                // Arrange
                const NotificationDispatchMode expectedMode = NotificationDispatchMode.Sequential;

                // Act
                var attribute = new NotificationAttribute { DispatchMode = expectedMode };

                // Assert
                attribute.DispatchMode.Should().Be(expectedMode);
            }

            [Fact]
            public void NotificationAttribute_ShouldAllowSettingPriority()
            {
                // Arrange
                const int expectedPriority = 50;

                // Act
                var attribute = new NotificationAttribute { Priority = expectedPriority };

                // Assert
                attribute.Priority.Should().Be(expectedPriority);
            }

            [Fact]
            public void NotificationAttribute_ShouldBeApplicableToMethods()
            {
                // Arrange
                var attributeType = typeof(NotificationAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                usage.Should().NotBeNull();
                usage!.ValidOn.Should().Be(AttributeTargets.Method);
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
                attribute.Order.Should().Be(0);
                attribute.Scope.Should().Be(PipelineScope.All);
            }

            [Fact]
            public void PipelineAttribute_ShouldAllowSettingOrder()
            {
                // Arrange
                const int expectedOrder = -100;

                // Act
                var attribute = new PipelineAttribute { Order = expectedOrder };

                // Assert
                attribute.Order.Should().Be(expectedOrder);
            }

            [Fact]
            public void PipelineAttribute_ShouldAllowSettingScope()
            {
                // Arrange
                const PipelineScope expectedScope = PipelineScope.Requests;

                // Act
                var attribute = new PipelineAttribute { Scope = expectedScope };

                // Assert
                attribute.Scope.Should().Be(expectedScope);
            }

            [Fact]
            public void PipelineAttribute_ShouldBeApplicableToMethods()
            {
                // Arrange
                var attributeType = typeof(PipelineAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                usage.Should().NotBeNull();
                usage!.ValidOn.Should().Be(AttributeTargets.Method);
            }
        }

        public class NotificationDispatchModeTests
        {
            [Fact]
            public void NotificationDispatchMode_ShouldHaveExpectedValues()
            {
                // Assert
                Enum.GetValues<NotificationDispatchMode>().Should().Contain(new[]
                {
                    NotificationDispatchMode.Parallel,
                    NotificationDispatchMode.Sequential
                });
            }

            [Fact]
            public void NotificationDispatchMode_ParallelShouldHaveCorrectValue()
            {
                // Assert
                ((int)NotificationDispatchMode.Parallel).Should().Be(0);
            }

            [Fact]
            public void NotificationDispatchMode_SequentialShouldHaveCorrectValue()
            {
                // Assert
                ((int)NotificationDispatchMode.Sequential).Should().Be(1);
            }
        }

        public class PipelineScopeTests
        {
            [Fact]
            public void PipelineScope_ShouldHaveExpectedValues()
            {
                // Assert
                Enum.GetValues<PipelineScope>().Should().Contain(new[]
                {
                    PipelineScope.All,
                    PipelineScope.Requests,
                    PipelineScope.Streams,
                    PipelineScope.Notifications
                });
            }

            [Fact]
            public void PipelineScope_AllShouldHaveCorrectValue()
            {
                // Assert
                ((int)PipelineScope.All).Should().Be(0);
            }

            [Fact]
            public void PipelineScope_RequestsShouldHaveCorrectValue()
            {
                // Assert
                ((int)PipelineScope.Requests).Should().Be(1);
            }

            [Fact]
            public void PipelineScope_StreamsShouldHaveCorrectValue()
            {
                // Assert
                ((int)PipelineScope.Streams).Should().Be(2);
            }

            [Fact]
            public void PipelineScope_NotificationsShouldHaveCorrectValue()
            {
                // Assert
                ((int)PipelineScope.Notifications).Should().Be(3);
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
                attribute.Route.Should().BeNull();
                attribute.HttpMethod.Should().Be("POST");
                attribute.Version.Should().BeNull();
            }

            [Fact]
            public void ExposeAsEndpointAttribute_ShouldAllowSettingRoute()
            {
                // Arrange
                const string expectedRoute = "/api/users";

                // Act
                var attribute = new ExposeAsEndpointAttribute { Route = expectedRoute };

                // Assert
                attribute.Route.Should().Be(expectedRoute);
            }

            [Fact]
            public void ExposeAsEndpointAttribute_ShouldAllowSettingHttpMethod()
            {
                // Arrange
                const string expectedMethod = "GET";

                // Act
                var attribute = new ExposeAsEndpointAttribute { HttpMethod = expectedMethod };

                // Assert
                attribute.HttpMethod.Should().Be(expectedMethod);
            }

            [Fact]
            public void ExposeAsEndpointAttribute_ShouldAllowSettingVersion()
            {
                // Arrange
                const string expectedVersion = "v2";

                // Act
                var attribute = new ExposeAsEndpointAttribute { Version = expectedVersion };

                // Assert
                attribute.Version.Should().Be(expectedVersion);
            }

            [Fact]
            public void ExposeAsEndpointAttribute_ShouldBeApplicableToMethods()
            {
                // Arrange
                var attributeType = typeof(ExposeAsEndpointAttribute);

                // Act
                var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

                // Assert
                usage.Should().NotBeNull();
                usage!.ValidOn.Should().Be(AttributeTargets.Method);
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
            attribute.Should().NotBeNull();
            attribute!.Name.Should().Be("TestHandler");
            attribute.Priority.Should().Be(10);
        }

        [Fact]
        public void NotificationAttribute_ShouldBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestHandlerClass).GetMethod(nameof(TestHandlerClass.TestNotificationMethod));

            // Act
            var attribute = method!.GetCustomAttribute<NotificationAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.DispatchMode.Should().Be(NotificationDispatchMode.Sequential);
            attribute.Priority.Should().Be(5);
        }

        [Fact]
        public void PipelineAttribute_ShouldBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestHandlerClass).GetMethod(nameof(TestHandlerClass.TestPipelineMethod));

            // Act
            var attribute = method!.GetCustomAttribute<PipelineAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Order.Should().Be(-100);
            attribute.Scope.Should().Be(PipelineScope.Requests);
        }

        [Fact]
        public void ExposeAsEndpointAttribute_ShouldBeAppliedToMethod()
        {
            // Arrange
            var method = typeof(TestHandlerClass).GetMethod(nameof(TestHandlerClass.TestEndpointMethod));

            // Act
            var attribute = method!.GetCustomAttribute<ExposeAsEndpointAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Route.Should().Be("/api/test");
            attribute.HttpMethod.Should().Be("GET");
            attribute.Version.Should().Be("v1");
        }
    }
}