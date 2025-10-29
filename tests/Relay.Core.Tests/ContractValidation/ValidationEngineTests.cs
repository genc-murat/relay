using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;
using Xunit;

namespace Relay.Core.Tests.ContractValidation;

public class ValidationEngineTests
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
    public void Constructor_WithoutValidatorComposer_ShouldNotHaveCustomValidators()
    {
        // Arrange & Act
        var engine = new ValidationEngine();

        // Assert
        Assert.False(engine.HasCustomValidators);
    }

    [Fact]
    public void Constructor_WithValidatorComposer_ShouldHaveCustomValidators()
    {
        // Arrange
        var validator = new TestValidator { Priority = 1 };
        var composer = new ValidatorComposer(new[] { validator });

        // Act
        var engine = new ValidationEngine(composer);

        // Assert
        Assert.True(engine.HasCustomValidators);
    }

    [Fact]
    public void Constructor_WithNullValidatorComposer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationEngine(null!));
    }

    [Fact]
    public void Constructor_WithEmptyValidatorComposer_ShouldNotHaveCustomValidators()
    {
        // Arrange
        var composer = new ValidatorComposer(Array.Empty<ICustomValidator>());

        // Act
        var engine = new ValidationEngine(composer);

        // Assert
        Assert.False(engine.HasCustomValidators);
    }

    [Fact]
    public async Task ValidateAsync_WithNullObject_ShouldReturnFailure()
    {
        // Arrange
        var engine = new ValidationEngine();
        var context = ValidationContext.ForRequest(typeof(string), null, null);

        // Act
        var result = await engine.ValidateAsync(null!, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCodes.CustomValidationFailed, result.Errors[0].ErrorCode);
        Assert.Contains("cannot be null", result.Errors[0].Message);
    }

    [Fact]
    public async Task ValidateAsync_WithoutCustomValidators_ShouldReturnSuccess()
    {
        // Arrange
        var engine = new ValidationEngine();
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await engine.ValidateAsync("test", context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(nameof(ValidationEngine), result.ValidatorName);
    }

    [Fact]
    public async Task ValidateAsync_WithCustomValidators_ShouldExecuteValidators()
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
        var engine = new ValidationEngine(composer);
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        await engine.ValidateAsync("test", context);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ValidateAsync_WithValidationErrors_ShouldReturnFailure()
    {
        // Arrange
        var validator = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) => ValueTask.FromResult<IEnumerable<ValidationError>>(new[]
            {
                ValidationError.Create("TEST001", "Test error")
            })
        };

        var composer = new ValidatorComposer(new[] { validator });
        var engine = new ValidationEngine(composer);
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await engine.ValidateAsync("test", context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("TEST001", result.Errors[0].ErrorCode);
        Assert.Equal("Test error", result.Errors[0].Message);
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
        var engine = new ValidationEngine(composer);
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await engine.ValidateAsync("test", context);

        // Assert
        Assert.True(result.ValidationDuration > TimeSpan.Zero);
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
        var engine = new ValidationEngine(composer);
        var context = ValidationContext.ForRequest(typeof(string), "test", null);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await engine.ValidateAsync("test", context, cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleValidators_ShouldAggregateErrors()
    {
        // Arrange
        var validator1 = new TestValidator
        {
            Priority = 2,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) => ValueTask.FromResult<IEnumerable<ValidationError>>(new[]
            {
                ValidationError.Create("TEST001", "Error 1")
            })
        };

        var validator2 = new TestValidator
        {
            Priority = 1,
            AppliesToFunc = type => type == typeof(string),
            ValidateFunc = (obj, ctx, ct) => ValueTask.FromResult<IEnumerable<ValidationError>>(new[]
            {
                ValidationError.Create("TEST002", "Error 2")
            })
        };

        var composer = new ValidatorComposer(new[] { validator1, validator2 });
        var engine = new ValidationEngine(composer);
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await engine.ValidateAsync("test", context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "TEST001");
        Assert.Contains(result.Errors, e => e.ErrorCode == "TEST002");
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
        var engine = new ValidationEngine(composer);
        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var result = await engine.ValidateAsync("test", context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
