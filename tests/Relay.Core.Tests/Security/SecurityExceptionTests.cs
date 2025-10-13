using System;
using System.Linq;
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
                Assert.Equal(requestType, exception.RequestType);
                Assert.Equal(requiredPermissions, exception.RequiredPermissions);
                Assert.Contains(requestType, exception.Message);
                Assert.Contains("admin", exception.Message);
                Assert.Contains("read", exception.Message);
                Assert.Contains("write", exception.Message);
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
                Assert.Equal(requestType, exception.RequestType);
                Assert.Empty(exception.RequiredPermissions);
                Assert.Contains(requestType, exception.Message);
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
                Assert.Equal(requestType, exception.RequestType);
                 Assert.Single(exception.RequiredPermissions);
                 Assert.Equal("admin", exception.RequiredPermissions.First());
                Assert.Contains("admin", exception.Message);
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
                Assert.Equal(requestType, exception.RequestType);
                Assert.Equal(requiredPermissions, exception.RequiredPermissions);
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
                Assert.Equal(requestType, exception.RequestType);
                Assert.Equal(requiredPermissions, exception.RequiredPermissions);
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
                Assert.Equal(requestType, exception.RequestType);
                Assert.Equal(100, exception.RequiredPermissions.Count());
                Assert.Contains(requestType, exception.Message);
                Assert.Contains("permission_1", exception.Message);
                Assert.Contains("permission_100", exception.Message);
            }

            [Fact]
            public void Exception_ShouldBeSerializable()
            {
                // Arrange
                var requestType = "TestRequest";
                var requiredPermissions = new[] { "admin", "read" };
                var exception = new InsufficientPermissionsException(requestType, requiredPermissions);

                // Act & Assert
                Assert.IsAssignableFrom<Exception>(exception);
                Assert.Null(exception.InnerException);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
                 Assert.Contains(userId, exception.Message);
                 Assert.Contains(requestType, exception.Message);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
                 Assert.Contains(requestType, exception.Message);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
                 Assert.Contains(userId, exception.Message);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
                 Assert.Contains(requestType, exception.Message);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
                 Assert.Contains(userId, exception.Message);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
                 Assert.Contains(userId, exception.Message);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
                 Assert.Contains(requestType, exception.Message);
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
                 Assert.Equal(userId, exception.UserId);
                 Assert.Equal(requestType, exception.RequestType);
                 Assert.Contains(userId, exception.Message);
                 Assert.Contains(requestType, exception.Message);
             }

            [Fact]
            public void Exception_ShouldBeSerializable()
            {
                // Arrange
                var userId = "user123";
                var requestType = "TestRequest";
                var exception = new RateLimitExceededException(userId, requestType);

                // Act & Assert
                Assert.IsAssignableFrom<Exception>(exception);
                Assert.Null(exception.InnerException);
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
                Assert.True(exception.HResult < 0);
            }
        }
    }
}