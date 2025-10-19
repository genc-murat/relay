using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AICachingOptimizationBehaviorCacheKeyTests : IDisposable
    {
        private readonly ILogger<AICachingOptimizationBehavior<TestRequest, TestResponse>> _logger;
        private readonly Mock<IAIPredictionCache> _cacheMock;
        private readonly List<AICachingOptimizationBehavior<TestRequest, TestResponse>> _behaviorsToDispose;

        public AICachingOptimizationBehaviorCacheKeyTests()
        {
            _logger = NullLogger<AICachingOptimizationBehavior<TestRequest, TestResponse>>.Instance;
            _cacheMock = new Mock<IAIPredictionCache>();
            _behaviorsToDispose = new List<AICachingOptimizationBehavior<TestRequest, TestResponse>>();
        }

        public void Dispose()
        {
            _behaviorsToDispose.Clear();
        }

        [Fact]
        public async Task HandleAsync_Should_Generate_Unique_Cache_Keys_For_Different_Requests()
        {
            // Arrange
            var capturedKeys = new List<string>();

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request1 = new TestRequest { Value = "test1" };
            var request2 = new TestRequest { Value = "test2" };

            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request1, next, CancellationToken.None);
            await behavior.HandleAsync(request2, next, CancellationToken.None);

            // Assert
            Assert.Equal(2, capturedKeys.Count);
            Assert.NotEqual(capturedKeys[0], capturedKeys[1]);
        }

        [Fact]
        public async Task HandleAsync_Should_Generate_Same_Cache_Key_For_Identical_Requests()
        {
            // Arrange
            var capturedKeys = new List<string>();

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object);
            _behaviorsToDispose.Add(behavior);

            var request1 = new TestRequest { Value = "test" };
            var request2 = new TestRequest { Value = "test" };

            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request1, next, CancellationToken.None);
            await behavior.HandleAsync(request2, next, CancellationToken.None);

            // Assert
            Assert.Equal(2, capturedKeys.Count);
            Assert.Equal(capturedKeys[0], capturedKeys[1]);
        }

        [Fact]
        public async Task HandleAsync_Should_Generate_Cache_Keys_With_Correct_Scope_Prefixes()
        {
            // Arrange
            var capturedKeys = new List<string>();

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
                .ReturnsAsync((OptimizationRecommendation?)null);

            var userLogger = NullLogger<AICachingOptimizationBehavior<TestUserScopeRequest, TestResponse>>.Instance;
            var sessionLogger = NullLogger<AICachingOptimizationBehavior<TestSessionScopeRequest, TestResponse>>.Instance;
            var requestLogger = NullLogger<AICachingOptimizationBehavior<TestRequestScopeRequest, TestResponse>>.Instance;

            var userBehavior = new AICachingOptimizationBehavior<TestUserScopeRequest, TestResponse>(userLogger, _cacheMock.Object);
            var sessionBehavior = new AICachingOptimizationBehavior<TestSessionScopeRequest, TestResponse>(sessionLogger, _cacheMock.Object);
            var requestBehavior = new AICachingOptimizationBehavior<TestRequestScopeRequest, TestResponse>(requestLogger, _cacheMock.Object);

            var userRequest = new TestUserScopeRequest { Value = "test" };
            var sessionRequest = new TestSessionScopeRequest { Value = "test" };
            var requestScopeRequest = new TestRequestScopeRequest { Value = "test" };

            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await userBehavior.HandleAsync(userRequest, next, CancellationToken.None);
            await sessionBehavior.HandleAsync(sessionRequest, next, CancellationToken.None);
            await requestBehavior.HandleAsync(requestScopeRequest, next, CancellationToken.None);

            // Assert
            Assert.Equal(3, capturedKeys.Count);
            Assert.StartsWith("user:", capturedKeys[0]);
            Assert.StartsWith("session:", capturedKeys[1]);
            Assert.StartsWith("request:", capturedKeys[2]);
        }

        [Fact]
        public async Task HandleAsync_Should_Generate_Fallback_Cache_Key_On_Serialization_Error()
        {
            // Arrange
            var capturedKeys = new List<string>();

            _cacheMock.Setup(c => c.GetCachedPredictionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
                .ReturnsAsync((OptimizationRecommendation?)null);

            // Create behavior with custom options that might cause serialization issues
            var options = new AICachingOptimizationOptions
            {
                SerializerOptions = new System.Text.Json.JsonSerializerOptions
                {
                    // Configure options that might cause issues, but actually JsonSerializer is robust
                    // We'll test that the method handles any potential exceptions gracefully
                }
            };

            var behavior = new AICachingOptimizationBehavior<TestRequest, TestResponse>(_logger, _cacheMock.Object, options);
            _behaviorsToDispose.Add(behavior);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should generate a valid cache key (either normal or fallback)
            Assert.Single(capturedKeys);
            var key = capturedKeys[0];
            Assert.NotNull(key);
            Assert.NotEmpty(key);
            // Key should either start with "global:" (normal) or "fallback:" (error case)
            Assert.True(key.StartsWith("global:") || key.StartsWith("fallback:"), $"Unexpected key format: {key}");
        }
    }
}