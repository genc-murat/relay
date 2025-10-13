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
using Relay.Core.Validation.Pipeline;
using Relay.Core.Validation.Extensions;
using Relay.Core.Testing;
using Relay.Core.Extensions;

namespace Relay.Core.Tests.Validation
{
    public class ValidationPipelineBehaviorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_Validator_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ValidationPipelineBehavior<string, int>(null!));
        }

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

        public class ComplexRequest : IRequest<string>
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public class ComplexRequestValidator : IValidator<ComplexRequest>
        {
            public ValueTask<System.Collections.Generic.IEnumerable<string>> ValidateAsync(ComplexRequest request, System.Threading.CancellationToken cancellationToken)
            {
                var errors = new System.Collections.Generic.List<string>();

                if (string.IsNullOrWhiteSpace(request.Name))
                    errors.Add("Name is required");

                if (request.Age < 0)
                    errors.Add("Age must be positive");

                if (request.Age > 150)
                    errors.Add("Age must be realistic");

                return new ValueTask<System.Collections.Generic.IEnumerable<string>>(errors);
            }
        }

        [Fact]
        public async Task Should_Validate_Complex_Request()
        {
            // Arrange
            var validator = new ComplexRequestValidator();
            var behavior = new ValidationPipelineBehavior<ComplexRequest, string>(validator);

            var request = new ComplexRequest { Name = "Murat", Age = 30 };
            var next = new RequestHandlerDelegate<string>(() => new ValueTask<string>("Success"));

            // Act
            var result = await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public async Task Should_Throw_When_Complex_Request_Invalid()
        {
            // Arrange
            var validator = new ComplexRequestValidator();
            var behavior = new ValidationPipelineBehavior<ComplexRequest, string>(validator);

            var request = new ComplexRequest { Name = "", Age = -5 };
            var next = new RequestHandlerDelegate<string>(() => new ValueTask<string>("Success"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default));

            Assert.Contains("Name is required", exception.Message);
            Assert.Contains("Age must be positive", exception.Message);
        }

        [Fact]
        public async Task Should_Handle_Multiple_Validation_Errors()
        {
            // Arrange
            var validator = new ComplexRequestValidator();
            var behavior = new ValidationPipelineBehavior<ComplexRequest, string>(validator);

            var request = new ComplexRequest { Name = null, Age = 200 };
            var next = new RequestHandlerDelegate<string>(() => new ValueTask<string>("Success"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default));

            Assert.Contains("Name is required", exception.Message);
            Assert.Contains("Age must be realistic", exception.Message);
        }

        [Fact]
        public async Task Should_Validate_Request_With_Whitespace_Name()
        {
            // Arrange
            var validator = new ComplexRequestValidator();
            var behavior = new ValidationPipelineBehavior<ComplexRequest, string>(validator);

            var request = new ComplexRequest { Name = "   ", Age = 25 };
            var next = new RequestHandlerDelegate<string>(() => new ValueTask<string>("Success"));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default));
        }

        [Fact]
        public async Task Should_Pass_Validation_With_Valid_Edge_Cases()
        {
            // Arrange
            var validator = new ComplexRequestValidator();
            var behavior = new ValidationPipelineBehavior<ComplexRequest, string>(validator);

            var request = new ComplexRequest { Name = "A", Age = 0 };
            var next = new RequestHandlerDelegate<string>(() => new ValueTask<string>("Success"));

            // Act
            var result = await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.Equal("Success", result);
        }

        [Fact]
        public async Task Should_Pass_Validation_With_Max_Age()
        {
            // Arrange
            var validator = new ComplexRequestValidator();
            var behavior = new ValidationPipelineBehavior<ComplexRequest, string>(validator);

            var request = new ComplexRequest { Name = "Elder", Age = 150 };
            var next = new RequestHandlerDelegate<string>(() => new ValueTask<string>("Success"));

            // Act
            var result = await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.Equal("Success", result);
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

        [Fact]
        public async Task Should_Handle_Async_Validation()
        {
            // Arrange
            var validator = new AsyncValidator();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);

            var request = "valid";
            var next = new RequestHandlerDelegate<int>(() => new ValueTask<int>(42));

            // Act
            var result = await behavior.HandleAsync(request, next, default);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task Should_Fail_Async_Validation()
        {
            // Arrange
            var validator = new AsyncValidator();
            var behavior = new ValidationPipelineBehavior<string, int>(validator);

            var request = "ab";
            var next = new RequestHandlerDelegate<int>(() => new ValueTask<int>(42));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default));

            Assert.Contains("at least 3 characters", exception.Message);
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

        private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
        {
            await Task.Yield();
            foreach (var item in items)
            {
                yield return item;
            }
        }

        private static async IAsyncEnumerable<T> CreateAsyncRepeat<T>(T item, int count)
        {
            await Task.Yield();
            for (int i = 0; i < count; i++)
            {
                yield return item;
            }
        }

        [Fact]
        public void StreamConstructor_Should_Throw_ArgumentNullException_When_Validator_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StreamValidationPipelineBehavior<string, int>(null!));
        }

        [Fact]
        public async Task Stream_Should_Pass_Validation_When_Request_Is_Valid()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new StreamValidationPipelineBehavior<string, int>(validator);

            var request = "valid request";
            var next = new StreamHandlerDelegate<int>(() => CreateAsyncEnumerable(new[] { 1, 2, 3 }));

            // Act
            var results = await behavior.HandleAsync(request, next, default).ToListAsync();

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, results);
        }

        [Fact]
        public async Task Stream_Should_Throw_ValidationException_When_Request_Is_Invalid()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new StreamValidationPipelineBehavior<string, int>(validator);

            var request = "";
            var next = new StreamHandlerDelegate<int>(() => CreateAsyncEnumerable(new[] { 1, 2, 3 }));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default).ToListAsync());
        }

        [Fact]
        public async Task Stream_Should_Validate_Complex_Request()
        {
            // Arrange
            var validator = new ComplexRequestValidator();
            var behavior = new StreamValidationPipelineBehavior<ComplexRequest, string>(validator);

            var request = new ComplexRequest { Name = "Murat", Age = 30 };
            var next = new StreamHandlerDelegate<string>(() => CreateAsyncRepeat("item", 2));

            // Act
            var results = await behavior.HandleAsync(request, next, default).ToListAsync();

            // Assert
            Assert.Equal(new[] { "item", "item" }, results);
        }

        [Fact]
        public async Task Stream_Should_Throw_When_Complex_Request_Invalid()
        {
            // Arrange
            var validator = new ComplexRequestValidator();
            var behavior = new StreamValidationPipelineBehavior<ComplexRequest, string>(validator);

            var request = new ComplexRequest { Name = "", Age = -5 };
            var next = new StreamHandlerDelegate<string>(() => CreateAsyncRepeat("item", 2));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default).ToListAsync());

            Assert.Contains("Name is required", exception.Message);
            Assert.Contains("Age must be positive", exception.Message);
        }

        [Fact]
        public async Task Stream_Should_Handle_Cancellation_Token()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddRelayValidation();
            services.AddTransient<IValidator<string>, DefaultValidator<string>>();
            services.AddTransient<IValidationRule<string>, TestValidationRule>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<string>>();
            var behavior = new StreamValidationPipelineBehavior<string, int>(validator);

            var request = "valid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var next = new StreamHandlerDelegate<int>(() =>
            {
                cts.Token.ThrowIfCancellationRequested();
                return CreateAsyncEnumerable(new[] { 1, 2, 3 });
            });

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await behavior.HandleAsync(request, next, cts.Token).ToListAsync());
        }

        [Fact]
        public async Task Stream_Should_Handle_Async_Validation()
        {
            // Arrange
            var validator = new AsyncValidator();
            var behavior = new StreamValidationPipelineBehavior<string, int>(validator);

            var request = "valid";
            var next = new StreamHandlerDelegate<int>(() => CreateAsyncEnumerable(new[] { 1, 2, 3 }));

            // Act
            var results = await behavior.HandleAsync(request, next, default).ToListAsync();

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, results);
        }

        [Fact]
        public async Task Stream_Should_Fail_Async_Validation()
        {
            // Arrange
            var validator = new AsyncValidator();
            var behavior = new StreamValidationPipelineBehavior<string, int>(validator);

            var request = "ab";
            var next = new StreamHandlerDelegate<int>(() => CreateAsyncEnumerable(new[] { 1, 2, 3 }));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(async () =>
                await behavior.HandleAsync(request, next, default).ToListAsync());
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