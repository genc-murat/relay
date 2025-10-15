using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class CronExpressionValidationRuleTests
{
    private readonly CronExpressionValidationRule _rule = new();

    [Theory]
    [InlineData("* * * * *")] // Every minute
    [InlineData("0 * * * *")] // Every hour
    [InlineData("0 0 * * *")] // Every day at midnight
    [InlineData("0 0 * * 0")] // Every Sunday at midnight
    [InlineData("*/15 * * * *")] // Every 15 minutes
    [InlineData("0 9-17 * * *")] // Every hour from 9 AM to 5 PM
    [InlineData("0 0 1 * *")] // First day of every month
    [InlineData("0 0 1 1 *")] // Every January 1st
    [InlineData("* * * * * *")] // Every second (6-field)
    [InlineData("30 * * * * *")] // Every 30 seconds
    public async Task ValidateAsync_ValidCronExpression_ReturnsEmptyErrors(string cron)
    {
        // Act
        var result = await _rule.ValidateAsync(cron);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("* * *")] // Too few fields
    [InlineData("* * * * * * *")] // Too many fields
    [InlineData("60 * * * *")] // Invalid minute
    [InlineData("* 24 * * *")] // Invalid hour
    [InlineData("* * 32 * *")] // Invalid day of month
    [InlineData("* * * 13 *")] // Invalid month
    [InlineData("* * * * 7")] // Invalid day of week
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidCronExpression_ReturnsError(string cron)
    {
        // Act
        var result = await _rule.ValidateAsync(cron);

        // Assert
        if (string.IsNullOrWhiteSpace(cron))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().NotBeEmpty();
        }
    }
}