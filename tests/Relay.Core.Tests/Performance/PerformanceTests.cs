using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Fallback;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests
{
    public class PerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task FallbackDispatcher_Performance_Baseline()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddSingleton<IRequestDispatcher, FallbackRequestDispatcher>();

            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = serviceProvider.GetRequiredService<IRequestDispatcher>();
            var request = new TestRequest { Message = "Test" };

            const int iterations = 1000;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var result = await dispatcher.DispatchAsync(request, CancellationToken.None);
                Assert.NotNull(result);
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"FallbackDispatcher: {iterations} iterations in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / iterations:F3}ms per request");

            // Should complete within reasonable time (this is a baseline, not a strict requirement)
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Performance test took too long");
        }

        [Fact]
        public async Task DirectHandler_Performance_Comparison()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<TestRequestHandler>();

            var serviceProvider = services.BuildServiceProvider();
            var handler = serviceProvider.GetRequiredService<TestRequestHandler>();
            var request = new TestRequest { Message = "Test" };

            const int iterations = 1000;
            var stopwatch = Stopwatch.StartNew();

            // Act - Direct handler call (what generated code should approximate)
            for (int i = 0; i < iterations; i++)
            {
                var result = await handler.HandleAsync(request, CancellationToken.None);
                Assert.NotNull(result);
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Direct Handler: {iterations} iterations in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / iterations:F3}ms per request");

            // Direct calls should be very fast
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Direct handler calls took too long");
        }

        [Fact]
        public async Task NotificationDispatcher_Performance_MultipleHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<INotificationHandler<TestNotification>, TestNotificationHandler1>();
            services.AddSingleton<INotificationHandler<TestNotification>, TestNotificationHandler2>();
            services.AddSingleton<INotificationHandler<TestNotification>, TestNotificationHandler3>();
            services.AddSingleton<INotificationDispatcher, FallbackNotificationDispatcher>();

            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = serviceProvider.GetRequiredService<INotificationDispatcher>();
            var notification = new TestNotification { Message = "Test" };

            const int iterations = 100; // Fewer iterations for notification tests
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                await dispatcher.DispatchAsync(notification, CancellationToken.None);
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"NotificationDispatcher (3 handlers): {iterations} iterations in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / iterations:F3}ms per notification");

            // Should complete within reasonable time
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Notification performance test took too long");
        }

        [Fact]
        public async Task StreamDispatcher_Performance_LargeStream()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IStreamHandler<TestStreamRequest, string>, TestStreamHandler>();
            services.AddSingleton<IStreamDispatcher, FallbackStreamDispatcher>();

            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = serviceProvider.GetRequiredService<IStreamDispatcher>();
            var request = new TestStreamRequest { Count = 1000 };

            var stopwatch = Stopwatch.StartNew();
            var itemCount = 0;

            // Act
            await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
            {
                itemCount++;
                Assert.NotNull(item);
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"StreamDispatcher: {itemCount} items in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / itemCount:F3}ms per item");

            Assert.Equal(1000, itemCount);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Stream performance test took too long");
        }

        // Test classes and handlers
        private class TestRequest : IRequest<string>
        {
            public string Message { get; set; } = string.Empty;
        }

        private class TestRequestHandler : IRequestHandler<TestRequest, string>
        {
            public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult($"Handled: {request.Message}");
            }
        }

        private class TestNotification : INotification
        {
            public string Message { get; set; } = string.Empty;
        }

        private class TestNotificationHandler1 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                // Simulate some work
                return ValueTask.CompletedTask;
            }
        }

        private class TestNotificationHandler2 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                // Simulate some work
                return ValueTask.CompletedTask;
            }
        }

        private class TestNotificationHandler3 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                // Simulate some work
                return ValueTask.CompletedTask;
            }
        }

        private class TestStreamRequest : IStreamRequest<string>
        {
            public int Count { get; set; }
        }

        private class TestStreamHandler : IStreamHandler<TestStreamRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(TestStreamRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (int i = 0; i < request.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return $"Item {i}";

                    // Simulate async work occasionally
                    if (i % 100 == 0)
                    {
                        await Task.Yield();
                    }
                }
            }
        }
    }
}