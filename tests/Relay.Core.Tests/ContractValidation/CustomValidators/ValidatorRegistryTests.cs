using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.CustomValidators;

public class ValidatorRegistryTests
{
    private class TestValidator : AbstractCustomValidator
    {
        public override bool AppliesTo(Type type) => type == typeof(string);

        protected override ValueTask<System.Collections.Generic.IEnumerable<ValidationError>> ValidateCoreAsync(
            object obj,
            ValidationContext context,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(System.Linq.Enumerable.Empty<ValidationError>());
        }
    }

    private class AnotherTestValidator : AbstractCustomValidator
    {
        public override bool AppliesTo(Type type) => type == typeof(int);

        protected override ValueTask<System.Collections.Generic.IEnumerable<ValidationError>> ValidateCoreAsync(
            object obj,
            ValidationContext context,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(System.Linq.Enumerable.Empty<ValidationError>());
        }
    }

    [Fact]
    public void Register_ShouldAddValidator()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        var validator = new TestValidator();

        // Act
        registry.Register(validator);

        // Assert
        var validators = registry.GetAll().ToList();
        Assert.Single(validators);
        Assert.Contains(validator, validators);
    }

    [Fact]
    public void Register_WithNullValidator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new ValidatorRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void Register_WithDuplicateValidator_ShouldNotAddDuplicate()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        var validator = new TestValidator();

        // Act
        registry.Register(validator);
        registry.Register(validator);

        // Assert
        var validators = registry.GetAll().ToList();
        Assert.Single(validators);
    }

    [Fact]
    public void RegisterGeneric_ShouldCreateAndAddValidator()
    {
        // Arrange
        var registry = new ValidatorRegistry();

        // Act
        registry.Register<TestValidator>();

        // Assert
        var validators = registry.GetAll().ToList();
        Assert.Single(validators);
        Assert.IsType<TestValidator>(validators[0]);
    }

    [Fact]
    public void Unregister_ShouldRemoveValidator()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        var validator = new TestValidator();
        registry.Register(validator);

        // Act
        var result = registry.Unregister(validator);

        // Assert
        Assert.True(result);
        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void Unregister_WithNonExistentValidator_ShouldReturnFalse()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        var validator = new TestValidator();

        // Act
        var result = registry.Unregister(validator);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Unregister_WithNullValidator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new ValidatorRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Unregister(null!));
    }

    [Fact]
    public void UnregisterAll_ShouldRemoveAllValidatorsOfType()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        registry.Register<TestValidator>();
        registry.Register<TestValidator>();
        registry.Register<AnotherTestValidator>();

        // Act
        var count = registry.UnregisterAll<TestValidator>();

        // Assert
        Assert.Equal(2, count);
        var remaining = registry.GetAll().ToList();
        Assert.Single(remaining);
        Assert.IsType<AnotherTestValidator>(remaining[0]);
    }

    [Fact]
    public void UnregisterAll_WithNoMatchingValidators_ShouldReturnZero()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        registry.Register<AnotherTestValidator>();

        // Act
        var count = registry.UnregisterAll<TestValidator>();

        // Assert
        Assert.Equal(0, count);
        Assert.Single(registry.GetAll());
    }

    [Fact]
    public void GetAll_ShouldReturnAllRegisteredValidators()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        var validator1 = new TestValidator();
        var validator2 = new AnotherTestValidator();
        registry.Register(validator1);
        registry.Register(validator2);

        // Act
        var validators = registry.GetAll().ToList();

        // Assert
        Assert.Equal(2, validators.Count);
        Assert.Contains(validator1, validators);
        Assert.Contains(validator2, validators);
    }

    [Fact]
    public void GetAll_WithNoValidators_ShouldReturnEmpty()
    {
        // Arrange
        var registry = new ValidatorRegistry();

        // Act
        var validators = registry.GetAll().ToList();

        // Assert
        Assert.Empty(validators);
    }

    [Fact]
    public void GetValidatorsFor_ShouldReturnApplicableValidators()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        var validator1 = new TestValidator(); // Applies to string
        var validator2 = new AnotherTestValidator(); // Applies to int
        registry.Register(validator1);
        registry.Register(validator2);

        // Act
        var validators = registry.GetValidatorsFor(typeof(string)).ToList();

        // Assert
        Assert.Single(validators);
        Assert.Contains(validator1, validators);
    }

    [Fact]
    public void GetValidatorsFor_WithNoApplicableValidators_ShouldReturnEmpty()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        var validator = new TestValidator(); // Applies to string
        registry.Register(validator);

        // Act
        var validators = registry.GetValidatorsFor(typeof(int)).ToList();

        // Assert
        Assert.Empty(validators);
    }

    [Fact]
    public void GetValidatorsFor_WithNullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new ValidatorRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.GetValidatorsFor(null!));
    }

    [Fact]
    public void Clear_ShouldRemoveAllValidators()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        registry.Register<TestValidator>();
        registry.Register<AnotherTestValidator>();

        // Act
        registry.Clear();

        // Assert
        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void Registry_ShouldBeThreadSafe()
    {
        // Arrange
        var registry = new ValidatorRegistry();
        var tasks = new Task[10];

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    registry.Register<TestValidator>();
                    registry.GetAll();
                    registry.Clear();
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - No exceptions should be thrown
        Assert.Empty(registry.GetAll());
    }
}
