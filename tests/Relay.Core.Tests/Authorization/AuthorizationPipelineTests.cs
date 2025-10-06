using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Authorization;
using Relay.Core.Configuration;
using Relay.Core.Configuration.Options;
using Xunit;

namespace Relay.Core.Tests.Authorization;

public class AuthorizationPipelineTests
{
    [Authorize("Admin")]
    public class AuthorizedRequest : IRequest<string>
    {
        public string Data { get; set; } = "";
    }

    public class UnauthorizedRequest : IRequest<string>
    {
        public string Data { get; set; } = "";
    }

    public class TestAuthHandler :
        IRequestHandler<AuthorizedRequest, string>,
        IRequestHandler<UnauthorizedRequest, string>
    {
        public ValueTask<string> HandleAsync(AuthorizedRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<string>($"Handled: {request.Data}");
        }

        public ValueTask<string> HandleAsync(UnauthorizedRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<string>($"Handled: {request.Data}");
        }
    }

    public class TestAuthorizationContext : IAuthorizationContext
    {
        public TestAuthorizationContext(params string[] roles)
        {
            UserRoles = roles;
            Properties = new Dictionary<string, object>();
            UserClaims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        }

        public IEnumerable<Claim> UserClaims { get; }
        public IEnumerable<string> UserRoles { get; }
        public IDictionary<string, object> Properties { get; }
    }

    public class TestAuthorizationService : IAuthorizationService
    {
        private readonly HashSet<string> _allowedRoles;

        public TestAuthorizationService(params string[] allowedRoles)
        {
            _allowedRoles = new HashSet<string>(allowedRoles);
        }

        public ValueTask<bool> AuthorizeAsync(IAuthorizationContext context, CancellationToken cancellationToken = default)
        {
            var userRoles = context.UserRoles;
            return new ValueTask<bool>(userRoles.Any(role => _allowedRoles.Contains(role)));
        }
    }

    [Fact]
    public async Task Should_AllowAccess_When_UserHasRequiredRole()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestAuthHandler();
        var authService = new TestAuthorizationService("Admin");
        var authContext = new TestAuthorizationContext("Admin");

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddSingleton<IAuthorizationService>(authService);
        services.AddSingleton<IAuthorizationContext>(authContext);
        services.Configure<RelayOptions>(opt =>
        {
            opt.DefaultAuthorizationOptions.EnableAutomaticAuthorization = true;
        });
        services.AddTransient<IPipelineBehavior<AuthorizedRequest, string>, AuthorizationPipelineBehavior<AuthorizedRequest, string>>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new AuthorizedRequest { Data = "test" };

        // Act
        var result = await executor.ExecuteAsync<AuthorizedRequest, string>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        result.Should().Be("Handled: test");
    }

    public class AlwaysDenyAuthorizationService : IAuthorizationService
    {
        public ValueTask<bool> AuthorizeAsync(IAuthorizationContext context, CancellationToken cancellationToken = default)
        {
            return new ValueTask<bool>(false); // Always deny
        }
    }

    [Fact]
    public async Task Should_ThrowAuthorizationException_When_UserDoesNotHaveRequiredRole()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestAuthHandler();
        var authService = new AlwaysDenyAuthorizationService(); // Always denies
        var authContext = new TestAuthorizationContext("User");

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddSingleton<IAuthorizationService>(authService);
        services.AddSingleton<IAuthorizationContext>(authContext);
        services.Configure<RelayOptions>(opt =>
        {
            opt.DefaultAuthorizationOptions.ThrowOnAuthorizationFailure = true;
            opt.DefaultAuthorizationOptions.EnableAutomaticAuthorization = true;
        });
        services.AddTransient<IPipelineBehavior<AuthorizedRequest, string>, AuthorizationPipelineBehavior<AuthorizedRequest, string>>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new AuthorizedRequest { Data = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(async () =>
        {
            await executor.ExecuteAsync<AuthorizedRequest, string>(
                request,
                (r, c) => handler.HandleAsync(r, c),
                CancellationToken.None);
        });
    }

    [Fact]
    public async Task Should_SkipAuthorization_When_AttributeIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestAuthHandler();
        var authService = new TestAuthorizationService(); // No roles
        var authContext = new TestAuthorizationContext(); // No roles

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddSingleton<IAuthorizationService>(authService);
        services.AddSingleton<IAuthorizationContext>(authContext);
        services.Configure<RelayOptions>(opt =>
        {
            opt.DefaultAuthorizationOptions.EnableAutomaticAuthorization = false;
        });
        services.AddTransient<IPipelineBehavior<UnauthorizedRequest, string>, AuthorizationPipelineBehavior<UnauthorizedRequest, string>>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new UnauthorizedRequest { Data = "test" };

        // Act
        var result = await executor.ExecuteAsync<UnauthorizedRequest, string>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert - Should not throw, attribute is missing
        result.Should().Be("Handled: test");
    }

    [Authorize("Role1", "Role2")]
    public class MultiRoleRequest : IRequest<string> { }

    public class MultiRoleHandler : IRequestHandler<MultiRoleRequest, string>
    {
        public ValueTask<string> HandleAsync(MultiRoleRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<string>("Success");
        }
    }

    [Fact]
    public async Task Should_AllowAccess_When_UserHasAnyOfTheRequiredRoles()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new MultiRoleHandler();
        var authService = new TestAuthorizationService("Role2"); // Has Role2
        var authContext = new TestAuthorizationContext("Role2");

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddSingleton<IAuthorizationService>(authService);
        services.AddSingleton<IAuthorizationContext>(authContext);
        services.Configure<RelayOptions>(opt =>
        {
            opt.DefaultAuthorizationOptions.EnableAutomaticAuthorization = true;
        });
        services.AddTransient<IPipelineBehavior<MultiRoleRequest, string>, AuthorizationPipelineBehavior<MultiRoleRequest, string>>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new MultiRoleRequest();

        // Act
        var result = await executor.ExecuteAsync<MultiRoleRequest, string>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        result.Should().Be("Success");
    }
}
