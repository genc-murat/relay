using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Authorization;
using Relay.Core.Configuration;
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
            context.UserClaims.Should().NotBeNull().And.BeEmpty();
            context.UserRoles.Should().NotBeNull().And.BeEmpty();
            context.Properties.Should().NotBeNull().And.BeEmpty();
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
            context.UserClaims.Should().BeEquivalentTo(claims);
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
            context.UserRoles.Should().BeEquivalentTo(roles);
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
            context.Properties.Should().HaveCount(2);
            context.Properties["RequestType"].Should().Be("TestRequest");
            context.Properties["UserId"].Should().Be(123);
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
            result.Should().BeTrue();
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
            result.Should().BeTrue();
        }

        #endregion

        #region AuthorizationException Tests

        [Fact]
        public void AuthorizationException_Should_CreateWithDefaultMessage()
        {
            // Act
            var exception = new AuthorizationException();

            // Assert
            exception.Message.Should().Be("Authorization failed.");
        }

        [Fact]
        public void AuthorizationException_Should_CreateWithCustomMessage()
        {
            // Arrange
            var message = "Custom authorization failure";

            // Act
            var exception = new AuthorizationException(message);

            // Assert
            exception.Message.Should().Be(message);
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
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeSameAs(inner);
        }

        #endregion

        #region AuthorizeAttribute Tests

        [Fact]
        public void AuthorizeAttribute_Should_InitializeWithRoles()
        {
            // Arrange & Act
            var attribute = new AuthorizeAttribute("Admin", "User");

            // Assert
            attribute.Roles.Should().BeEquivalentTo(new[] { "Admin", "User" });
            attribute.Policies.Should().BeEmpty();
            attribute.AuthenticationSchemes.Should().BeEmpty();
        }

        [Fact]
        public void AuthorizeAttribute_Should_InitializeWithPolicies()
        {
            // Arrange & Act
            var attribute = new AuthorizeAttribute(usePolicies: true, "Policy1", "Policy2");

            // Assert
            attribute.Policies.Should().BeEquivalentTo(new[] { "Policy1", "Policy2" });
            attribute.Roles.Should().BeEmpty();
            attribute.AuthenticationSchemes.Should().BeEmpty();
        }

        [Fact]
        public void AuthorizeAttribute_Should_HandleNullRoles()
        {
            // Arrange & Act
            var attribute = new AuthorizeAttribute(null!);

            // Assert
            attribute.Roles.Should().BeEmpty();
        }

        [Fact]
        public void AuthorizeAttribute_Should_HandleNullPolicies()
        {
            // Arrange & Act
            var attribute = new AuthorizeAttribute(usePolicies: true, null!);

            // Assert
            attribute.Policies.Should().BeEmpty();
        }

        [Fact]
        public void AuthorizeAttribute_Should_AllowMultipleInstances()
        {
            // This test verifies the AllowMultiple = true attribute property
            var attributes = typeof(TestRequestWithMultipleAuthorize).GetCustomAttributes(typeof(AuthorizeAttribute), true);
            
            // Assert
            attributes.Should().HaveCount(2);
        }

        #endregion

        #region AuthorizationOptions Tests

        [Fact]
        public void AuthorizationOptions_Should_HaveCorrectDefaults()
        {
            // Arrange & Act
            var options = new AuthorizationOptions();

            // Assert
            options.EnableAutomaticAuthorization.Should().BeFalse();
            options.ThrowOnAuthorizationFailure.Should().BeTrue();
            options.DefaultOrder.Should().Be(-3000);
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
            options.EnableAutomaticAuthorization.Should().BeTrue();
            options.ThrowOnAuthorizationFailure.Should().BeFalse();
            options.DefaultOrder.Should().Be(-5000);
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
