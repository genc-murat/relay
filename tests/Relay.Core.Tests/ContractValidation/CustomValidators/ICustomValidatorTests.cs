using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.CustomValidators;

public class ICustomValidatorTests
{
    private class TestValidator : ICustomValidator
    {
        public int Priority { get; set; } = 0;
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
    public void Priority_ShouldBeAccessible()
    {
        // Arrange
        var validator = new TestValidator { Priority = 10 };

        // Act & Assert
        Assert.Equal(10, validator.Priority);
    }

    [Fact]
    public void AppliesTo_ShouldReturnCorrectValue()
    {
        // Arrange
        var validator = new TestValidator
        {
            AppliesToFunc = type => type == typeof(string)
        };

        // Act & Assert
        Assert.True(validator.AppliesTo(typeof(string)));
        Assert.False(validator.AppliesTo(typeof(int)));
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnErrors()
    {
        // Arrange
        var expectedError = ValidationError.Create("TEST001", "Test error");
        var validator = new TestValidator
        {
            ValidateFunc = (obj, ctx, ct) => ValueTask.FromResult<IEnumerable<ValidationError>>(new[] { expectedError })
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);

        // Act
        var errors = await validator.ValidateAsync("test", context);

        // Assert
        Assert.Single(errors);
        Assert.Equal("TEST001", errors.First().ErrorCode);
        Assert.Equal("Test error", errors.First().Message);
    }

    [Fact]
    public async Task ValidateAsync_ShouldSupportCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var validator = new TestValidator
        {
            ValidateFunc = async (obj, ctx, ct) =>
            {
                await Task.Delay(100, ct);
                return Enumerable.Empty<ValidationError>();
            }
        };

        var context = ValidationContext.ForRequest(typeof(string), "test", null);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await validator.ValidateAsync("test", context, cts.Token));
    }
}
