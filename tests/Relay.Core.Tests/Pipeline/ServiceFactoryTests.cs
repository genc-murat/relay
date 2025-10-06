using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Extensions;
using Relay.Core.Pipeline;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for ServiceFactory delegate pattern and extensions.
    /// </summary>
    public class ServiceFactoryTests
    {
        [Fact]
        public void ServiceFactory_Should_Be_Registered_Automatically()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayConfiguration();

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var serviceFactory = serviceProvider.GetService<ServiceFactory>();

            // Assert
            Assert.NotNull(serviceFactory);
        }

        [Fact]
        public void ServiceFactory_Should_Resolve_Services()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act
            var service = factory(typeof(ITestService));

            // Assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<ITestService>(service);
        }

        [Fact]
        public void GetService_Extension_Should_Resolve_Service()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act
            var service = factory.GetService<ITestService>();

            // Assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<ITestService>(service);
        }

        [Fact]
        public void GetService_Extension_Should_Return_Null_For_Unregistered_Service()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act
            var service = factory.GetService<ITestService>();

            // Assert
            Assert.Null(service);
        }

        [Fact]
        public void GetRequiredService_Should_Resolve_Service()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act
            var service = factory.GetRequiredService<ITestService>();

            // Assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<ITestService>(service);
        }

        [Fact]
        public void GetRequiredService_Should_Throw_For_Unregistered_Service()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                factory.GetRequiredService<ITestService>());

            Assert.Contains("not registered", exception.Message);
        }

        [Fact]
        public void GetServices_Should_Resolve_Multiple_Services()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            services.AddSingleton<ITestService, TestService2>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act
            var resolvedServices = factory.GetServices<ITestService>().ToList();

            // Assert
            Assert.Equal(2, resolvedServices.Count);
        }

        [Fact]
        public void GetServices_Should_Return_Empty_For_Unregistered_Service()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act
            var resolvedServices = factory.GetServices<ITestService>().ToList();

            // Assert
            Assert.Empty(resolvedServices);
        }

        [Fact]
        public void TryGetService_Should_Return_True_For_Registered_Service()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act
            var result = factory.TryGetService<ITestService>(out var service);

            // Assert
            Assert.True(result);
            Assert.NotNull(service);
        }

        [Fact]
        public void TryGetService_Should_Return_False_For_Unregistered_Service()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            ServiceFactory factory = serviceProvider.GetService;

            // Act
            var result = factory.TryGetService<ITestService>(out var service);

            // Assert
            Assert.False(result);
            Assert.Null(service);
        }

        [Fact]
        public void ServiceFactory_Should_Be_Injected_Into_Pipeline_Behavior()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayConfiguration();
            services.AddSingleton<ITestService, TestService>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TestServiceFactoryBehavior<,>));
            
            var serviceProvider = services.BuildServiceProvider();

            // Act - Resolve the pipeline behavior
            var behavior = serviceProvider.GetService<IPipelineBehavior<TestRequest, string>>();

            // Assert - Verify behavior was created with ServiceFactory
            Assert.NotNull(behavior);
            Assert.IsType<TestServiceFactoryBehavior<TestRequest, string>>(behavior);
        }

        [Fact]
        public async Task ServiceFactory_Can_Resolve_Services_In_Behavior()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayConfiguration();
            services.AddSingleton<ITestService, TestService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var serviceFactory = serviceProvider.GetRequiredService<ServiceFactory>();

            // Create behavior manually
            var behavior = new TestServiceFactoryBehavior<TestRequest, string>(serviceFactory);

            // Act - Execute behavior
            var wasCalled = false;
            RequestHandlerDelegate<string> next = () =>
            {
                wasCalled = true;
                return new ValueTask<string>("FromNext");
            };

            var result = await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);

            // Assert - The behavior resolved the service and used it
            Assert.Equal("Test", result); // From TestService.GetValue()
            Assert.False(wasCalled); // next() should not be called
        }

        [Fact]
        public void RelayImplementation_Should_Expose_ServiceFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayConfiguration();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var relay = serviceProvider.GetRequiredService<IRelay>();

            // Assert
            Assert.IsType<RelayImplementation>(relay);
            var implementation = (RelayImplementation)relay;
            Assert.NotNull(implementation.ServiceFactory);
        }

        [Fact]
        public void ServiceFactory_Should_Handle_Null_Argument()
        {
            // Arrange
            ServiceFactory factory = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => factory.GetService<ITestService>());
        }

        [Fact]
        public void CreateScopedFactory_Should_Create_Valid_Factory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var factory = ServiceFactoryExtensions.CreateScopedFactory(serviceProvider);

            // Assert
            Assert.NotNull(factory);
            var service = factory.GetService<ITestService>();
            Assert.NotNull(service);
        }

        #region Test Classes

        public interface ITestService
        {
            string GetValue();
        }

        public class TestService : ITestService
        {
            public string GetValue() => "Test";
        }

        public class TestService2 : ITestService
        {
            public string GetValue() => "Test2";
        }

        public record TestRequest : IRequest<string>;

        public class TestRequestHandler : IRequestHandler<TestRequest, string>
        {
            public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            {
                return new ValueTask<string>("DefaultHandler");
            }
        }

        public class TestServiceFactoryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        {
            private readonly ServiceFactory _serviceFactory;

            public TestServiceFactoryBehavior(ServiceFactory serviceFactory)
            {
                _serviceFactory = serviceFactory;
            }

            public async ValueTask<TResponse> HandleAsync(
                TRequest request,
                RequestHandlerDelegate<TResponse> next,
                CancellationToken cancellationToken)
            {
                // Resolve service using factory
                var testService = _serviceFactory.GetService<ITestService>();
                
                if (testService != null && typeof(TResponse) == typeof(string))
                {
                    // Return service value instead of calling next
                    return (TResponse)(object)testService.GetValue();
                }

                return await next();
            }
        }

        #endregion
    }
}
