using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.CustomValidators;

public class AbstractCustomValidatorTests
{
    private class TestCustomValidator : AbstractCustomValidator
    {
        public Func<Type, bool>? AppliesToFunc { get; set; }
        public Func<object, ValidationContext, CancellationToken, ValueTask<IEnumerable<ValidationError>>>? ValidateCoreFunc { get; set; }

        public TestCustomValidator(int priority = 0) : base(priority)
        {
        }

        public override bool AppliesTo(Type type)
        {
            return AppliesToFunc?.Invoke(type) ?? false;
        }

        protected override ValueTask<IEnumerable<ValidationError>> ValidateCoreAsync(
            object obj,
            ValidationContext context,
            CancellationToken cancellationToken)
        {
            return ValidateCoreFunc?.Invoke(obj, context, cancellationToken)
                ?? ValueTask.FromResult(Enumerable.Empty<ValidationError>());
        }
    }

    [Fact]
    public void Constructor_ShouldSetPriority()
    {
        // Arrange & Act
        var validator = new TestCustomValidator(priority: 10);

        // Assert
        Assert.Equal(10, validator.Priority);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultPriority()
    {
        // Arrange & Act
        var validator = new TestCustomValidator();

        // Assert
        Assert.Equal(0, validator.Priority);
    }

    [Fact]
    public async Task ValidateAsync_WithNullObject_ShouldReturnNullObjectError()
    {
        // Arrange
        var validator = new TestCustomValidator();
        var context = ValidationContext.ForRequest(typeof(string), null, null);

        // Act
        var errors = await validator.ValidateAsync(null!, context);

        // Assert
        var errorList = errors.ToList();
        Assert.Single(errorList);
        Assert.Equal(ValidationErrorCodes.CustomValidationFailed, errorList[0].ErrorCode);
        Assert.Contains("cannot be null", errorList[0].Message);
    }

    [Fact]
    public async Task ValidateAsync_WithValidObject_ShouldCallValidateCoreAsync()
    {
        // Arrange
        var called = false;
        var validator = new TestCustomValidator
        {
            ValidateCoreFunc = (obj, ctx, ct) =>
            {
                called = true;
                return ValueTask.FromResult(Enumerable.Empty<ValidationError>());
            }
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        await validator.ValidateAsync("test", context);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task ValidateAsync_WhenValidateCoreThrows_ShouldReturnValidationExceptionError()
    {
        // Arrange
        var validator = new TestCustomValidator
        {
            ValidateCoreFunc = (obj, ctx, ct) => throw new InvalidOperationException("Test exception")
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var errors = await validator.ValidateAsync("test", context);

        // Assert
        var errorList = errors.ToList();
        Assert.Single(errorList);
        Assert.Equal(ValidationErrorCodes.CustomValidationFailed, errorList[0].ErrorCode);
        Assert.Contains("Test exception", errorList[0].Message);
    }

    [Fact]
    public async Task ValidateAsync_WithSuccessfulValidation_ShouldReturnEmptyErrors()
    {
        // Arrange
        var validator = new TestCustomValidator
        {
            ValidateCoreFunc = (obj, ctx, ct) => ValueTask.FromResult(Enumerable.Empty<ValidationError>())
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var errors = await validator.ValidateAsync("test", context);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_WithValidationErrors_ShouldReturnErrors()
    {
        // Arrange
        var expectedErrors = new[]
        {
            ValidationError.Create("TEST001", "Error 1"),
            ValidationError.Create("TEST002", "Error 2")
        };

        var validator = new TestCustomValidator
        {
            ValidateCoreFunc = (obj, ctx, ct) => ValueTask.FromResult<IEnumerable<ValidationError>>(expectedErrors)
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var errors = await validator.ValidateAsync("test", context);

        // Assert
        var errorList = errors.ToList();
        Assert.Equal(2, errorList.Count);
        Assert.Equal("TEST001", errorList[0].ErrorCode);
        Assert.Equal("TEST002", errorList[1].ErrorCode);
    }

    [Fact]
    public async Task ValidateAsync_ShouldSupportCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var validator = new TestCustomValidator
        {
            ValidateCoreFunc = async (obj, ctx, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);
                return Enumerable.Empty<ValidationError>();
            }
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);
        cts.Cancel();

        // Act
        var errors = await validator.ValidateAsync("test", context, cts.Token);

        // Assert - Cancellation is caught and returned as an error
        var errorList = errors.ToList();
        Assert.Single(errorList);
        Assert.Equal(ValidationErrorCodes.CustomValidationFailed, errorList[0].ErrorCode);
    }

    [Fact]
    public async Task ValidateCoreAsync_CanUseCreateErrorHelper()
    {
        // Arrange
        var validator = new TestCustomValidator
        {
            ValidateCoreFunc = (obj, ctx, ct) =>
            {
                var error = ValidationError.Create("TEST001", "Test message");
                return ValueTask.FromResult<System.Collections.Generic.IEnumerable<ValidationError>>(new[] { error });
            }
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var errors = await validator.ValidateAsync("test", context);

        // Assert
        var errorList = errors.ToList();
        Assert.Single(errorList);
        Assert.Equal("TEST001", errorList[0].ErrorCode);
        Assert.Equal("Test message", errorList[0].Message);
    }

    [Fact]
    public async Task ValidateCoreAsync_CanUseCreateErrorHelperWithPath()
    {
        // Arrange
        var validator = new TestCustomValidator
        {
            ValidateCoreFunc = (obj, ctx, ct) =>
            {
                var error = ValidationError.Create("TEST001", "Test message", "$.property");
                return ValueTask.FromResult<System.Collections.Generic.IEnumerable<ValidationError>>(new[] { error });
            }
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var errors = await validator.ValidateAsync("test", context);

        // Assert
        var errorList = errors.ToList();
        Assert.Single(errorList);
        Assert.Equal("TEST001", errorList[0].ErrorCode);
        Assert.Equal("Test message", errorList[0].Message);
        Assert.Equal("$.property", errorList[0].JsonPath);
    }
}
