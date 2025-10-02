using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
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
        public async Task ServiceFactory_Should_Work_In_Pipeline_Behavior()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayConfiguration();
            services.AddSingleton<ITestService, TestService>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TestServiceFactoryBehavior<,>));
            
            var serviceProvider = services.BuildServiceProvider();
            var relay = serviceProvider.GetRequiredService<IRelay>();

            // Act
            var request = new TestRequest();
            var response = await relay.SendAsync(request);

            // Assert - The behavior should have resolved the service
            Assert.Equal("Test", response);
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
                    return (TResponse)(object)testService.GetValue();
                }

                return await next();
            }
        }

        #endregion
    }
}
