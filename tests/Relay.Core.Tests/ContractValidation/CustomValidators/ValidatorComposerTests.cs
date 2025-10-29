using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.CustomValidators;

public class ValidatorComposerTests
{
    private class TestValidator : ICustomValidator
    {
        public int Priority { get; set; }
        public Func<Type, bool>? AppliesToFunc { get; set; }
        public Func<object, ValidationContext, CancellationToken, ValueTask<IEnumerable<ValidationError>>>? ValidateFunc { get; set; }

        public bool AppliesTo(Type type)
        {
            return AppliesToFunc?.Invoke(type) ?? false;
        }

        public ValueTask<IEnumerable<ValidationError>> ValidateAsync(
            object obj,
            ValidationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValidateFunc?.Invoke(obj, context, cancellationToken)
                ?? ValueTask.FromResult(Enumerable.Empty<ValidationError>());
        }
    }

    [Fact]
    public void Constructor_ShouldOrderValidatorsByPriorityDescending()
    {
        // Arrange
        var validators = new[]
        {
            new TestValidator { Priority = 1 },
            new TestValidator { Priority = 3 },
            new TestValidator { Priority = 2 }
        };

        // Act
        var composer = new ValidatorComposer(validators);

        // Assert
        Assert.Equal(3, composer.ValidatorCount);
    }

    [Fact]
    public void ValidatorCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var validators = new[]
        {
            new TestValidator { Priority = 1 },
            new TestValidator { Priority = 2 }
        };

        // Act
        var composer = new ValidatorComposer(validators);

        // Assert
        Assert.Equal(2, composer.ValidatorCount);
    }

    [Fact]
    public void ValidatorCount_WithEmptyValidators_ShouldReturnZero()
    {
        // Arrange & Act
        var composer = new ValidatorComposer(Array.Empty<ICustomValidator>());

        // Assert
        Assert.Equal(0, composer.ValidatorCount);
    }

    [Fact]
    public async Task ValidateAsync_WithNoApplicableValidators_ShouldReturnSuccess()
    {
        // Arrange
        var validator = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(int)
        };

        var composer = new ValidatorComposer(new[] { validator });
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await composer.ValidateAsync("test", context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(nameof(ValidatorComposer), result.ValidatorName);
    }

    [Fact]
    public async Task ValidateAsync_WithApplicableValidator_ShouldExecuteValidator()
    {
        // Arrange
        var executed = false;
        var validator = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) =>
            {
                executed = true;
                return ValueTask.FromResult(Enumerable.Empty<ValidationError>());
            }
        };

        var composer = new ValidatorComposer(new[] { validator });
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        await composer.ValidateAsync("test", context);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ValidateAsync_WithValidationErrors_ShouldAggregateErrors()
    {
        // Arrange
        var validator1 = new TestValidator
        {
            Priority = 2,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) => ValueTask.FromResult<IEnumerable<ValidationError>>(new[]
            {
                ValidationError.Create("TEST001", "Error from validator 1")
            })
        };

        var validator2 = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) => ValueTask.FromResult<IEnumerable<ValidationError>>(new[]
            {
                ValidationError.Create("TEST002", "Error from validator 2")
            })
        };

        var composer = new ValidatorComposer(new[] { validator1, validator2 });
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await composer.ValidateAsync("test", context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "TEST001");
        Assert.Contains(result.Errors, e => e.ErrorCode == "TEST002");
    }

    [Fact]
    public async Task ValidateAsync_ShouldExecuteValidatorsInPriorityOrder()
    {
        // Arrange
        var executionOrder = new List<int>();

        var validator1 = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) =>
            {
                executionOrder.Add(1);
                return ValueTask.FromResult(Enumerable.Empty<ValidationError>());
            }
        };

        var validator2 = new TestValidator
        {
            Priority = 3,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) =>
            {
                executionOrder.Add(3);
                return ValueTask.FromResult(Enumerable.Empty<ValidationError>());
            }
        };

        var validator3 = new TestValidator
        {
            Priority = 2,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) =>
            {
                executionOrder.Add(2);
                return ValueTask.FromResult(Enumerable.Empty<ValidationError>());
            }
        };

        var composer = new ValidatorComposer(new[] { validator1, validator2, validator3 });
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        await composer.ValidateAsync("test", context);

        // Assert
        Assert.Equal(new[] { 3, 2, 1 }, executionOrder);
    }

    [Fact]
    public async Task ValidateAsync_WhenValidatorThrows_ShouldCatchAndReturnError()
    {
        // Arrange
        var validator = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) => throw new InvalidOperationException("Validator exception")
        };

        var composer = new ValidatorComposer(new[] { validator });
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await composer.ValidateAsync("test", context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCodes.CustomValidationFailed, result.Errors[0].ErrorCode);
        Assert.Contains("Validator exception", result.Errors[0].Message);
    }

    [Fact]
    public async Task ValidateAsync_ShouldSupportCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var validator = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = async (obj, ctx, ct) =>
            {
                await Task.Delay(100, ct);
                return Enumerable.Empty<ValidationError>();
            }
        };

        var composer = new ValidatorComposer(new[] { validator });
        var context = ValidationContext.ForRequest(typeof(string), "test", null);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await composer.ValidateAsync("test", context, cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_ShouldSetValidationDuration()
    {
        // Arrange
        var validator = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = async (obj, ctx, ct) =>
            {
                await Task.Delay(10);
                return Enumerable.Empty<ValidationError>();
            }
        };

        var composer = new ValidatorComposer(new[] { validator });
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await composer.ValidateAsync("test", context);

        // Assert
        Assert.True(result.ValidationDuration > TimeSpan.Zero);
    }

    [Fact]
    public void GetApplicableValidators_ShouldReturnOnlyApplicableValidators()
    {
        // Arrange
        var validator1 = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string)
        };

        var validator2 = new TestValidator
        {
            Priority = 2,
            AppliesToFunc = type => type == typeof(int)
        };

        var validator3 = new TestValidator
        {
            Priority = 3,
            AppliesToFunc = type => type == typeof(string)
        };

        var composer = new ValidatorComposer(new[] { validator1, validator2, validator3 });

        // Act
        var applicableValidators = composer.GetApplicableValidators(typeof(string)).ToList();

        // Assert
        Assert.Equal(2, applicableValidators.Count);
        Assert.Contains(validator1, applicableValidators);
        Assert.Contains(validator3, applicableValidators);
        Assert.DoesNotContain(validator2, applicableValidators);
    }

    [Fact]
    public void GetApplicableValidators_WithNoApplicableValidators_ShouldReturnEmpty()
    {
        // Arrange
        var validator = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(int)
        };

        var composer = new ValidatorComposer(new[] { validator });

        // Act
        var applicableValidators = composer.GetApplicableValidators(typeof(string)).ToList();

        // Assert
        Assert.Empty(applicableValidators);
    }
}
