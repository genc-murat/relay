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
    public class ValidationPipelineBehaviorAsyncTests
    {
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
    }
}