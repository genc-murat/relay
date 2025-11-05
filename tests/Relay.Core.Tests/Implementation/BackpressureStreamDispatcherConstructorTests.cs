using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Dispatchers;
using Xunit;

namespace Relay.Core.Tests.Implementation
{
    public class BackpressureStreamDispatcherConstructorTests
    {
        // Test request and response types
        public class TestStreamRequest : IStreamRequest<int>
        {
            public int Count { get; set; } = 10;
        }

        // Test handlers
        public class TestStreamHandler : IStreamHandler<TestStreamRequest, int>, IStreamHandler<IStreamRequest<int>, int>
        {
            public async IAsyncEnumerable<int> HandleAsync(
                TestStreamRequest request,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (int i = 0; i < request.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1, cancellationToken);
                    yield return i;
                }
            }

            // Explicit interface implementation for base interface
            async IAsyncEnumerable<int> IStreamHandler<IStreamRequest<int>, int>.HandleAsync(
                IStreamRequest<int> request,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (var item in HandleAsync((TestStreamRequest)request, cancellationToken))
                {
                    yield return item;
                }
            }
        }

        private IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            var testHandler = new TestStreamHandler();

            services.AddSingleton<TestStreamHandler>(testHandler);
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => testHandler);

            return services.BuildServiceProvider();
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => new BackpressureStreamDispatcher(null!);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(act);
            Assert.Equal("serviceProvider", ex.ParamName);
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider, 5, 50);

            // Assert
            Assert.NotNull(dispatcher);
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithDefaultParameters_ShouldSucceed()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);

            // Assert
            Assert.NotNull(dispatcher);
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithNegativeMaxConcurrency_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            Action act = () => new BackpressureStreamDispatcher(serviceProvider, -1, 100);

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("maxConcurrency", ex.ParamName);
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithZeroMaxConcurrency_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            Action act = () => new BackpressureStreamDispatcher(serviceProvider, 0, 100);

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("maxConcurrency", ex.ParamName);
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithNegativeBufferSize_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            Action act = () => new BackpressureStreamDispatcher(serviceProvider, 10, -1);

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("bufferSize", ex.ParamName);
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithZeroBufferSize_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            Action act = () => new BackpressureStreamDispatcher(serviceProvider, 10, 0);

            // Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
            Assert.Equal("bufferSize", ex.ParamName);
        }
    }
}
