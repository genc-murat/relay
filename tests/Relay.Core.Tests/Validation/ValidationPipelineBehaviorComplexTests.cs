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
    public class ValidationPipelineBehaviorComplexTests
    {
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
    }
}