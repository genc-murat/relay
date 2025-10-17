using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class PastValidationRuleTests
{
    [Fact]
    public async Task ValidateAsync_DateTime_PastDate_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PastValidationRule();
        var pastDate = DateTime.Now.AddDays(-1);

        // Act
        var result = await rule.ValidateAsync(pastDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTime_FutureDate_ReturnsError()
    {
        // Arrange
        var rule = new PastValidationRule();
        var futureDate = DateTime.Now.AddDays(1);

        // Act
        var result = await rule.ValidateAsync(futureDate);

        // Assert
        Assert.Single(result, "Date must be in the past.");
    }

    [Fact]
    public async Task ValidateAsync_DateTime_CurrentTime_ReturnsError()
    {
        // Arrange
        var rule = new PastValidationRule();
        var currentTime = DateTime.Now.AddMilliseconds(1); // Ensure it's slightly in the future

        // Act
        var result = await rule.ValidateAsync(currentTime);

        // Assert
        Assert.Single(result, "Date must be in the past.");
    }

    [Fact]
    public async Task ValidateAsync_DateTime_BoundaryPast_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PastValidationRule();
        var justPast = DateTime.Now.AddMilliseconds(-1);

        // Act
        var result = await rule.ValidateAsync(justPast);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTime_BoundaryFuture_ReturnsError()
    {
        // Arrange
        var rule = new PastValidationRule();
        var justFuture = DateTime.Now.AddMilliseconds(1);

        // Act
        var result = await rule.ValidateAsync(justFuture);

        // Assert
        Assert.Single(result, "Date must be in the past.");
    }

    [Fact]
    public async Task ValidateAsync_DateTime_VeryOldDate_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PastValidationRule();
        var oldDate = new DateTime(1900, 1, 1);

        // Act
        var result = await rule.ValidateAsync(oldDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTime_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var rule = new PastValidationRule();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var pastDate = DateTime.Now.AddDays(-1);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await rule.ValidateAsync(pastDate, cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_PastDate_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var pastDate = DateTimeOffset.Now.AddDays(-1);

        // Act
        var result = await rule.ValidateAsync(pastDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_FutureDate_ReturnsError()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var futureDate = DateTimeOffset.Now.AddDays(1);

        // Act
        var result = await rule.ValidateAsync(futureDate);

        // Assert
        Assert.Single(result, "Date must be in the past.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_CurrentTime_ReturnsError()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var currentTime = DateTimeOffset.Now.AddMilliseconds(1); // Ensure it's slightly in the future

        // Act
        var result = await rule.ValidateAsync(currentTime);

        // Assert
        Assert.Single(result, "Date must be in the past.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_BoundaryPast_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var justPast = DateTimeOffset.Now.AddMilliseconds(-1);

        // Act
        var result = await rule.ValidateAsync(justPast);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_BoundaryFuture_ReturnsError()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var justFuture = DateTimeOffset.Now.AddMilliseconds(1);

        // Act
        var result = await rule.ValidateAsync(justFuture);

        // Assert
        Assert.Single(result, "Date must be in the past.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_WithOffset_PastDate_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var pastDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(5));

        // Act
        var result = await rule.ValidateAsync(pastDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_WithOffset_FutureDate_ReturnsError()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var futureDate = DateTimeOffset.Now.AddHours(1); // Ensure it's in the future

        // Act
        var result = await rule.ValidateAsync(futureDate);

        // Assert
        Assert.Single(result, "Date must be in the past.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var pastDate = DateTimeOffset.Now.AddDays(-1);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await rule.ValidateAsync(pastDate, cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_DateTime_MinValue_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PastValidationRule();
        var minDate = DateTime.MinValue;

        // Act
        var result = await rule.ValidateAsync(minDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_MinValue_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new PastValidationRuleDateTimeOffset();
        var minDate = DateTimeOffset.MinValue;

        // Act
        var result = await rule.ValidateAsync(minDate);

        // Assert
        Assert.Empty(result);
    }
}