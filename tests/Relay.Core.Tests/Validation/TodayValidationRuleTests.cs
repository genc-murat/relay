using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class TodayValidationRuleTests
{
    [Fact]
    public async Task ValidateAsync_DateTime_Today_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new TodayValidationRule();
        var today = DateTime.Today;

        // Act
        var result = await rule.ValidateAsync(today);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTime_TodayWithTime_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new TodayValidationRule();
        var todayWithTime = DateTime.Now; // Today with current time

        // Act
        var result = await rule.ValidateAsync(todayWithTime);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTime_Yesterday_ReturnsError()
    {
        // Arrange
        var rule = new TodayValidationRule();
        var yesterday = DateTime.Today.AddDays(-1);

        // Act
        var result = await rule.ValidateAsync(yesterday);

        // Assert
        Assert.Single(result, "Date must be today.");
    }

    [Fact]
    public async Task ValidateAsync_DateTime_Tomorrow_ReturnsError()
    {
        // Arrange
        var rule = new TodayValidationRule();
        var tomorrow = DateTime.Today.AddDays(1);

        // Act
        var result = await rule.ValidateAsync(tomorrow);

        // Assert
        Assert.Single(result, "Date must be today.");
    }

    [Fact]
    public async Task ValidateAsync_DateTime_MidnightToday_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new TodayValidationRule();
        var midnightToday = DateTime.Today;

        // Act
        var result = await rule.ValidateAsync(midnightToday);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTime_EndOfToday_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new TodayValidationRule();
        var endOfToday = DateTime.Today.AddDays(1).AddTicks(-1);

        // Act
        var result = await rule.ValidateAsync(endOfToday);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTime_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var rule = new TodayValidationRule();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var today = DateTime.Today;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await rule.ValidateAsync(today, cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_Today_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new TodayValidationRuleDateTimeOffset();
        var today = DateTimeOffset.Now.Date;

        // Act
        var result = await rule.ValidateAsync(today);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_TodayWithTime_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new TodayValidationRuleDateTimeOffset();
        var todayWithTime = DateTimeOffset.Now;

        // Act
        var result = await rule.ValidateAsync(todayWithTime);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_Yesterday_ReturnsError()
    {
        // Arrange
        var rule = new TodayValidationRuleDateTimeOffset();
        var yesterday = DateTimeOffset.Now.Date.AddDays(-1);

        // Act
        var result = await rule.ValidateAsync(yesterday);

        // Assert
        Assert.Single(result, "Date must be today.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_Tomorrow_ReturnsError()
    {
        // Arrange
        var rule = new TodayValidationRuleDateTimeOffset();
        var tomorrow = DateTimeOffset.Now.Date.AddDays(1);

        // Act
        var result = await rule.ValidateAsync(tomorrow);

        // Assert
        Assert.Single(result, "Date must be today.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_WithOffset_Today_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new TodayValidationRuleDateTimeOffset();
        var today = DateTime.Today;
        var offset = TimeSpan.FromHours(5);
        var todayWithOffset = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, offset);

        // Act
        var result = await rule.ValidateAsync(todayWithOffset);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_WithOffset_Yesterday_ReturnsError()
    {
        // Arrange
        var rule = new TodayValidationRuleDateTimeOffset();
        var yesterday = DateTime.Today.AddDays(-1);
        var offset = TimeSpan.FromHours(5);
        var yesterdayWithOffset = new DateTimeOffset(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0, offset);

        // Act
        var result = await rule.ValidateAsync(yesterdayWithOffset);

        // Assert
        Assert.Single(result, "Date must be today.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var rule = new TodayValidationRuleDateTimeOffset();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var today = DateTimeOffset.Now.Date;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await rule.ValidateAsync(today, cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_DateTime_MinValue_ReturnsError()
    {
        // Arrange
        var rule = new TodayValidationRule();
        var minDate = DateTime.MinValue;

        // Act
        var result = await rule.ValidateAsync(minDate);

        // Assert
        Assert.Single(result, "Date must be today.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeOffset_MinValue_ReturnsError()
    {
        // Arrange
        var rule = new TodayValidationRuleDateTimeOffset();
        var minDate = DateTimeOffset.MinValue;

        // Act
        var result = await rule.ValidateAsync(minDate);

        // Assert
        Assert.Single(result, "Date must be today.");
    }
}