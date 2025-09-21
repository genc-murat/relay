using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Caching;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Threading;

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
    }
}
