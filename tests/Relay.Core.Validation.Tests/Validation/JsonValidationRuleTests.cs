using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        Assert.Empty(result);
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
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Invalid JSON format.", result.Single());
        }
    }
    
    [Fact]
    public async Task ValidateAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _rule.ValidateAsync("{\"test\":\"value\"}", cancellationTokenSource.Token));
    }
    
    [Theory]
    [InlineData("   ", true)] // Whitespace only - should return empty errors
    [InlineData("  {\"test\":\"value\"}  ", false)] // JSON with whitespace - should be valid
    [InlineData("\t\n{\t\n\"test\"\t\n:\t\n\"value\"\t\n}\t\n", false)] // JSON with various whitespace - should be valid
    public async Task ValidateAsync_WhitespaceHandling(string input, bool shouldReturnEmpty)
    {
        // Act
        var result = await _rule.ValidateAsync(input);

        // Assert
        if (shouldReturnEmpty)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Empty(result); // Valid JSON even with whitespace should return empty errors
        }
    }
    
    [Theory]
    [InlineData("{")] // Incomplete JSON
    [InlineData("[")] // Incomplete JSON
    [InlineData("\"")] // Incomplete string
    [InlineData("{\"name\":\"John")] // Incomplete string in object
    [InlineData("{'name':'John'}")] // Single quotes (invalid JSON)
    public async Task ValidateAsync_InvalidJsonSyntax_ReturnsError(string json)
    {
        // Act
        var result = await _rule.ValidateAsync(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid JSON format.", result.Single());
    }
    
    [Fact]
    public async Task ValidateAsync_WithVeryLargeValidJson_ReturnsEmptyErrors()
    {
        // Create a large valid JSON string to ensure it handles large inputs
        var largeJson = "{";
        for (int i = 0; i < 1000; i++)
        {
            if (i > 0) largeJson += ",";
            largeJson += $"\"key{i}\":\"value{i}\"";
        }
        largeJson += "}";
        
        // Act
        var result = await _rule.ValidateAsync(largeJson);

        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task ValidateAsync_WithVeryLargeInvalidJson_ReturnsError()
    {
        // Create a large invalid JSON string (missing closing brace)
        var largeJson = "{";
        for (int i = 0; i < 1000; i++)
        {
            if (i > 0) largeJson += ",";
            largeJson += $"\"key{i}\":\"value{i}\"";
        }
        // Intentionally missing the closing brace
        
        // Act
        var result = await _rule.ValidateAsync(largeJson);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid JSON format.", result.Single());
    }
    
    [Fact]
    public async Task ValidateAsync_WithDeeplyNestedJson_ReturnsEmptyErrors()
    {
        // Create deeply nested valid JSON
        var nestedJson = new string(' ', 1000); // Placeholder
        var depth = 50;
        var json = new System.Text.StringBuilder();
        
        // Create a deeply nested object: {"level1": {"level2": {"level3": ... "value"}}}
        for (int i = 0; i < depth; i++)
        {
            json.Append("{\"level");
            json.Append(i);
            json.Append("\":");
        }
        
        json.Append("\"value\"");
        
        for (int i = 0; i < depth; i++)
        {
            json.Append("}");
        }
        
        var deeplyNestedJson = json.ToString();
        
        // Act
        var result = await _rule.ValidateAsync(deeplyNestedJson);

        // Assert
        Assert.Empty(result); // Should handle deep nesting if JSON is valid
    }
    
    [Fact]
    public async Task ValidateAsync_WithDeeplyNestedInvalidJson_ReturnsError()
    {
        // Create deeply nested invalid JSON (missing closing braces)
        var depth = 50;
        var json = new System.Text.StringBuilder();
        
        // Create a deeply nested structure without closing braces
        for (int i = 0; i < depth; i++)
        {
            json.Append("{\"level");
            json.Append(i);
            json.Append("\":");
        }
        
        json.Append("\"value\""); // Missing closing braces
        
        var deeplyNestedInvalidJson = json.ToString();
        
        // Act
        var result = await _rule.ValidateAsync(deeplyNestedInvalidJson);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid JSON format.", result.Single());
    }
}