using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Pipeline;
using Relay.Core.Pipeline.Behaviors;
using Relay.Core.Pipeline.Extensions;
using Relay.Core.Pipeline.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    public class ExceptionHandlingTests
    {
        #region Test Models

        public record TestRequest(string Message) : IRequest<TestResponse>;
        public record TestResponse(string Result);

        public class TestException : Exception
        {
            public TestException(string message) : base(message) { }
        }

        public class SpecificException : TestException
        {
            public SpecificException(string message) : base(message) { }
        }

        public class ThrowingHandler : IRequestHandler<TestRequest, TestResponse>
        {
            public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            {
                throw new TestException($"Handler failed: {request.Message}");
            }
        }

        public class SpecificExceptionThrowingHandler : IRequestHandler<TestRequest, TestResponse>
        {
            public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            {
                throw new SpecificException($"Specific error: {request.Message}");
            }
        }

        #endregion

        #region Exception Handler Tests

        public class TestExceptionHandler : IRequestExceptionHandler<TestRequest, TestResponse, TestException>
        {
            public static List<string> ExecutionLog { get; } = new();
            public static bool ShouldHandle { get; set; } = true;

            public ValueTask<ExceptionHandlerResult<TestResponse>> HandleAsync(
                TestRequest request,
                TestException exception,
                CancellationToken cancellationToken)
            {
                ExecutionLog.Add($"Handler: {exception.Message}");

                if (ShouldHandle)
                {
                    return new ValueTask<ExceptionHandlerResult<TestResponse>>(
                        ExceptionHandlerResult<TestResponse>.Handle(
                            new TestResponse($"Handled: {request.Message}")));
                }

                return new ValueTask<ExceptionHandlerResult<TestResponse>>(
                    ExceptionHandlerResult<TestResponse>.Unhandled());
            }
        }

        public class AnotherExceptionHandler : IRequestExceptionHandler<TestRequest, TestResponse, TestException>
        {
            public ValueTask<ExceptionHandlerResult<TestResponse>> HandleAsync(
                TestRequest request,
                TestException exception,
                CancellationToken cancellationToken)
            {
                TestExceptionHandler.ExecutionLog.Add($"AnotherHandler: {exception.Message}");

                return new ValueTask<ExceptionHandlerResult<TestResponse>>(
                    ExceptionHandlerResult<TestResponse>.Handle(
                        new TestResponse($"Handled by AnotherHandler: {request.Message}")));
            }
        }

        [Fact]
        public async Task ExceptionHandler_Should_Handle_Exception_And_Return_Response()
        {
            // Arrange
            TestExceptionHandler.ExecutionLog.Clear();
            TestExceptionHandler.ShouldHandle = true;

            var services = new ServiceCollection();
            services.AddTransient<ThrowingHandler>();
            services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestExceptionHandlerBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<ThrowingHandler>();

            var request = new TestRequest("test");

            // Act
            var response = await behavior.HandleAsync(
                request,
                async () => await handler.HandleAsync(request, default),
                default);

            // Assert
            Assert.Equal("Handled: test", response.Result);
            Assert.Single(TestExceptionHandler.ExecutionLog);
            Assert.Contains("Handler failed: test", TestExceptionHandler.ExecutionLog[0]);
        }

        [Fact]
        public async Task ExceptionHandler_Should_Let_Exception_Propagate_When_Unhandled()
        {
            // Arrange
            TestExceptionHandler.ExecutionLog.Clear();
            TestExceptionHandler.ShouldHandle = false;

            var services = new ServiceCollection();
            services.AddTransient<ThrowingHandler>();
            services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestExceptionHandlerBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<ThrowingHandler>();

            var request = new TestRequest("test");

            // Act & Assert
            await Assert.ThrowsAsync<TestException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    async () => await handler.HandleAsync(request, default),
                    default);
            });

            Assert.Single(TestExceptionHandler.ExecutionLog);
        }

        [Fact]
        public async Task Multiple_ExceptionHandlers_Should_Execute_Until_One_Handles()
        {
            // Arrange
            TestExceptionHandler.ExecutionLog.Clear();
            TestExceptionHandler.ShouldHandle = false; // First handler won't handle

            var services = new ServiceCollection();
            services.AddTransient<ThrowingHandler>();
            services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>();
            services.AddExceptionHandler<TestRequest, TestResponse, TestException, AnotherExceptionHandler>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestExceptionHandlerBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<ThrowingHandler>();

            var request = new TestRequest("test");

            // Act
            var response = await behavior.HandleAsync(
                request,
                async () => await handler.HandleAsync(request, default),
                default);

            // Assert
            Assert.Equal("Handled by AnotherHandler: test", response.Result);
            Assert.Equal(2, TestExceptionHandler.ExecutionLog.Count);
        }

        [Fact]
        public async Task ExceptionHandler_Should_Handle_Derived_Exception_Types()
        {
            // Arrange
            TestExceptionHandler.ExecutionLog.Clear();
            TestExceptionHandler.ShouldHandle = true;

            var services = new ServiceCollection();
            services.AddTransient<SpecificExceptionThrowingHandler>();
            services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestExceptionHandlerBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<SpecificExceptionThrowingHandler>();

            var request = new TestRequest("test");

            // Act
            var response = await behavior.HandleAsync(
                request,
                async () => await handler.HandleAsync(request, default),
                default);

            // Assert
            Assert.Equal("Handled: test", response.Result);
            Assert.Single(TestExceptionHandler.ExecutionLog);
        }

        #endregion

        #region Exception Action Tests

        public class TestExceptionAction : IRequestExceptionAction<TestRequest, TestException>
        {
            public static List<string> ExecutionLog { get; } = new();

            public ValueTask ExecuteAsync(TestRequest request, TestException exception, CancellationToken cancellationToken)
            {
                ExecutionLog.Add($"Action: {exception.Message}");
                return default;
            }
        }

        public class AnotherExceptionAction : IRequestExceptionAction<TestRequest, TestException>
        {
            public ValueTask ExecuteAsync(TestRequest request, TestException exception, CancellationToken cancellationToken)
            {
                TestExceptionAction.ExecutionLog.Add($"AnotherAction: {exception.Message}");
                return default;
            }
        }

        [Fact]
        public async Task ExceptionAction_Should_Execute_But_Not_Suppress_Exception()
        {
            // Arrange
            TestExceptionAction.ExecutionLog.Clear();

            var services = new ServiceCollection();
            services.AddTransient<ThrowingHandler>();
            services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestExceptionActionBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<ThrowingHandler>();

            var request = new TestRequest("test");

            // Act & Assert
            await Assert.ThrowsAsync<TestException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    async () => await handler.HandleAsync(request, default),
                    default);
            });

            Assert.Single(TestExceptionAction.ExecutionLog);
            Assert.Contains("Handler failed: test", TestExceptionAction.ExecutionLog[0]);
        }

        [Fact]
        public async Task Multiple_ExceptionActions_Should_All_Execute()
        {
            // Arrange
            TestExceptionAction.ExecutionLog.Clear();

            var services = new ServiceCollection();
            services.AddTransient<ThrowingHandler>();
            services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>();
            services.AddExceptionAction<TestRequest, TestException, AnotherExceptionAction>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestExceptionActionBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<ThrowingHandler>();

            var request = new TestRequest("test");

            // Act & Assert
            await Assert.ThrowsAsync<TestException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    async () => await handler.HandleAsync(request, default),
                    default);
            });

            Assert.Equal(2, TestExceptionAction.ExecutionLog.Count);
            Assert.Contains("Action: Handler failed", TestExceptionAction.ExecutionLog[0]);
            Assert.Contains("AnotherAction: Handler failed", TestExceptionAction.ExecutionLog[1]);
        }

        [Fact]
        public async Task ExceptionAction_Should_Continue_If_One_Action_Throws()
        {
            // Arrange
            TestExceptionAction.ExecutionLog.Clear();

            var services = new ServiceCollection();
            services.AddTransient<ThrowingHandler>();
            services.AddExceptionAction<TestRequest, TestException, ThrowingExceptionAction>();
            services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var behavior = new RequestExceptionActionBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<ThrowingHandler>();

            var request = new TestRequest("test");

            // Act & Assert
            await Assert.ThrowsAsync<TestException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    async () => await handler.HandleAsync(request, default),
                    default);
            });

            // Second action should still execute
            Assert.Single(TestExceptionAction.ExecutionLog);
        }

        public class ThrowingExceptionAction : IRequestExceptionAction<TestRequest, TestException>
        {
            public ValueTask ExecuteAsync(TestRequest request, TestException exception, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("Action failed");
            }
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task ExceptionHandler_And_ExceptionAction_Should_Work_Together()
        {
            // Arrange
            TestExceptionHandler.ExecutionLog.Clear();
            TestExceptionAction.ExecutionLog.Clear();
            TestExceptionHandler.ShouldHandle = true;

            var services = new ServiceCollection();
            services.AddTransient<ThrowingHandler>();
            services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>();
            services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var actionBehavior = new RequestExceptionActionBehavior<TestRequest, TestResponse>(provider);
            var handlerBehavior = new RequestExceptionHandlerBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<ThrowingHandler>();

            var request = new TestRequest("integration");

            // Act
            var response = await handlerBehavior.HandleAsync(
                request,
                async () => await actionBehavior.HandleAsync(
                    request,
                    async () => await handler.HandleAsync(request, default),
                    default),
                default);

            // Assert
            Assert.Equal("Handled: integration", response.Result);
            Assert.Single(TestExceptionHandler.ExecutionLog);
            // Action executes BEFORE handler (inner pipeline first), so it DOES run
            Assert.Single(TestExceptionAction.ExecutionLog);
        }

        [Fact]
        public async Task ExceptionAction_Then_ExceptionHandler_Order()
        {
            // Arrange
            TestExceptionHandler.ExecutionLog.Clear();
            TestExceptionAction.ExecutionLog.Clear();
            TestExceptionHandler.ShouldHandle = false; // Handler won't handle

            var services = new ServiceCollection();
            services.AddTransient<ThrowingHandler>();
            services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>();
            services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>();
            services.AddRelayExceptionHandlers();

            var provider = services.BuildServiceProvider();
            var actionBehavior = new RequestExceptionActionBehavior<TestRequest, TestResponse>(provider);
            var handlerBehavior = new RequestExceptionHandlerBehavior<TestRequest, TestResponse>(provider);
            var handler = provider.GetRequiredService<ThrowingHandler>();

            var request = new TestRequest("integration");

            // Act & Assert
            await Assert.ThrowsAsync<TestException>(async () =>
            {
                await handlerBehavior.HandleAsync(
                    request,
                    async () => await actionBehavior.HandleAsync(
                        request,
                        async () => await handler.HandleAsync(request, default),
                        default),
                    default);
            });

            // Both should have executed
            Assert.Single(TestExceptionHandler.ExecutionLog);
            Assert.Single(TestExceptionAction.ExecutionLog);
        }

        #endregion
    }
}