using System;
using System.Linq;
using Relay.Core.Security;
using Relay.Core.Security.Exceptions;
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
    }
}