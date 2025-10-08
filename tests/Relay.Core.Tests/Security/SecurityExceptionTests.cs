using System;
using System.Linq;
using FluentAssertions;
using Relay.Core.Security;
using Xunit;

namespace Relay.Core.Tests.Security
{
    public class SecurityExceptionTests
    {
        public class InsufficientPermissionsExceptionTests
        {
            [Fact]
            public void Constructor_ShouldSetProperties_WhenCalled()
            {
                // Arrange
                var requestType = "TestRequest";
                var requiredPermissions = new[] { "admin", "read", "write" };

                // Act
                var exception = new InsufficientPermissionsException(requestType, requiredPermissions);

                // Assert
                exception.RequestType.Should().Be(requestType);
                exception.RequiredPermissions.Should().BeEquivalentTo(requiredPermissions);
                exception.Message.Should().Contain(requestType);
                exception.Message.Should().Contain("admin");
                exception.Message.Should().Contain("read");
                exception.Message.Should().Contain("write");
            }

            [Fact]
            public void Constructor_ShouldHandleEmptyPermissions()
            {
                // Arrange
                var requestType = "TestRequest";
                var requiredPermissions = Array.Empty<string>();

                // Act
                var exception = new InsufficientPermissionsException(requestType, requiredPermissions);

                // Assert
                exception.RequestType.Should().Be(requestType);
                exception.RequiredPermissions.Should().BeEmpty();
                exception.Message.Should().Contain(requestType);
            }

            [Fact]
            public void Constructor_ShouldHandleNullPermissions()
            {
                // Arrange
                var requestType = "TestRequest";
                string[] requiredPermissions = null!;

                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => 
                    new InsufficientPermissionsException(requestType, requiredPermissions));
            }

            [Fact]
            public void Constructor_ShouldHandleSinglePermission()
            {
                // Arrange
                var requestType = "TestRequest";
                var requiredPermissions = new[] { "admin" };

                // Act
                var exception = new InsufficientPermissionsException(requestType, requiredPermissions);

                // Assert
                exception.RequestType.Should().Be(requestType);
                exception.RequiredPermissions.Should().ContainSingle().Which.Should().Be("admin");
                exception.Message.Should().Contain("admin");
            }

            [Fact]
            public void Constructor_ShouldHandleEmptyRequestType()
            {
                // Arrange
                var requestType = "";
                var requiredPermissions = new[] { "admin" };

                // Act
                var exception = new InsufficientPermissionsException(requestType, requiredPermissions);

                // Assert
                exception.RequestType.Should().Be(requestType);
                exception.RequiredPermissions.Should().BeEquivalentTo(requiredPermissions);
            }

            [Fact]
            public void Constructor_ShouldHandleWhitespaceRequestType()
            {
                // Arrange
                var requestType = "   ";
                var requiredPermissions = new[] { "admin" };

                // Act
                var exception = new InsufficientPermissionsException(requestType, requiredPermissions);

                // Assert
                exception.RequestType.Should().Be(requestType);
                exception.RequiredPermissions.Should().BeEquivalentTo(requiredPermissions);
            }

            [Fact]
            public void Constructor_ShouldHandleManyPermissions()
            {
                // Arrange
                var requestType = "ComplexRequest";
                var requiredPermissions = Enumerable.Range(1, 100)
                    .Select(i => $"permission_{i}")
                    .ToArray();

                // Act
                var exception = new InsufficientPermissionsException(requestType, requiredPermissions);

                // Assert
                exception.RequestType.Should().Be(requestType);
                exception.RequiredPermissions.Should().HaveCount(100);
                exception.Message.Should().Contain(requestType);
                exception.Message.Should().Contain("permission_1");
                exception.Message.Should().Contain("permission_100");
            }

            [Fact]
            public void Exception_ShouldBeSerializable()
            {
                // Arrange
                var requestType = "TestRequest";
                var requiredPermissions = new[] { "admin", "read" };
                var exception = new InsufficientPermissionsException(requestType, requiredPermissions);

                // Act & Assert
                exception.Should().BeAssignableTo<Exception>();
                exception.InnerException.Should().BeNull();
            }
        }

        public class RateLimitExceededExceptionTests
        {
            [Fact]
            public void Constructor_ShouldSetProperties_WhenCalled()
            {
                // Arrange
                var userId = "user123";
                var requestType = "TestRequest";

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
                exception.Message.Should().Contain(userId);
                exception.Message.Should().Contain(requestType);
            }

            [Fact]
            public void Constructor_ShouldHandleEmptyUserId()
            {
                // Arrange
                var userId = "";
                var requestType = "TestRequest";

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
                exception.Message.Should().Contain(requestType);
            }

            [Fact]
            public void Constructor_ShouldHandleEmptyRequestType()
            {
                // Arrange
                var userId = "user123";
                var requestType = "";

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
                exception.Message.Should().Contain(userId);
            }

            [Fact]
            public void Constructor_ShouldHandleNullUserId()
            {
                // Arrange
                string userId = null!;
                var requestType = "TestRequest";

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
                exception.Message.Should().Contain(requestType);
            }

            [Fact]
            public void Constructor_ShouldHandleNullRequestType()
            {
                // Arrange
                var userId = "user123";
                string requestType = null!;

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
                exception.Message.Should().Contain(userId);
            }

            [Fact]
            public void Constructor_ShouldHandleWhitespaceValues()
            {
                // Arrange
                var userId = "   ";
                var requestType = "   ";

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
            }

            [Fact]
            public void Constructor_ShouldHandleLongUserId()
            {
                // Arrange
                var userId = new string('a', 1000);
                var requestType = "TestRequest";

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
                exception.Message.Should().Contain(userId);
            }

            [Fact]
            public void Constructor_ShouldHandleLongRequestType()
            {
                // Arrange
                var userId = "user123";
                var requestType = new string('b', 1000);

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
                exception.Message.Should().Contain(requestType);
            }

            [Fact]
            public void Constructor_ShouldHandleSpecialCharacters()
            {
                // Arrange
                var userId = "user@domain.com";
                var requestType = "Request-Type_123";

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.UserId.Should().Be(userId);
                exception.RequestType.Should().Be(requestType);
                exception.Message.Should().Contain(userId);
                exception.Message.Should().Contain(requestType);
            }

            [Fact]
            public void Exception_ShouldBeSerializable()
            {
                // Arrange
                var userId = "user123";
                var requestType = "TestRequest";
                var exception = new RateLimitExceededException(userId, requestType);

                // Act & Assert
                exception.Should().BeAssignableTo<Exception>();
                exception.InnerException.Should().BeNull();
            }

            [Fact]
            public void Exception_ShouldHaveCorrectHResult()
            {
                // Arrange
                var userId = "user123";
                var requestType = "TestRequest";

                // Act
                var exception = new RateLimitExceededException(userId, requestType);

                // Assert
                exception.HResult.Should().BeNegative();
            }
        }
    }
}