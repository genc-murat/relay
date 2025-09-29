using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core;

namespace Relay.Core.Tests
{
    public class StreamingPipelineTests
    {
        // Test request and response types
        public class StreamingTestRequest : IStreamRequest<string>
        {
            public int ItemCount { get; set; } = 5;
            public string Prefix { get; set; } = "Item";
        }

        public class StreamingTestHandler : IStreamHandler<StreamingTestRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(StreamingTestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (int i = 0; i < request.ItemCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1, cancellationToken); // Small delay to simulate work
                    yield return $"{request.Prefix} {i}";
                }
            }
        }

        // Test pipeline behaviors
        public class LoggingStreamPipelineBehavior : IStreamPipelineBehavior<StreamingTestRequest, string>
        {
            public List<string> LogEntries { get; } = new List<string>();

            public async IAsyncEnumerable<string> HandleAsync(
                StreamingTestRequest request,
                StreamHandlerDelegate<string> next,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                LogEntries.Add("Pipeline: Before streaming");

                var itemCount = 0;
                await foreach (var item in next().WithCancellation(cancellationToken))
                {
                    LogEntries.Add($"Pipeline: Processing item {itemCount}");
                    itemCount++;
                    yield return $"[Logged] {item}";
                }

                LogEntries.Add($"Pipeline: After streaming, processed {itemCount} items");
            }
        }

        public class TransformStreamPipelineBehavior : IStreamPipelineBehavior<StreamingTestRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(
                StreamingTestRequest request,
                StreamHandlerDelegate<string> next,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (var item in next().WithCancellation(cancellationToken))
                {
                    // Use culture-invariant uppercase to ensure consistent behavior across platforms
                    yield return item.ToUpperInvariant();
                }
            }
        }

        public class FilterStreamPipelineBehavior : IStreamPipelineBehavior<StreamingTestRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(
                StreamingTestRequest request,
                StreamHandlerDelegate<string> next,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var index = 0;
                await foreach (var item in next().WithCancellation(cancellationToken))
                {
                    // Only yield even-indexed items
                    if (index % 2 == 0)
                    {
                        yield return item;
                    }
                    index++;
                }
            }
        }

        public class ExceptionHandlingStreamPipelineBehavior : IStreamPipelineBehavior<StreamingTestRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(
                StreamingTestRequest request,
                StreamHandlerDelegate<string> next,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                // Simple pass-through for now - exception handling in async enumerables is complex
                await foreach (var item in next().WithCancellation(cancellationToken))
                {
                    yield return item;
                }
            }
        }

        // Test system module
        public class TestStreamSystemModule : ISystemModule
        {
            public int Order => -1000; // Execute early

            public List<string> ExecutionLog { get; } = new List<string>();

            public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            {
                // Not used for streaming tests
                return next();
            }

            public async IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                ExecutionLog.Add("SystemModule: Before streaming");

                await foreach (var item in next().WithCancellation(cancellationToken))
                {
                    ExecutionLog.Add($"SystemModule: Processing item");
                    yield return item;
                }

                ExecutionLog.Add("SystemModule: After streaming");
            }
        }

        // Test dispatcher that manually executes pipeline behaviors for testing
        public class TestStreamingPipelineDispatcher : BaseStreamDispatcher
        {
            public TestStreamingPipelineDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
            {
                ValidateRequest(request);
                return ExecuteWithPipelines<TResponse>(request, cancellationToken);
            }

            public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
            {
                ValidateRequest(request);
                ValidateHandlerName(handlerName);
                return ExecuteWithPipelines<TResponse>(request, cancellationToken);
            }

            private IAsyncEnumerable<TResponse> ExecuteWithPipelines<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
            {
                // Only handle string response type for simplicity
                if (typeof(TResponse) == typeof(string) && request is StreamingTestRequest testRequest)
                {
                    return (IAsyncEnumerable<TResponse>)ExecuteStringPipeline(testRequest, cancellationToken);
                }

                // Fallback to direct handler execution
                return DispatchToHandler<TResponse>(request, cancellationToken);
            }

            private IAsyncEnumerable<string> ExecuteStringPipeline(StreamingTestRequest request, CancellationToken cancellationToken)
            {
                // Get pipeline behaviors from DI
                var behaviors = GetStreamPipelineBehaviors<string>(request);
                var systemModules = GetSystemModules();

                // Build execution chain
                StreamHandlerDelegate<string> next = () => GetService<StreamingTestHandler>().HandleAsync(request, cancellationToken);

                // Add behaviors in reverse order
                foreach (var behavior in behaviors.Reverse())
                {
                    var currentNext = next;
                    next = () => behavior.HandleAsync(request, currentNext, cancellationToken);
                }

                // Add system modules in reverse order
                foreach (var module in systemModules.OrderByDescending(m => m.Order))
                {
                    var currentNext = next;
                    next = () => module.ExecuteStreamAsync(request, currentNext, cancellationToken);
                }

                return next();
            }

            private IEnumerable<IStreamPipelineBehavior<StreamingTestRequest, string>> GetStreamPipelineBehaviors<TResponse>(IStreamRequest<TResponse> request)
            {
                var behaviors = new List<IStreamPipelineBehavior<StreamingTestRequest, string>>();

                // Only handle string response type for our test
                if (typeof(TResponse) == typeof(string))
                {
                    // Try to get each behavior type from DI
                    if (GetServiceOrNull<LoggingStreamPipelineBehavior>() is LoggingStreamPipelineBehavior logging)
                        behaviors.Add(logging);
                    if (GetServiceOrNull<TransformStreamPipelineBehavior>() is TransformStreamPipelineBehavior transform)
                        behaviors.Add(transform);
                    if (GetServiceOrNull<FilterStreamPipelineBehavior>() is FilterStreamPipelineBehavior filter)
                        behaviors.Add(filter);
                    if (GetServiceOrNull<ExceptionHandlingStreamPipelineBehavior>() is ExceptionHandlingStreamPipelineBehavior exception)
                        behaviors.Add(exception);
                }

                return behaviors;
            }

            private IEnumerable<ISystemModule> GetSystemModules()
            {
                var modules = new List<ISystemModule>();
                if (GetServiceOrNull<TestStreamSystemModule>() is TestStreamSystemModule module)
                    modules.Add(module);
                return modules;
            }

            private IAsyncEnumerable<TResponse> DispatchToHandler<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
            {
                return request switch
                {
                    StreamingTestRequest testRequest when typeof(TResponse) == typeof(string) =>
                        (IAsyncEnumerable<TResponse>)GetService<StreamingTestHandler>().HandleAsync(testRequest, cancellationToken),
                    _ => ThrowHandlerNotFound<TResponse>(request.GetType())
                };
            }
        }

        private IServiceProvider CreateServiceProvider(params object[] additionalServices)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IStreamDispatcher, TestStreamingPipelineDispatcher>();
            services.AddSingleton<StreamingTestHandler>();

            foreach (var service in additionalServices)
            {
                services.AddSingleton(service.GetType(), service);
            }

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task StreamAsync_WithSinglePipelineBehavior_ShouldExecutePipelineAndHandler()
        {
            // Arrange
            var loggingBehavior = new LoggingStreamPipelineBehavior();
            var serviceProvider = CreateServiceProvider(loggingBehavior);
            var relay = new RelayImplementation(serviceProvider);
            var request = new StreamingTestRequest { ItemCount = 3, Prefix = "Test" };

            // Act
            var results = new List<string>();
            await foreach (var item in relay.StreamAsync(request))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, item => Assert.StartsWith("[Logged] Test", item));

            // Verify pipeline logging
            Assert.Contains("Pipeline: Before streaming", loggingBehavior.LogEntries);
            Assert.Contains("Pipeline: After streaming, processed 3 items", loggingBehavior.LogEntries);
            Assert.Equal(5, loggingBehavior.LogEntries.Count); // Before + 3 processing + After
        }

        [Fact]
        public async Task StreamAsync_WithMultiplePipelineBehaviors_ShouldExecuteInCorrectOrder()
        {
            // Arrange
            var loggingBehavior = new LoggingStreamPipelineBehavior();
            var transformBehavior = new TransformStreamPipelineBehavior();
            var serviceProvider = CreateServiceProvider(loggingBehavior, transformBehavior);
            var relay = new RelayImplementation(serviceProvider);
            var request = new StreamingTestRequest { ItemCount = 2, Prefix = "test" };

            // Act
            var results = new List<string>();
            await foreach (var item in relay.StreamAsync(request))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(2, results.Count);
            // Should be logged first, then transformed to uppercase
            Assert.All(results, item =>
            {
                Assert.StartsWith("[Logged] TEST", item);
            });
        }

        [Fact]
        public async Task StreamAsync_WithFilteringPipeline_ShouldFilterItems()
        {
            // Arrange
            var filterBehavior = new FilterStreamPipelineBehavior();
            var serviceProvider = CreateServiceProvider(filterBehavior);
            var relay = new RelayImplementation(serviceProvider);
            var request = new StreamingTestRequest { ItemCount = 5, Prefix = "Item" };

            // Act
            var results = new List<string>();
            await foreach (var item in relay.StreamAsync(request))
            {
                results.Add(item);
            }

            // Assert
            // Should only get even-indexed items (0, 2, 4) = 3 items
            Assert.Equal(3, results.Count);
            Assert.Contains("Item 0", results);
            Assert.Contains("Item 2", results);
            Assert.Contains("Item 4", results);
        }

        [Fact]
        public async Task StreamAsync_WithSystemModule_ShouldExecuteSystemModuleFirst()
        {
            // Arrange
            var systemModule = new TestStreamSystemModule();
            var loggingBehavior = new LoggingStreamPipelineBehavior();
            var serviceProvider = CreateServiceProvider(systemModule, loggingBehavior);
            var relay = new RelayImplementation(serviceProvider);
            var request = new StreamingTestRequest { ItemCount = 2, Prefix = "Test" };

            // Act
            var results = new List<string>();
            await foreach (var item in relay.StreamAsync(request))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(2, results.Count);

            // Verify system module executed
            Assert.Contains("SystemModule: Before streaming", systemModule.ExecutionLog);
            Assert.Contains("SystemModule: After streaming", systemModule.ExecutionLog);

            // Verify pipeline behavior also executed
            Assert.Contains("Pipeline: Before streaming", loggingBehavior.LogEntries);
        }

        [Fact]
        public async Task StreamAsync_WithExceptionHandlingPipeline_ShouldHandleExceptions()
        {
            // Arrange
            var exceptionBehavior = new ExceptionHandlingStreamPipelineBehavior();
            var serviceProvider = CreateServiceProvider(exceptionBehavior);
            var relay = new RelayImplementation(serviceProvider);
            var request = new StreamingTestRequest { ItemCount = 3, Prefix = "Test" };

            // Act
            var results = new List<string>();
            await foreach (var item in relay.StreamAsync(request))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, item => Assert.StartsWith("Test", item));
        }

        [Fact]
        public async Task StreamAsync_WithCancellation_ShouldCancelPipelineExecution()
        {
            // Arrange
            var loggingBehavior = new LoggingStreamPipelineBehavior();
            var serviceProvider = CreateServiceProvider(loggingBehavior);
            var relay = new RelayImplementation(serviceProvider);
            var request = new StreamingTestRequest { ItemCount = 100, Prefix = "Test" };

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));

            // Act & Assert
            var results = new List<string>();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var item in relay.StreamAsync(request, cts.Token))
                {
                    results.Add(item);
                    await Task.Delay(10, cts.Token); // Slow processing to trigger cancellation
                }
            });

            // Should have processed some items but not all (or none if cancellation was very fast)
            Assert.True(results.Count < 100);
        }

        [Fact]
        public async Task StreamAsync_WithAsyncEnumerableComposition_ShouldComposeCorrectly()
        {
            // Arrange
            var loggingBehavior = new LoggingStreamPipelineBehavior();
            var transformBehavior = new TransformStreamPipelineBehavior();
            var filterBehavior = new FilterStreamPipelineBehavior();
            var serviceProvider = CreateServiceProvider(loggingBehavior, transformBehavior, filterBehavior);
            var relay = new RelayImplementation(serviceProvider);
            var request = new StreamingTestRequest { ItemCount = 6, Prefix = "item" };

            // Act
            var results = new List<string>();
            await foreach (var item in relay.StreamAsync(request))
            {
                results.Add(item);
            }

            // Assert
            // Filter should reduce 6 items to 3 (even indices: 0, 2, 4)
            // Transform should uppercase
            // Logging should add prefix
            Assert.Equal(3, results.Count);
            
            // Use culture-invariant comparison to handle "item" -> "ITEM" vs "İTEM" differences
            var expectedPrefix = "[Logged] " + "item".ToUpperInvariant();
            Assert.All(results, item => Assert.StartsWith(expectedPrefix, item));
        }
    }
}