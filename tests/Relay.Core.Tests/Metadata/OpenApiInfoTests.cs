using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiInfo class
/// </summary>
public class OpenApiInfoTests
{
    [Fact]
    public void OpenApiInfo_DefaultConstructor_InitializesProperties()
    {
        // Act
        var info = new OpenApiInfo();

        // Assert
        Assert.Equal("Relay API", info.Title);
        Assert.Null(info.Description);
        Assert.Equal("1.0.0", info.Version);
        Assert.Null(info.Contact);
        Assert.Null(info.License);
    }

    [Fact]
    public void OpenApiInfo_CanSetTitle()
    {
        // Arrange
        var info = new OpenApiInfo();

        // Act
        info.Title = "Custom API";

        // Assert
        Assert.Equal("Custom API", info.Title);
    }

    [Fact]
    public void OpenApiInfo_CanSetDescription()
    {
        // Arrange
        var info = new OpenApiInfo();

        // Act
        info.Description = "Custom description";

        // Assert
        Assert.Equal("Custom description", info.Description);
    }

    [Fact]
    public void OpenApiInfo_CanSetVersion()
    {
        // Arrange
        var info = new OpenApiInfo();

        // Act
        info.Version = "2.0.0";

        // Assert
        Assert.Equal("2.0.0", info.Version);
    }

    [Fact]
    public void OpenApiInfo_CanSetContact()
    {
        // Arrange
        var info = new OpenApiInfo();
        var contact = new OpenApiContact { Name = "Test Contact" };

        // Act
        info.Contact = contact;

        // Assert
        Assert.Equal(contact, info.Contact);
        Assert.Equal("Test Contact", info.Contact.Name);
    }

    [Fact]
    public void OpenApiInfo_CanSetLicense()
    {
        // Arrange
        var info = new OpenApiInfo();
        var license = new OpenApiLicense { Name = "MIT" };

        // Act
        info.License = license;

        // Assert
        Assert.Equal(license, info.License);
        Assert.Equal("MIT", info.License.Name);
    }

    [Fact]
    public void OpenApiInfo_ObjectInitialization_Works()
    {
        // Act
        var info = new OpenApiInfo
        {
            Title = "Test API",
            Description = "Test description",
            Version = "3.0.0",
            Contact = new OpenApiContact { Name = "Test Contact" },
            License = new OpenApiLicense { Name = "MIT" }
        };

        // Assert
        Assert.Equal("Test API", info.Title);
        Assert.Equal("Test description", info.Description);
        Assert.Equal("3.0.0", info.Version);
        Assert.NotNull(info.Contact);
        Assert.Equal("Test Contact", info.Contact.Name);
        Assert.NotNull(info.License);
        Assert.Equal("MIT", info.License.Name);
    }
}
