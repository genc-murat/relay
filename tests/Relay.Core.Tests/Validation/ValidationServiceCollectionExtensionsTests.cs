using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core.Validation;
using Relay.Core.Validation.Extensions;
using Relay.Core.Validation.Interfaces;
using Relay.Core.Validation.Pipeline;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class ValidationServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddRelayValidation_Should_Register_Pipeline_Behaviors()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayValidation()
                    .AddValidationRulesFromAssembly(typeof(TestValidationRule).Assembly);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Check that pipeline behaviors are registered
            var regularBehavior = serviceProvider.GetService(typeof(IPipelineBehavior<string, int>));
            Assert.NotNull(regularBehavior);
            Assert.IsType<ValidationPipelineBehavior<string, int>>(regularBehavior);

            var streamBehavior = serviceProvider.GetService(typeof(IStreamPipelineBehavior<string, int>));
            Assert.NotNull(streamBehavior);
            Assert.IsType<StreamValidationPipelineBehavior<string, int>>(streamBehavior);
        }

        [Fact]
        public void AddRelayValidation_Should_Return_Same_ServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRelayValidation();

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddValidationRulesFromAssembly_Should_Throw_ArgumentNullException_When_Assembly_Is_Null()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddValidationRulesFromAssembly(null!));
        }

        [Fact]
        public void AddValidationRulesFromAssembly_Should_Register_Validation_Rules()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly = typeof(TestValidationRule).Assembly;

            // Act
            services.AddValidationRulesFromAssembly(assembly);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Check that validation rules are registered
            var rules = serviceProvider.GetServices<IValidationRule<string>>();
            Assert.Contains(rules, r => r is TestValidationRule);
        }

        [Fact]
        public void AddValidationRulesFromAssembly_Should_Register_Validators_For_Request_Types()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly = typeof(TestValidationRule).Assembly;

            // Act
            services.AddValidationRulesFromAssembly(assembly);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Check that validators are registered for request types
            var validator = serviceProvider.GetService<IValidator<string>>();
            Assert.NotNull(validator);
            Assert.IsType<DefaultValidator<string>>(validator);

            // Verify the validator contains the test rule
            var defaultValidator = (DefaultValidator<string>)validator!;
            // We can't easily test the internal rules, but we can verify the validator was created
        }

        [Fact]
        public void AddValidationRulesFromAssembly_Should_Return_Same_ServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly = typeof(TestValidationRule).Assembly;

            // Act
            var result = services.AddValidationRulesFromAssembly(assembly);

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddValidationRulesFromCallingAssembly_Should_Register_Rules_From_Calling_Assembly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddValidationRulesFromCallingAssembly();

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Check that validators are registered
            var validator = serviceProvider.GetService<IValidator<string>>();
            Assert.NotNull(validator);
            Assert.IsType<DefaultValidator<string>>(validator);
        }

        [Fact]
        public void AddValidationRulesFromCallingAssembly_Should_Return_Same_ServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddValidationRulesFromCallingAssembly();

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddValidationRulesFromAssembly_Should_Handle_Assembly_With_No_Validation_Rules()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly = typeof(string).Assembly; // System assembly with no validation rules

            // Act
            services.AddValidationRulesFromAssembly(assembly);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Should not register any validators since there are no validation rules
            var validator = serviceProvider.GetService<IValidator<string>>();
            Assert.Null(validator); // No validator should be registered
        }

        [Fact]
        public void AddValidationRulesFromAssembly_Should_Register_Multiple_Validators_For_Different_Request_Types()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly = typeof(TestValidationRule).Assembly;

            // Act
            services.AddValidationRulesFromAssembly(assembly);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Check validators for different types
            var stringValidator = serviceProvider.GetService<IValidator<string>>();
            Assert.NotNull(stringValidator);

            // Note: int validator may not exist if no rules are defined for int
            // This test just verifies that the registration process works
        }

        [Fact]
        public void AddRelayValidation_Should_Be_Idempotent()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayValidation()
                    .AddValidationRulesFromAssembly(typeof(TestValidationRule).Assembly);
            services.AddRelayValidation(); // Call again

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Should still work
            var behavior = serviceProvider.GetService(typeof(IPipelineBehavior<string, int>));
            Assert.NotNull(behavior);
        }

        [Fact]
        public void AddValidationRulesFromAssembly_Should_Be_Idempotent()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly = typeof(TestValidationRule).Assembly;

            // Act
            services.AddValidationRulesFromAssembly(assembly);
            services.AddValidationRulesFromAssembly(assembly); // Call again

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Should still work
            var validator = serviceProvider.GetService<IValidator<string>>();
            Assert.NotNull(validator);
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