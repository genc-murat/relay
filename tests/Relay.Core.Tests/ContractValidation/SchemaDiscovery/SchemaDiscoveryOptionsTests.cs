using System;
using System.Linq;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.SchemaDiscovery;

public class SchemaDiscoveryOptionsTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var options = new SchemaDiscoveryOptions();

        // Assert
        Assert.NotNull(options.SchemaDirectories);
        Assert.Empty(options.SchemaDirectories);
        Assert.Equal("{TypeName}.schema.json", options.NamingConvention);
        Assert.True(options.EnableEmbeddedResources);
        Assert.False(options.EnableFileSystemWatcher);
        Assert.False(options.EnableHttpSchemas);
        Assert.NotNull(options.HttpSchemaEndpoints);
        Assert.Empty(options.HttpSchemaEndpoints);
        Assert.Equal(TimeSpan.FromSeconds(5), options.HttpSchemaTimeout);
    }

    [Fact]
    public void SchemaDirectories_CanBeModified()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions();

        // Act
        options.SchemaDirectories.Add("/path/to/schemas");
        options.SchemaDirectories.Add("/another/path");

        // Assert
        Assert.Equal(2, options.SchemaDirectories.Count);
        Assert.Contains("/path/to/schemas", options.SchemaDirectories);
        Assert.Contains("/another/path", options.SchemaDirectories);
    }

    [Fact]
    public void NamingConvention_CanBeSet()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions();
        var customConvention = "{TypeName}.{IsRequest}.schema.json";

        // Act
        options.NamingConvention = customConvention;

        // Assert
        Assert.Equal(customConvention, options.NamingConvention);
    }

    [Fact]
    public void EnableEmbeddedResources_CanBeDisabled()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions();

        // Act
        options.EnableEmbeddedResources = false;

        // Assert
        Assert.False(options.EnableEmbeddedResources);
    }

    [Fact]
    public void EnableFileSystemWatcher_CanBeEnabled()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions();

        // Act
        options.EnableFileSystemWatcher = true;

        // Assert
        Assert.True(options.EnableFileSystemWatcher);
    }

    [Fact]
    public void HttpSchemaEndpoints_CanBeModified()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions();

        // Act
        options.HttpSchemaEndpoints.Add("https://example.com/schemas");
        options.EnableHttpSchemas = true;

        // Assert
        Assert.Single(options.HttpSchemaEndpoints);
        Assert.True(options.EnableHttpSchemas);
    }

    [Fact]
    public void HttpSchemaTimeout_CanBeSet()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions();
        var timeout = TimeSpan.FromSeconds(10);

        // Act
        options.HttpSchemaTimeout = timeout;

        // Assert
        Assert.Equal(timeout, options.HttpSchemaTimeout);
    }
}
