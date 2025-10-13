using Relay.Core.Authorization;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Authorization
{
    public class AuthorizationTests
    {
        #region DefaultAuthorizationContext Tests

        [Fact]
        public void DefaultAuthorizationContext_Should_Initialize_WithEmptyCollections()
        {
            // Arrange & Act
            var context = new DefaultAuthorizationContext();

            // Assert
            Assert.NotNull(context.UserClaims);
            Assert.Empty(context.UserClaims);
            Assert.NotNull(context.UserRoles);
            Assert.Empty(context.UserRoles);
            Assert.NotNull(context.Properties);
            Assert.Empty(context.Properties);
        }

        [Fact]
        public void DefaultAuthorizationContext_Should_AllowSettingUserClaims()
        {
            // Arrange
            var context = new DefaultAuthorizationContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };

            // Act
            context.UserClaims = claims;

            // Assert
            Assert.Equal(claims, context.UserClaims);
        }

        [Fact]
        public void DefaultAuthorizationContext_Should_AllowSettingUserRoles()
        {
            // Arrange
            var context = new DefaultAuthorizationContext();
            var roles = new List<string> { "Admin", "User" };

            // Act
            context.UserRoles = roles;

            // Assert
            Assert.Equal(roles, context.UserRoles);
        }

        [Fact]
        public void DefaultAuthorizationContext_Should_AllowAddingProperties()
        {
            // Arrange
            var context = new DefaultAuthorizationContext();

            // Act
            context.Properties["RequestType"] = "TestRequest";
            context.Properties["UserId"] = 123;

            // Assert
            Assert.Equal(2, context.Properties.Count);
            Assert.Equal("TestRequest", context.Properties["RequestType"]);
            Assert.Equal(123, context.Properties["UserId"]);
        }

        #endregion

        #region DefaultAuthorizationService Tests

        [Fact]
        public async Task DefaultAuthorizationService_Should_AlwaysReturnTrue()
        {
            // Arrange
            var service = new DefaultAuthorizationService();
            var context = new DefaultAuthorizationContext();

            // Act
            var result = await service.AuthorizeAsync(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DefaultAuthorizationService_Should_AcceptCancellationToken()
        {
            // Arrange
            var service = new DefaultAuthorizationService();
            var context = new DefaultAuthorizationContext();
            var cts = new CancellationTokenSource();

            // Act
            var result = await service.AuthorizeAsync(context, cts.Token);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region AuthorizationException Tests

        [Fact]
        public void AuthorizationException_Should_CreateWithDefaultMessage()
        {
            // Act
            var exception = new AuthorizationException();

            // Assert
            Assert.Equal("Authorization failed.", exception.Message);
        }

        [Fact]
        public void AuthorizationException_Should_CreateWithCustomMessage()
        {
            // Arrange
            var message = "Custom authorization failure";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void AuthorizationException_Should_CreateWithInnerException()
        {
            // Arrange
            var message = "Authorization failed";
            var inner = new InvalidOperationException("Inner error");

            // Act
            var exception = new AuthorizationException(message, inner);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(inner, exception.InnerException);
        }

        #endregion

        #region AuthorizeAttribute Tests

        [Fact]
        public void AuthorizeAttribute_Should_InitializeWithRoles()
        {
            // Arrange & Act
            var attribute = new AuthorizeAttribute("Admin", "User");

            // Assert
            Assert.Equal(new[] { "Admin", "User" }, attribute.Roles);
            Assert.Empty(attribute.Policies);
            Assert.Empty(attribute.AuthenticationSchemes);
        }

        [Fact]
        public void AuthorizeAttribute_Should_InitializeWithPolicies()
        {
            // Arrange & Act
            var attribute = new AuthorizeAttribute(usePolicies: true, "Policy1", "Policy2");

            // Assert
            Assert.Equal(new[] { "Policy1", "Policy2" }, attribute.Policies);
            Assert.Empty(attribute.Roles);
            Assert.Empty(attribute.AuthenticationSchemes);
        }

        [Fact]
        public void AuthorizeAttribute_Should_HandleNullRoles()
        {
            // Arrange & Act
            var attribute = new AuthorizeAttribute(null!);

            // Assert
            Assert.Empty(attribute.Roles);
        }

        [Fact]
        public void AuthorizeAttribute_Should_HandleNullPolicies()
        {
            // Arrange & Act
            var attribute = new AuthorizeAttribute(usePolicies: true, null!);

            // Assert
            Assert.Empty(attribute.Policies);
        }

        [Fact]
        public void AuthorizeAttribute_Should_AllowMultipleInstances()
        {
            // This test verifies the AllowMultiple = true attribute property
            var attributes = typeof(TestRequestWithMultipleAuthorize).GetCustomAttributes(typeof(AuthorizeAttribute), true);
            
            // Assert
            Assert.Equal(2, attributes.Length);
        }

        #endregion

        #region AuthorizationOptions Tests

        [Fact]
        public void AuthorizationOptions_Should_HaveCorrectDefaults()
        {
            // Arrange & Act
            var options = new AuthorizationOptions();

            // Assert
            Assert.False(options.EnableAutomaticAuthorization);
            Assert.True(options.ThrowOnAuthorizationFailure);
            Assert.Equal(-3000, options.DefaultOrder);
        }

        [Fact]
        public void AuthorizationOptions_Should_AllowCustomization()
        {
            // Arrange & Act
            var options = new AuthorizationOptions
            {
                EnableAutomaticAuthorization = true,
                ThrowOnAuthorizationFailure = false,
                DefaultOrder = -5000
            };

            // Assert
            Assert.True(options.EnableAutomaticAuthorization);
            Assert.False(options.ThrowOnAuthorizationFailure);
            Assert.Equal(-5000, options.DefaultOrder);
        }

        #endregion

        #region Test Helper Classes

        [Authorize("Admin", "User")]
        [Authorize(usePolicies: true, "Policy1")]
        public class TestRequestWithMultipleAuthorize : IRequest<string>
        {
        }

        public class TestRequest : IRequest<string>
        {
        }

        [Authorize("Admin")]
        public class TestAuthorizedRequest : IRequest<string>
        {
        }

        public class TestRequestHandler : IRequestHandler<TestRequest, string>
        {
            public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            {
                return new ValueTask<string>("Success");
            }
        }

        #endregion
    }
}
