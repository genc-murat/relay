using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Pipeline;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    public class PrePostProcessorTests
    {
        #region Test Models

        public record TestRequest(string Message) : IRequest<TestResponse>;
        public record TestResponse(string Result);

        public class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
        {
            public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            {
                return new ValueTask<TestResponse>(new TestResponse($"Handled: {request.Message}"));
            }
        }

        #endregion

        #region Pre-Processor Tests

        public class TestPreProcessor : IRequestPreProcessor<TestRequest>
        {
            public static List<string> ExecutionLog { get; } = new();

            public ValueTask ProcessAsync(TestRequest request, CancellationToken cancellationToken)
            {
                ExecutionLog.Add($"PreProcessor: {request.Message}");
                return default;
            }
        }

        public class AnotherPreProcessor : IRequestPreProcessor<TestRequest>
        {
            public ValueTask ProcessAsync(TestRequest request, CancellationToken cancellationToken)
            {
                TestPreProcessor.ExecutionLog.Add($"AnotherPreProcessor: {request.Message}");
                return default;
            }
        }

        [Fact]
        public async Task PreProcessor_Should_Execute_Before_Handler()
        {
            // Arrange
            TestPreProcessor.ExecutionLog.Clear();
            var services = new ServiceCollection();
            services.AddTransient<TestRequestHandler>();
            services.AddPreProcessor<TestRequest, TestPreProcessor>();
            services.AddRelayPrePostProcessors();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestPreProcessorBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<TestRequestHandler>();

            var request = new TestRequest("test");
            var handlerCalled = false;

            // Act
            var response = await behavior.HandleAsync(
                request,
                async () =>
                {
                    handlerCalled = true;
                    return await handler.HandleAsync(request, default);
                },
                default);

            // Assert
            handlerCalled.Should().BeTrue();
            TestPreProcessor.ExecutionLog.Should().ContainSingle();
            TestPreProcessor.ExecutionLog[0].Should().Be("PreProcessor: test");
            response.Result.Should().Be("Handled: test");
        }

        [Fact]
        public async Task Multiple_PreProcessors_Should_Execute_In_Order()
        {
            // Arrange
            TestPreProcessor.ExecutionLog.Clear();
            var services = new ServiceCollection();
            services.AddTransient<TestRequestHandler>();
            services.AddPreProcessor<TestRequest, TestPreProcessor>();
            services.AddPreProcessor<TestRequest, AnotherPreProcessor>();
            services.AddRelayPrePostProcessors();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestPreProcessorBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<TestRequestHandler>();

            var request = new TestRequest("test");

            // Act
            await behavior.HandleAsync(
                request,
                async () => await handler.HandleAsync(request, default),
                default);

            // Assert
            TestPreProcessor.ExecutionLog.Should().HaveCount(2);
            TestPreProcessor.ExecutionLog[0].Should().Be("PreProcessor: test");
            TestPreProcessor.ExecutionLog[1].Should().Be("AnotherPreProcessor: test");
        }

        [Fact]
        public async Task PreProcessor_Exception_Should_Stop_Pipeline()
        {
            // Arrange
            TestPreProcessor.ExecutionLog.Clear();
            var services = new ServiceCollection();
            services.AddTransient<TestRequestHandler>();
            services.AddPreProcessor<TestRequest, ThrowingPreProcessor>();
            services.AddRelayPrePostProcessors();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestPreProcessorBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<TestRequestHandler>();

            var request = new TestRequest("test");
            var handlerCalled = false;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    async () =>
                    {
                        handlerCalled = true;
                        return await handler.HandleAsync(request, default);
                    },
                    default);
            });

            handlerCalled.Should().BeFalse();
        }

        public class ThrowingPreProcessor : IRequestPreProcessor<TestRequest>
        {
            public ValueTask ProcessAsync(TestRequest request, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("PreProcessor failed");
            }
        }

        #endregion

        #region Post-Processor Tests

        public class TestPostProcessor : IRequestPostProcessor<TestRequest, TestResponse>
        {
            public static List<string> ExecutionLog { get; } = new();

            public ValueTask ProcessAsync(TestRequest request, TestResponse response, CancellationToken cancellationToken)
            {
                ExecutionLog.Add($"PostProcessor - Request: {request.Message}, Response: {response.Result}");
                return default;
            }
        }

        public class AnotherPostProcessor : IRequestPostProcessor<TestRequest, TestResponse>
        {
            public ValueTask ProcessAsync(TestRequest request, TestResponse response, CancellationToken cancellationToken)
            {
                TestPostProcessor.ExecutionLog.Add($"AnotherPostProcessor - Request: {request.Message}, Response: {response.Result}");
                return default;
            }
        }

        [Fact]
        public async Task PostProcessor_Should_Execute_After_Handler()
        {
            // Arrange
            TestPostProcessor.ExecutionLog.Clear();
            var services = new ServiceCollection();
            services.AddTransient<TestRequestHandler>();
            services.AddPostProcessor<TestRequest, TestResponse, TestPostProcessor>();
            services.AddRelayPrePostProcessors();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestPostProcessorBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<TestRequestHandler>();

            var request = new TestRequest("test");

            // Act
            var response = await behavior.HandleAsync(
                request,
                async () => await handler.HandleAsync(request, default),
                default);

            // Assert
            response.Result.Should().Be("Handled: test");
            TestPostProcessor.ExecutionLog.Should().ContainSingle();
            TestPostProcessor.ExecutionLog[0].Should().Be("PostProcessor - Request: test, Response: Handled: test");
        }

        [Fact]
        public async Task Multiple_PostProcessors_Should_Execute_In_Order()
        {
            // Arrange
            TestPostProcessor.ExecutionLog.Clear();
            var services = new ServiceCollection();
            services.AddTransient<TestRequestHandler>();
            services.AddPostProcessor<TestRequest, TestResponse, TestPostProcessor>();
            services.AddPostProcessor<TestRequest, TestResponse, AnotherPostProcessor>();
            services.AddRelayPrePostProcessors();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestPostProcessorBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<TestRequestHandler>();

            var request = new TestRequest("test");

            // Act
            await behavior.HandleAsync(
                request,
                async () => await handler.HandleAsync(request, default),
                default);

            // Assert
            TestPostProcessor.ExecutionLog.Should().HaveCount(2);
            TestPostProcessor.ExecutionLog[0].Should().Be("PostProcessor - Request: test, Response: Handled: test");
            TestPostProcessor.ExecutionLog[1].Should().Be("AnotherPostProcessor - Request: test, Response: Handled: test");
        }

        [Fact]
        public async Task PostProcessor_Should_Not_Execute_If_Handler_Throws()
        {
            // Arrange
            TestPostProcessor.ExecutionLog.Clear();
            var services = new ServiceCollection();
            services.AddPostProcessor<TestRequest, TestResponse, TestPostProcessor>();
            services.AddRelayPrePostProcessors();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestPostProcessorBehavior<TestRequest, TestResponse>(provider);

            var request = new TestRequest("test");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    () => throw new InvalidOperationException("Handler failed"),
                    default);
            });

            TestPostProcessor.ExecutionLog.Should().BeEmpty();
        }

        [Fact]
        public async Task PostProcessor_Exception_Should_Propagate()
        {
            // Arrange
            TestPostProcessor.ExecutionLog.Clear();
            var services = new ServiceCollection();
            services.AddTransient<TestRequestHandler>();
            services.AddPostProcessor<TestRequest, TestResponse, ThrowingPostProcessor>();
            services.AddRelayPrePostProcessors();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestPostProcessorBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<TestRequestHandler>();

            var request = new TestRequest("test");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    async () => await handler.HandleAsync(request, default),
                    default);
            });
        }

        public class ThrowingPostProcessor : IRequestPostProcessor<TestRequest, TestResponse>
        {
            public ValueTask ProcessAsync(TestRequest request, TestResponse response, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("PostProcessor failed");
            }
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task PreProcessor_And_PostProcessor_Should_Work_Together()
        {
            // Arrange
            TestPreProcessor.ExecutionLog.Clear();
            TestPostProcessor.ExecutionLog.Clear();

            var services = new ServiceCollection();
            services.AddTransient<TestRequestHandler>();
            services.AddPreProcessor<TestRequest, TestPreProcessor>();
            services.AddPostProcessor<TestRequest, TestResponse, TestPostProcessor>();
            services.AddRelayPrePostProcessors();

            var provider = services.BuildServiceProvider();
            var preProcessorBehavior = new RequestPreProcessorBehavior<TestRequest, TestResponse>(provider);
            var postProcessorBehavior = new RequestPostProcessorBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<TestRequestHandler>();

            var request = new TestRequest("integration");

            // Act
            var response = await preProcessorBehavior.HandleAsync(
                request,
                async () => await postProcessorBehavior.HandleAsync(
                    request,
                    async () => await handler.HandleAsync(request, default),
                    default),
                default);

            // Assert
            response.Result.Should().Be("Handled: integration");
            TestPreProcessor.ExecutionLog.Should().ContainSingle();
            TestPreProcessor.ExecutionLog[0].Should().Be("PreProcessor: integration");
            TestPostProcessor.ExecutionLog.Should().ContainSingle();
            TestPostProcessor.ExecutionLog[0].Should().Be("PostProcessor - Request: integration, Response: Handled: integration");
        }

        #endregion
    }
}
