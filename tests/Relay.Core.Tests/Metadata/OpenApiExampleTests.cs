using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiExample class
/// </summary>
public class OpenApiExampleTests
{
    [Fact]
    public void OpenApiExample_DefaultConstructor_InitializesProperties()
    {
        // Act
        var example = new OpenApiExample();

        // Assert
        Assert.Null(example.Summary);
        Assert.Null(example.Description);
        Assert.Null(example.Value);
        Assert.Null(example.ExternalValue);
    }

    [Fact]
    public void OpenApiExample_CanSetSummary()
    {
        // Arrange
        var example = new OpenApiExample();

        // Act
        example.Summary = "Sample user data";

        // Assert
        Assert.Equal("Sample user data", example.Summary);
    }

    [Fact]
    public void OpenApiExample_CanSetDescription()
    {
        // Arrange
        var example = new OpenApiExample();

        // Act
        example.Description = "An example of user data structure";

        // Assert
        Assert.Equal("An example of user data structure", example.Description);
    }

    [Fact]
    public void OpenApiExample_CanSetValue()
    {
        // Arrange
        var example = new OpenApiExample();
        var value = new { name = "John", age = 25 };

        // Act
        example.Value = value;

        // Assert
        Assert.NotNull(example.Value);
        Assert.Equal(value, example.Value);
    }

    [Fact]
    public void OpenApiExample_CanSetExternalValue()
    {
        // Arrange
        var example = new OpenApiExample();

        // Act
        example.ExternalValue = "https://example.com/examples/user.json";

        // Assert
        Assert.Equal("https://example.com/examples/user.json", example.ExternalValue);
    }

    [Fact]
    public void OpenApiExample_ObjectInitialization_Works()
    {
        // Act
        var example = new OpenApiExample
        {
            Summary = "User example",
            Description = "Example user object",
            Value = new { id = 1, name = "John Doe" },
            ExternalValue = "https://api.example.com/examples/user"
        };

        // Assert
        Assert.Equal("User example", example.Summary);
        Assert.Equal("Example user object", example.Description);
        Assert.NotNull(example.Value);
        Assert.Equal("https://api.example.com/examples/user", example.ExternalValue);
    }

    [Fact]
    public void OpenApiExample_CanSetPropertiesToNull()
    {
        // Arrange
        var example = new OpenApiExample
        {
            Summary = "Test",
            Description = "Test description",
            Value = "test value",
            ExternalValue = "https://example.com"
        };

        // Act
        example.Summary = null;
        example.Description = null;
        example.Value = null;
        example.ExternalValue = null;

        // Assert
        Assert.Null(example.Summary);
        Assert.Null(example.Description);
        Assert.Null(example.Value);
        Assert.Null(example.ExternalValue);
    }
}