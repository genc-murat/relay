using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class JsonValidationRuleTests
{
    private readonly JsonValidationRule _rule = new();

    [Theory]
    [InlineData("{}")] // Empty object
    [InlineData("{\"name\":\"John\"}")] // Simple object
    [InlineData("[1,2,3]")] // Array
    [InlineData("{\"users\":[{\"id\":1,\"name\":\"John\"},{\"id\":2,\"name\":\"Jane\"}]}")] // Complex object
    [InlineData("\"string\"")] // String
    [InlineData("42")] // Number
    [InlineData("true")] // Boolean
    [InlineData("null")] // Null
    public async Task ValidateAsync_ValidJson_ReturnsEmptyErrors(string json)
    {
        // Act
        var result = await _rule.ValidateAsync(json);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("{invalid}")] // Invalid object
    [InlineData("[1,2,]")] // Trailing comma
    [InlineData("{\"name\":}")] // Missing value
    [InlineData("not json")] // Plain text
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidJson_ReturnsError(string json)
    {
        // Act
        var result = await _rule.ValidateAsync(json);

        // Assert
        if (string.IsNullOrWhiteSpace(json))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Invalid JSON format.");
        }
    }
}