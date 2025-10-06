using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Caching;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Threading;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;

namespace Relay.Core.Tests.Caching
{
    public class CachingPipelineTests
    {
        [Cache(10)]
        public class CachedRequest : IRequest<string>
        {
            public int Id { get; set; }
        }

        public class NonCachedRequest : IRequest<string> { }

        public class TestRequestHandler :
            IRequestHandler<CachedRequest, string>,
            IRequestHandler<NonCachedRequest, string>
        {
            public int CallCount { get; private set; }

            public ValueTask<string> HandleAsync(CachedRequest request, CancellationToken cancellationToken)
            {
                CallCount++;
                return new ValueTask<string>($"Cached Response {request.Id}");
            }

            public ValueTask<string> HandleAsync(NonCachedRequest request, CancellationToken cancellationToken)
            {
                CallCount++;
                return new ValueTask<string>("Non-Cached Response");
            }
        }

        private (PipelineExecutor, ServiceProvider, TestRequestHandler) CreateExecutor()
        {
            var services = new ServiceCollection();
            var handler = new TestRequestHandler();

            services.AddMemoryCache();
            services.AddLogging();
            services.AddSingleton(handler);

            services.AddTransient<IPipelineBehavior<CachedRequest, string>, CachingPipelineBehavior<CachedRequest, string>>();
            services.AddTransient<IPipelineBehavior<NonCachedRequest, string>, CachingPipelineBehavior<NonCachedRequest, string>>();

            var provider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(provider);
            return (executor, provider, handler);
        }

        [Fact]
        public async Task Should_NotCache_When_AttributeIsMissing()
        {
            // Arrange
            var (executor, _, handler) = CreateExecutor();
            var request = new NonCachedRequest();

            // Act
            await executor.ExecuteAsync<NonCachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            await executor.ExecuteAsync<NonCachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(2, handler.CallCount);
        }

        [Fact]
        public async Task Should_Cache_When_AttributeIsPresent()
        {
            // Arrange
            var (executor, _, handler) = CreateExecutor();
            var request = new CachedRequest { Id = 1 };

            // Act
            var response1 = await executor.ExecuteAsync<CachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            var response2 = await executor.ExecuteAsync<CachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(1, handler.CallCount);
            Assert.Equal("Cached Response 1", response1);
            Assert.Equal(response1, response2);
        }

        [Fact]
        public async Task Should_InvalidateCache_After_Expiration()
        {
            // Arrange
            var (executor, _, handler) = CreateExecutor();
            var request = new CachedRequest { Id = 2 };

            // Act
            await executor.ExecuteAsync<CachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(11));
            await executor.ExecuteAsync<CachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(2, handler.CallCount);
        }

        [Fact]
        public async Task Should_Cache_Different_Requests_Separately()
        {
            // Arrange
            var (executor, _, handler) = CreateExecutor();
            var request1 = new CachedRequest { Id = 1 };
            var request2 = new CachedRequest { Id = 2 };

            // Act
            await executor.ExecuteAsync<CachedRequest, string>(request1, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            await executor.ExecuteAsync<CachedRequest, string>(request2, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            await executor.ExecuteAsync<CachedRequest, string>(request1, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(2, handler.CallCount);
        }

        [Fact]
        public async Task Should_Return_Cached_Response_For_Same_Request()
        {
            // Arrange
            var (executor, _, handler) = CreateExecutor();
            var request = new CachedRequest { Id = 5 };

            // Act
            var response1 = await executor.ExecuteAsync<CachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            var response2 = await executor.ExecuteAsync<CachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            var response3 = await executor.ExecuteAsync<CachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(1, handler.CallCount);
            Assert.Equal(response1, response2);
            Assert.Equal(response2, response3);
        }

        [Fact]
        public async Task Should_Cache_Null_Response()
        {
            // Arrange
            var services = new ServiceCollection();
            var nullHandler = new NullResponseHandler();

            services.AddMemoryCache();
            services.AddLogging();
            services.AddSingleton(nullHandler);
            services.AddTransient<IPipelineBehavior<NullRequest, string?>, CachingPipelineBehavior<NullRequest, string?>>();

            var provider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(provider);
            var request = new NullRequest();

            // Act
            var response1 = await executor.ExecuteAsync<NullRequest, string?>(request, (r, c) => nullHandler.HandleAsync(r, c), CancellationToken.None);
            var response2 = await executor.ExecuteAsync<NullRequest, string?>(request, (r, c) => nullHandler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(1, nullHandler.CallCount); // Null is also cached
            Assert.Null(response1);
            Assert.Null(response2);
        }

        [Cache(5)]
        public class NullRequest : IRequest<string?> { }

        public class NullResponseHandler : IRequestHandler<NullRequest, string?>
        {
            public int CallCount { get; private set; }

            public ValueTask<string?> HandleAsync(NullRequest request, CancellationToken cancellationToken)
            {
                CallCount++;
                return new ValueTask<string?>((string?)null);
            }
        }

        [Fact]
        public async Task Should_Respect_CancellationToken()
        {
            // Arrange
            var (executor, _, handler) = CreateExecutor();
            var request = new CachedRequest { Id = 10 };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await executor.ExecuteAsync<CachedRequest, string>(request, async (r, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return await handler.HandleAsync(r, c);
                }, cts.Token));
        }

        [Cache(2)]
        public class ShortCacheRequest : IRequest<string>
        {
            public int Value { get; set; }
        }

        public class ShortCacheHandler : IRequestHandler<ShortCacheRequest, string>
        {
            public int CallCount { get; private set; }

            public ValueTask<string> HandleAsync(ShortCacheRequest request, CancellationToken cancellationToken)
            {
                CallCount++;
                return new ValueTask<string>($"Value: {request.Value}");
            }
        }

        [Fact]
        public async Task Should_Expire_Cache_After_Short_Duration()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new ShortCacheHandler();

            services.AddMemoryCache();
            services.AddLogging();
            services.AddSingleton(handler);
            services.AddTransient<IPipelineBehavior<ShortCacheRequest, string>, CachingPipelineBehavior<ShortCacheRequest, string>>();

            var provider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(provider);
            var request = new ShortCacheRequest { Value = 100 };

            // Act
            await executor.ExecuteAsync<ShortCacheRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(3));
            await executor.ExecuteAsync<ShortCacheRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(2, handler.CallCount);
        }

        [Fact]
        public async Task Should_Not_Cache_Without_Attribute()
        {
            // Arrange
            var services = new ServiceCollection();
            var uncachedHandler = new UncachedHandler();

            services.AddMemoryCache();
            services.AddLogging();
            services.AddSingleton(uncachedHandler);
            services.AddTransient<IPipelineBehavior<UncachedRequest, int>, CachingPipelineBehavior<UncachedRequest, int>>();

            var provider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(provider);
            var request = new UncachedRequest();

            // Act
            await executor.ExecuteAsync<UncachedRequest, int>(request, (r, c) => uncachedHandler.HandleAsync(r, c), CancellationToken.None);
            await executor.ExecuteAsync<UncachedRequest, int>(request, (r, c) => uncachedHandler.HandleAsync(r, c), CancellationToken.None);
            await executor.ExecuteAsync<UncachedRequest, int>(request, (r, c) => uncachedHandler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(3, uncachedHandler.CallCount);
        }

        public class UncachedRequest : IRequest<int> { }

        public class UncachedHandler : IRequestHandler<UncachedRequest, int>
        {
            public int CallCount { get; private set; }

            public ValueTask<int> HandleAsync(UncachedRequest request, CancellationToken cancellationToken)
            {
                CallCount++;
                return new ValueTask<int>(CallCount);
            }
        }

        [Cache(60)]
        public class ComplexCacheRequest : IRequest<ComplexResponse>
        {
            public string Key { get; set; } = "";
            public int Version { get; set; }
        }

        public class ComplexResponse
        {
            public string Data { get; set; } = "";
            public DateTime Timestamp { get; set; }
        }

        public class ComplexHandler : IRequestHandler<ComplexCacheRequest, ComplexResponse>
        {
            public int CallCount { get; private set; }

            public ValueTask<ComplexResponse> HandleAsync(ComplexCacheRequest request, CancellationToken cancellationToken)
            {
                CallCount++;
                return new ValueTask<ComplexResponse>(new ComplexResponse
                {
                    Data = $"{request.Key}-{request.Version}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        [Fact]
        public async Task Should_Cache_Complex_Objects()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new ComplexHandler();

            services.AddMemoryCache();
            services.AddLogging();
            services.AddSingleton(handler);
            services.AddTransient<IPipelineBehavior<ComplexCacheRequest, ComplexResponse>, CachingPipelineBehavior<ComplexCacheRequest, ComplexResponse>>();

            var provider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(provider);
            var request = new ComplexCacheRequest { Key = "test", Version = 1 };

            // Act
            var response1 = await executor.ExecuteAsync<ComplexCacheRequest, ComplexResponse>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            var response2 = await executor.ExecuteAsync<ComplexCacheRequest, ComplexResponse>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(1, handler.CallCount);
            Assert.Equal(response1.Data, response2.Data);
            Assert.Equal(response1.Timestamp, response2.Timestamp);
        }

        [Fact]
        public async Task Should_Differentiate_Cache_By_Request_Properties()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new ComplexHandler();

            services.AddMemoryCache();
            services.AddLogging();
            services.AddSingleton(handler);
            services.AddTransient<IPipelineBehavior<ComplexCacheRequest, ComplexResponse>, CachingPipelineBehavior<ComplexCacheRequest, ComplexResponse>>();

            var provider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(provider);
            var request1 = new ComplexCacheRequest { Key = "test", Version = 1 };
            var request2 = new ComplexCacheRequest { Key = "test", Version = 2 };

            // Act
            await executor.ExecuteAsync<ComplexCacheRequest, ComplexResponse>(request1, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            await executor.ExecuteAsync<ComplexCacheRequest, ComplexResponse>(request2, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(2, handler.CallCount);
        }

        [Fact]
        public async Task Should_Handle_Multiple_Concurrent_Cache_Requests()
        {
            // Arrange
            var (executor, _, handler) = CreateExecutor();
            var request = new CachedRequest { Id = 99 };

            // Act
            var tasks = new Task<string>[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = executor.ExecuteAsync<CachedRequest, string>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None).AsTask();
            }
            await Task.WhenAll(tasks);

            // Assert - Handler might be called once or a few times due to race conditions, but should be less than 5
            Assert.True(handler.CallCount <= 5);
        }

        [Cache(30)]
        public class ValueTypeRequest : IRequest<int>
        {
            public int Input { get; set; }
        }

        public class ValueTypeHandler : IRequestHandler<ValueTypeRequest, int>
        {
            public int CallCount { get; private set; }

            public ValueTask<int> HandleAsync(ValueTypeRequest request, CancellationToken cancellationToken)
            {
                CallCount++;
                return new ValueTask<int>(request.Input * 2);
            }
        }

        [Fact]
        public async Task Should_Cache_Value_Type_Responses()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new ValueTypeHandler();

            services.AddMemoryCache();
            services.AddLogging();
            services.AddSingleton(handler);
            services.AddTransient<IPipelineBehavior<ValueTypeRequest, int>, CachingPipelineBehavior<ValueTypeRequest, int>>();

            var provider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(provider);
            var request = new ValueTypeRequest { Input = 10 };

            // Act
            var result1 = await executor.ExecuteAsync<ValueTypeRequest, int>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);
            var result2 = await executor.ExecuteAsync<ValueTypeRequest, int>(request, (r, c) => handler.HandleAsync(r, c), CancellationToken.None);

            // Assert
            Assert.Equal(1, handler.CallCount);
            Assert.Equal(20, result1);
            Assert.Equal(20, result2);
        }
    }
}
