using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Metadata;

/// <summary>
/// Tests for OpenApiLicense class
/// </summary>
public class OpenApiLicenseTests
{
    [Fact]
    public void OpenApiLicense_DefaultConstructor_InitializesProperties()
    {
        // Act
        var license = new OpenApiLicense();

        // Assert
        Assert.Equal(string.Empty, license.Name);
        Assert.Null(license.Url);
    }

    [Fact]
    public void OpenApiLicense_CanSetName()
    {
        // Arrange
        var license = new OpenApiLicense();

        // Act
        license.Name = "MIT";

        // Assert
        Assert.Equal("MIT", license.Name);
    }

    [Fact]
    public void OpenApiLicense_CanSetUrl()
    {
        // Arrange
        var license = new OpenApiLicense();

        // Act
        license.Url = "https://opensource.org/licenses/MIT";

        // Assert
        Assert.Equal("https://opensource.org/licenses/MIT", license.Url);
    }

    [Fact]
    public void OpenApiLicense_ObjectInitialization_Works()
    {
        // Act
        var license = new OpenApiLicense
        {
            Name = "MIT",
            Url = "https://opensource.org/licenses/MIT"
        };

        // Assert
        Assert.Equal("MIT", license.Name);
        Assert.Equal("https://opensource.org/licenses/MIT", license.Url);
    }

    [Fact]
    public void OpenApiLicense_CanSetUrlToNull()
    {
        // Arrange
        var license = new OpenApiLicense
        {
            Url = "https://example.com"
        };

        // Act
        license.Url = null;

        // Assert
        Assert.Null(license.Url);
    }
}