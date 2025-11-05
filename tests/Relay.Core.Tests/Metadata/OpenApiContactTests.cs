using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiContact class
/// </summary>
public class OpenApiContactTests
{
    [Fact]
    public void OpenApiContact_DefaultConstructor_InitializesProperties()
    {
        // Act
        var contact = new OpenApiContact();

        // Assert
        Assert.Null(contact.Name);
        Assert.Null(contact.Url);
        Assert.Null(contact.Email);
    }

    [Fact]
    public void OpenApiContact_CanSetName()
    {
        // Arrange
        var contact = new OpenApiContact();

        // Act
        contact.Name = "Test Contact";

        // Assert
        Assert.Equal("Test Contact", contact.Name);
    }

    [Fact]
    public void OpenApiContact_CanSetUrl()
    {
        // Arrange
        var contact = new OpenApiContact();

        // Act
        contact.Url = "https://example.com";

        // Assert
        Assert.Equal("https://example.com", contact.Url);
    }

    [Fact]
    public void OpenApiContact_CanSetEmail()
    {
        // Arrange
        var contact = new OpenApiContact();

        // Act
        contact.Email = "test@example.com";

        // Assert
        Assert.Equal("test@example.com", contact.Email);
    }

    [Fact]
    public void OpenApiContact_ObjectInitialization_Works()
    {
        // Act
        var contact = new OpenApiContact
        {
            Name = "Test Contact",
            Url = "https://example.com",
            Email = "test@example.com"
        };

        // Assert
        Assert.Equal("Test Contact", contact.Name);
        Assert.Equal("https://example.com", contact.Url);
        Assert.Equal("test@example.com", contact.Email);
    }

    [Fact]
    public void OpenApiContact_CanSetPropertiesToNull()
    {
        // Arrange
        var contact = new OpenApiContact
        {
            Name = "Test",
            Url = "https://example.com",
            Email = "test@example.com"
        };

        // Act
        contact.Name = null;
        contact.Url = null;
        contact.Email = null;

        // Assert
        Assert.Null(contact.Name);
        Assert.Null(contact.Url);
        Assert.Null(contact.Email);
    }
}
