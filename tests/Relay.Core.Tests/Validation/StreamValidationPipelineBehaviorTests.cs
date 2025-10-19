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
    public class StreamValidationPipelineBehaviorTests
    {
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