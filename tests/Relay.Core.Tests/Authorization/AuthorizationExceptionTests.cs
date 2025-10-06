using System;
using FluentAssertions;
using Relay.Core.Authorization;
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
            exception.Message.Should().Be("Authorization failed.");
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithMessage_ShouldSetMessage()
        {
            // Arrange
            var message = "User is not authorized to perform this action.";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithNullMessage_ShouldAcceptNull()
        {
            // Act
            var exception = new AuthorizationException(null!);

            // Assert
            exception.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Constructor_WithEmptyMessage_ShouldAcceptEmpty()
        {
            // Act
            var exception = new AuthorizationException(string.Empty);

            // Assert
            exception.Message.Should().Be(string.Empty);
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
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeSameAs(innerException);
        }

        [Fact]
        public void Constructor_WithMessageAndNullInnerException_ShouldSetMessageOnly()
        {
            // Arrange
            var message = "Access denied.";

            // Act
            var exception = new AuthorizationException(message, null!);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeNull();
        }

        #endregion

        #region Exception Behavior Tests

        [Fact]
        public void AuthorizationException_ShouldBeInstanceOfException()
        {
            // Act
            var exception = new AuthorizationException();

            // Assert
            exception.Should().BeAssignableTo<Exception>();
        }

        [Fact]
        public void AuthorizationException_ShouldBeThrowable()
        {
            // Arrange
            Action act = () => throw new AuthorizationException("Test exception");

            // Act & Assert
            act.Should().Throw<AuthorizationException>()
                .WithMessage("Test exception");
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
            caughtException.Should().NotBeNull();
            caughtException.Should().BeOfType<AuthorizationException>();
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
            act.Should().Throw<AuthorizationException>()
                .WithInnerException<InvalidOperationException>()
                .WithMessage("Inner error");
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
            exception.Message.Should().Be("User 'user123' is not authorized to access 'AdminPanel'.");
        }

        [Fact]
        public void Constructor_WithMultilineMessage_ShouldPreserveNewlines()
        {
            // Arrange
            var message = "Authorization failed.\nReason: Insufficient permissions.\nRequired role: Admin";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            exception.Message.Should().Contain("Authorization failed.")
                .And.Contain("Reason: Insufficient permissions.")
                .And.Contain("Required role: Admin");
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
            authException.InnerException.Should().BeSameAs(middleException);
            authException.InnerException?.InnerException.Should().BeSameAs(rootException);
        }

        [Fact]
        public void AuthorizationException_WhenWrappingAnotherAuthException_ShouldNest()
        {
            // Arrange
            var innerAuthException = new AuthorizationException("Inner auth failure");
            var outerAuthException = new AuthorizationException("Outer auth failure", innerAuthException);

            // Act & Assert
            outerAuthException.InnerException.Should().BeOfType<AuthorizationException>();
            outerAuthException.InnerException?.Message.Should().Be("Inner auth failure");
        }

        #endregion

        #region Real-World Scenario Tests

        [Fact]
        public void AuthorizationException_InsufficientPermissions_ShouldHaveDescriptiveMessage()
        {
            // Arrange
            var userName = "JohnDoe";
            var requiredPermission = "admin.write";
            var message = $"User '{userName}' lacks required permission '{requiredPermission}'.";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            exception.Message.Should().Contain(userName)
                .And.Contain(requiredPermission);
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
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeOfType<SecurityException>();
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
            exception.Message.Should().Contain(userRole)
                .And.Contain(requiredRole);
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
            exception.Message.Should().Contain(resourceId)
                .And.Contain(action);
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
            wasCaught.Should().BeTrue();
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
                catch (AuthorizationException ex)
                {
                    // Log or process
                    throw; // Rethrow
                }
            };

            // Act & Assert
            act.Should().Throw<AuthorizationException>()
                .WithMessage("Original");
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
            act.Should().Throw<AuthorizationException>()
                .WithMessage("Outer failure")
                .WithInnerException<AuthorizationException>()
                .WithMessage("Inner failure");
        }

        #endregion
    }

    // Helper exception for testing
    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
    }
}
