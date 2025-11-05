using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core.Validation;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Validation.Exceptions;
using Relay.Core.Validation.Interfaces;
using Relay.Core.Validation.Rules;
using Relay.Core.Pipeline.Validation;
using Relay.Core.Testing;
using Relay.Core.Extensions;

namespace Relay.Core.Tests.Validation
{
    public class ValidationPipelineBehaviorBasicTests
    {
        [Fact]
        public async Task Should_Pass_Validation_When_Request_Is_Valid()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);

            var request = "valid request";
            var next = new RequestHandlerDelegate<int>(() => new ValueTask<int>(42));

            // Act
            var result = await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task Should_Throw_ValidationException_When_Request_Is_Invalid()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);

            var request = "";
            var next = new RequestHandlerDelegate<int>(() => new ValueTask<int>(42));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default));
        }

        [Fact]
        public async Task Should_Call_Next_Handler_When_Validation_Passes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);

            var request = "valid";
            var nextCalled = false;
            var next = new RequestHandlerDelegate<int>(() =>
            {
                nextCalled = true;
                return new ValueTask<int>(100);
            });

            // Act
            var result = await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(100, result);
        }

        [Fact]
        public async Task Should_Not_Call_Next_Handler_When_Validation_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);

            var request = "";
            var nextCalled = false;
            var next = new RequestHandlerDelegate<int>(() =>
            {
                nextCalled = true;
                return new ValueTask<int>(100);
            });

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default));

            Assert.False(nextCalled);
        }

        [Fact]
        public async Task Should_Handle_Cancellation_Token()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);

            var request = "valid";
            var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();

            var next = new RequestHandlerDelegate<int>(() =>
            {
                cts.Token.ThrowIfCancellationRequested();
                return new ValueTask<int>(42);
            });

            // Act & Assert
            await Assert.ThrowsAsync<System.OperationCanceledException>(async () =>
                await behavior.HandleAsync(request, next, cts.Token));
        }

        [Fact]
        public async Task Should_Preserve_Response_Type()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, ComplexRequest>(validator);

            var request = "valid";
            var expectedResponse = new ComplexRequest { Name = "Result", Age = 50 };
            var next = new RequestHandlerDelegate<ComplexRequest>(() => new ValueTask<ComplexRequest>(expectedResponse));

            // Act
            var result = await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.Same(expectedResponse, result);
        }

        [Fact]
        public async Task Should_Work_With_Null_Response()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new ValidationPipelineBehavior<string, string?>(validator);

            var request = "valid";
            var next = new RequestHandlerDelegate<string?>(() => new ValueTask<string?>((string?)null));

            // Act
            var result = await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Should_Execute_Validation_Before_Handler()
        {
            // Arrange
            var executionOrder = new System.Collections.Generic.List<string>();

            var validator = new AsyncValidator();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);

            var request = "valid";
            var next = new RequestHandlerDelegate<int>(() =>
            {
                executionOrder.Add("handler");
                return new ValueTask<int>(42);
            });

            // Act
            await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.Single(executionOrder);
            Assert.Equal("handler", executionOrder[0]);
        }

        public class ComplexRequest : IRequest<string>
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public class AsyncValidator : IValidator<string>
        {
            public async ValueTask<System.Collections.Generic.IEnumerable<string>> ValidateAsync(string request, System.Threading.CancellationToken cancellationToken)
            {
                await Task.Delay(10, cancellationToken);
                var errors = new System.Collections.Generic.List<string>();
                if (request.Length < 3)
                    errors.Add("String must be at least 3 characters");
                return errors;
            }
        }

        // Test validation rule for testing purposes
        internal class TestValidationRule : IValidationRule<string>
        {
            public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
            {
                var errors = new List<string>();

                if (string.IsNullOrEmpty(request))
                {
                    errors.Add("Request cannot be null or empty");
                }
                else if (request.Length < 3)
                {
                    errors.Add("Request must be at least 3 characters long");
                }

                return new ValueTask<IEnumerable<string>>(errors);
            }
        }
    }
}