using Relay.Core.Authorization;
using System;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationExceptionTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_Default_ShouldSetDefaultMessage()
        {
            // Act
            var exception = new AuthorizationException();

            // Assert
            Assert.Equal("Authorization failed.", exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithMessage_ShouldSetMessage()
        {
            // Arrange
            var message = "User is not authorized to perform this action.";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithNullMessage_ShouldAcceptNull()
        {
            // Act
            var exception = new AuthorizationException(null!);

            // Assert
            Assert.False(string.IsNullOrEmpty(exception.Message));
        }

        [Fact]
        public void Constructor_WithEmptyMessage_ShouldAcceptEmpty()
        {
            // Act
            var exception = new AuthorizationException(string.Empty);

            // Assert
            Assert.Equal(string.Empty, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
        {
            // Arrange
            var message = "Authorization failed due to invalid token.";
            var innerException = new InvalidOperationException("Token validation failed.");

            // Act
            var exception = new AuthorizationException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_WithMessageAndNullInnerException_ShouldSetMessageOnly()
        {
            // Arrange
            var message = "Access denied.";

            // Act
            var exception = new AuthorizationException(message, null!);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        #endregion

        #region Exception Behavior Tests

        [Fact]
        public void AuthorizationException_ShouldBeInstanceOfException()
        {
            // Act
            var exception = new AuthorizationException();

            // Assert
            Assert.IsAssignableFrom<Exception>(exception);
        }

        [Fact]
        public void AuthorizationException_ShouldBeThrowable()
        {
            // Arrange
            Action act = () => throw new AuthorizationException("Test exception");

            // Act & Assert
            var exception = Assert.Throws<AuthorizationException>(act);
            Assert.Equal("Test exception", exception.Message);
        }

        [Fact]
        public void AuthorizationException_ShouldBeCatchableAsException()
        {
            // Arrange
            Exception? caughtException = null;

            // Act
            try
            {
                throw new AuthorizationException("Test");
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.NotNull(caughtException);
            Assert.IsType<AuthorizationException>(caughtException);
        }

        [Fact]
        public void AuthorizationException_WithInnerException_ShouldPreserveStackTrace()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");
            var authException = new AuthorizationException("Auth failed", innerException);

            // Act
            Action act = () => throw authException;

            // Assert
            var exception = Assert.Throws<AuthorizationException>(act);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Equal("Inner error", exception.InnerException?.Message);
        }

        #endregion

        #region Message Formatting Tests

        [Fact]
        public void Constructor_WithFormattedMessage_ShouldPreserveFormatting()
        {
            // Arrange
            var userId = "user123";
            var resource = "AdminPanel";
            var message = $"User '{userId}' is not authorized to access '{resource}'.";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            Assert.Equal("User 'user123' is not authorized to access 'AdminPanel'.", exception.Message);
        }

        [Fact]
        public void Constructor_WithMultilineMessage_ShouldPreserveNewlines()
        {
            // Arrange
            var message = "Authorization failed.\nReason: Insufficient permissions.\nRequired role: Admin";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            Assert.Contains("Authorization failed.", exception.Message);
            Assert.Contains("Reason: Insufficient permissions.", exception.Message);
            Assert.Contains("Required role: Admin", exception.Message);
        }

        #endregion

        #region Exception Chain Tests

        [Fact]
        public void AuthorizationException_WithNestedExceptions_ShouldMaintainChain()
        {
            // Arrange
            var rootException = new InvalidOperationException("Root cause");
            var middleException = new ArgumentException("Middle layer", rootException);
            var authException = new AuthorizationException("Authorization layer", middleException);

            // Act & Assert
            Assert.Same(middleException, authException.InnerException);
            Assert.Same(rootException, authException.InnerException?.InnerException);
        }

        [Fact]
        public void AuthorizationException_WhenWrappingAnotherAuthException_ShouldNest()
        {
            // Arrange
            var innerAuthException = new AuthorizationException("Inner auth failure");
            var outerAuthException = new AuthorizationException("Outer auth failure", innerAuthException);

            // Act & Assert
            Assert.IsType<AuthorizationException>(outerAuthException.InnerException);
            Assert.Equal("Inner auth failure", outerAuthException.InnerException?.Message);
        }

        #endregion

        #region Real-World Scenario Tests

        [Fact]
        public void AuthorizationException_InsufficientPermissions_ShouldHaveDescriptiveMessage()
        {
            // Arrange
            var userName = "MuratDoe";
            var requiredPermission = "admin.edit";
            var message = $"User '{userName}' lacks required permission '{requiredPermission}'.";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            Assert.Contains(userName, exception.Message);
            Assert.Contains(requiredPermission, exception.Message);
        }

        [Fact]
        public void AuthorizationException_TokenExpired_ShouldWrapInnerException()
        {
            // Arrange
            var tokenException = new SecurityException("JWT token has expired");
            var message = "Authorization failed: Token expired.";

            // Act
            var exception = new AuthorizationException(message, tokenException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.IsType<SecurityException>(exception.InnerException);
        }

        [Fact]
        public void AuthorizationException_RoleBasedAccess_ShouldIndicateRequiredRole()
        {
            // Arrange
            var userRole = "User";
            var requiredRole = "Administrator";
            var message = $"Access denied. User role '{userRole}' does not have required role '{requiredRole}'.";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            Assert.Contains(userRole, exception.Message);
            Assert.Contains(requiredRole, exception.Message);
        }

        [Fact]
        public void AuthorizationException_ResourceBasedAccess_ShouldIndicateResource()
        {
            // Arrange
            var resourceId = "document-12345";
            var action = "delete";
            var message = $"User is not authorized to '{action}' resource '{resourceId}'.";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            Assert.Contains(resourceId, exception.Message);
            Assert.Contains(action, exception.Message);
        }

        #endregion

        #region Exception Handling Pattern Tests

        [Fact]
        public void AuthorizationException_ShouldBeCatchableSpecifically()
        {
            // Arrange
            var wasCaught = false;

            // Act
            try
            {
                throw new AuthorizationException("Unauthorized");
            }
            catch (AuthorizationException)
            {
                wasCaught = true;
            }

            // Assert
            Assert.True(wasCaught);
        }

        [Fact]
        public void AuthorizationException_ShouldAllowRethrow()
        {
            // Arrange
            Action act = () =>
            {
                try
                {
                    throw new AuthorizationException("Original");
                }
                catch (AuthorizationException)
                {
                    // Log or process
                    throw; // Rethrow
                }
            };

            // Act & Assert
            var exception = Assert.Throws<AuthorizationException>(act);
            Assert.Equal("Original", exception.Message);
        }

        [Fact]
        public void AuthorizationException_ShouldAllowWrappingInNewException()
        {
            // Arrange
            Action act = () =>
            {
                try
                {
                    throw new AuthorizationException("Inner failure");
                }
                catch (AuthorizationException ex)
                {
                    throw new AuthorizationException("Outer failure", ex);
                }
            };

            // Act & Assert
            var exception = Assert.Throws<AuthorizationException>(act);
            Assert.Equal("Outer failure", exception.Message);
            Assert.IsType<AuthorizationException>(exception.InnerException);
            Assert.Equal("Inner failure", exception.InnerException?.Message);
        }

        #endregion
    }

    // Helper exception for testing
    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
    }
}

