using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Endpoints;

/// <summary>
/// Tests for OpenApiServerVariable class and its usage in OpenAPI documents
/// </summary>
public class OpenApiServerVariableTests
{
    [Fact]
    public void OpenApiServerVariable_DefaultConstructor_InitializesProperties()
    {
        // Act
        var variable = new OpenApiServerVariable();

        // Assert
        Assert.Equal(string.Empty, variable.Default);
        Assert.Null(variable.Description);
        Assert.NotNull(variable.Enum);
        Assert.Empty(variable.Enum);
    }

    [Fact]
    public void OpenApiServerVariable_CanSetDefaultValue()
    {
        // Arrange
        var variable = new OpenApiServerVariable
        {
            // Act
            Default = "https"
        };

        // Assert
        Assert.Equal("https", variable.Default);
    }

    [Fact]
    public void OpenApiServerVariable_CanSetDescription()
    {
        // Arrange
        var variable = new OpenApiServerVariable
        {
            // Act
            Description = "Protocol to use"
        };

        // Assert
        Assert.Equal("Protocol to use", variable.Description);
    }

    [Fact]
    public void OpenApiServerVariable_CanAddEnumValues()
    {
        // Arrange
        var variable = new OpenApiServerVariable();

        // Act
        variable.Enum.Add("http");
        variable.Enum.Add("https");

        // Assert
        Assert.Equal(2, variable.Enum.Count);
        Assert.Contains("http", variable.Enum);
        Assert.Contains("https", variable.Enum);
    }

    [Fact]
    public void OpenApiServerVariable_EnumIsInitializedAsEmptyList()
    {
        // Arrange
        var variable = new OpenApiServerVariable();

        // Assert
        Assert.NotNull(variable.Enum);
        Assert.IsType<List<string>>(variable.Enum);
        Assert.Empty(variable.Enum);
    }

    [Fact]
    public void OpenApiServerVariable_CanInitializeWithValues()
    {
        // Act
        var variable = new OpenApiServerVariable
        {
            Default = "production",
            Description = "Environment selection",
            Enum = ["development", "staging", "production"]
        };

        // Assert
        Assert.Equal("production", variable.Default);
        Assert.Equal("Environment selection", variable.Description);
        Assert.Equal(3, variable.Enum.Count);
        Assert.Contains("development", variable.Enum);
        Assert.Contains("staging", variable.Enum);
        Assert.Contains("production", variable.Enum);
    }

    [Fact]
    public void OpenApiServer_VariablesProperty_IsInitialized()
    {
        // Act
        var server = new OpenApiServer();

        // Assert
        Assert.NotNull(server.Variables);
        Assert.IsType<Dictionary<string, OpenApiServerVariable>>(server.Variables);
        Assert.Empty(server.Variables);
    }

    [Fact]
    public void OpenApiServer_CanAddServerVariables()
    {
        // Arrange
        var server = new OpenApiServer
        {
            Url = "https://{environment}.api.example.com",
            Description = "API server with environment variable"
        };

        var envVariable = new OpenApiServerVariable
        {
            Default = "prod",
            Description = "Environment name",
            Enum = ["dev", "staging", "prod"]
        };

        // Act
        server.Variables["environment"] = envVariable;

        // Assert
        Assert.Single(server.Variables);
        Assert.True(server.Variables.ContainsKey("environment"));
        Assert.Equal("prod", server.Variables["environment"].Default);
        Assert.Equal("Environment name", server.Variables["environment"].Description);
        Assert.Equal(3, server.Variables["environment"].Enum.Count);
    }

    [Fact]
    public void OpenApiServer_CanHaveMultipleVariables()
    {
        // Arrange
        var server = new OpenApiServer
        {
            Url = "{protocol}://{environment}.api.example.com:{port}",
            Description = "Server with multiple variables"
        };

        // Act
        server.Variables["protocol"] = new OpenApiServerVariable
        {
            Default = "https",
            Enum = ["http", "https"]
        };

        server.Variables["environment"] = new OpenApiServerVariable
        {
            Default = "prod",
            Enum = ["dev", "staging", "prod"]   
        };

        server.Variables["port"] = new OpenApiServerVariable
        {
            Default = "443",
            Description = "Port number"
        };

        // Assert
        Assert.Equal(3, server.Variables.Count);
        Assert.True(server.Variables.ContainsKey("protocol"));
        Assert.True(server.Variables.ContainsKey("environment"));
        Assert.True(server.Variables.ContainsKey("port"));

        Assert.Equal("https", server.Variables["protocol"].Default);
        Assert.Equal("prod", server.Variables["environment"].Default);
        Assert.Equal("443", server.Variables["port"].Default);
    }

    [Fact]
    public void OpenApiDocument_ServersProperty_IsInitialized()
    {
        // Act
        var document = new OpenApiDocument();

        // Assert
        Assert.NotNull(document.Servers);
        Assert.IsType<List<OpenApiServer>>(document.Servers);
        Assert.Empty(document.Servers);
    }

    [Fact]
    public void OpenApiDocument_CanAddServersWithVariables()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "1.0.0" }
        };

        var server = new OpenApiServer
        {
            Url = "https://{environment}.api.example.com",
            Description = "Environment-specific server"
        };

        server.Variables["environment"] = new OpenApiServerVariable
        {
            Default = "prod",
            Description = "Target environment",
            Enum = ["dev", "staging", "prod"]
        };

        // Act
        document.Servers.Add(server);

        // Assert
        Assert.Single(document.Servers);
        var addedServer = document.Servers[0];
        Assert.Equal("https://{environment}.api.example.com", addedServer.Url);
        Assert.Equal("Environment-specific server", addedServer.Description);
        Assert.Single(addedServer.Variables);
        Assert.True(addedServer.Variables.ContainsKey("environment"));
    }

    [Fact]
    public void OpenApiGenerationOptions_ServersProperty_IsInitializedWithDefaultServer()
    {
        // Act
        var options = new OpenApiGenerationOptions();

        // Assert
        Assert.NotNull(options.Servers);
        Assert.Single(options.Servers);
        Assert.Equal("https://localhost:5001", options.Servers[0].Url);
        Assert.Equal("Development server", options.Servers[0].Description);
        Assert.Empty(options.Servers[0].Variables);
    }

    [Fact]
    public void OpenApiGenerationOptions_CanAddServersWithVariables()
    {
        // Arrange
        var options = new OpenApiGenerationOptions();

        var server = new OpenApiServer
        {
            Url = "{protocol}://api.example.com",
            Description = "Configurable protocol server"
        };

        server.Variables["protocol"] = new OpenApiServerVariable
        {
            Default = "https",
            Description = "Communication protocol",
            Enum = ["http", "https"]
        };

        // Act
        options.Servers.Add(server);

        // Assert
        Assert.Equal(2, options.Servers.Count); // Default + added
        var addedServer = options.Servers[1];
        Assert.Equal("{protocol}://api.example.com", addedServer.Url);
        Assert.Equal("Configurable protocol server", addedServer.Description);
        Assert.Single(addedServer.Variables);
        Assert.True(addedServer.Variables.ContainsKey("protocol"));
    }

    [Fact]
    public void OpenApiServerVariable_CanSerializeToJson()
    {
        // Arrange
        var variable = new OpenApiServerVariable
        {
            Default = "prod",
            Description = "Environment",
            Enum = ["dev", "prod"]
        };

        // Act
        var json = JsonSerializer.Serialize(variable);
        var deserialized = JsonSerializer.Deserialize<OpenApiServerVariable>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("prod", deserialized.Default);
        Assert.Equal("Environment", deserialized.Description);
        Assert.Equal(2, deserialized.Enum.Count);
        Assert.Contains("dev", deserialized.Enum);
        Assert.Contains("prod", deserialized.Enum);
    }

    [Fact]
    public void OpenApiServer_CanSerializeToJson_WithVariables()
    {
        // Arrange
        var server = new OpenApiServer
        {
            Url = "https://{env}.api.example.com",
            Description = "Environment server"
        };

        server.Variables["env"] = new OpenApiServerVariable
        {
            Default = "prod",
            Enum = ["dev", "prod"]
        };

        // Act
        var json = JsonSerializer.Serialize(server);
        var deserialized = JsonSerializer.Deserialize<OpenApiServer>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("https://{env}.api.example.com", deserialized.Url);
        Assert.Equal("Environment server", deserialized.Description);
        Assert.Single(deserialized.Variables);
        Assert.True(deserialized.Variables.ContainsKey("env"));
        Assert.Equal("prod", deserialized.Variables["env"].Default);
    }

    [Fact]
    public void OpenApiServerVariable_EnumCanBeNullInJson()
    {
        // Arrange
        var variable = new OpenApiServerVariable
        {
            Default = "test",
            Description = "Test variable"
            // Enum is not set, should be empty list
        };

        // Act
        var json = JsonSerializer.Serialize(variable);
        var deserialized = JsonSerializer.Deserialize<OpenApiServerVariable>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("test", deserialized.Default);
        Assert.Equal("Test variable", deserialized.Description);
        Assert.NotNull(deserialized.Enum);
        Assert.Empty(deserialized.Enum);
    }

    [Fact]
    public void OpenApiServerVariable_CanHaveEmptyEnum()
    {
        // Arrange
        var variable = new OpenApiServerVariable
        {
            Default = "default",
            Description = "Variable with no enum constraints",
            Enum = [] // Explicitly empty
        };

        // Act & Assert
        Assert.Equal("default", variable.Default);
        Assert.Equal("Variable with no enum constraints", variable.Description);
        Assert.Empty(variable.Enum);
    }

    [Fact]
    public void OpenApiServer_VariablesDictionary_IsCaseSensitive()
    {
        // Arrange
        var server = new OpenApiServer();

        server.Variables["PORT"] = new OpenApiServerVariable { Default = "8080" };
        server.Variables["port"] = new OpenApiServerVariable { Default = "3000" };

        // Act & Assert
        Assert.Equal(2, server.Variables.Count);
        Assert.True(server.Variables.ContainsKey("PORT"));
        Assert.True(server.Variables.ContainsKey("port"));
        Assert.Equal("8080", server.Variables["PORT"].Default);
        Assert.Equal("3000", server.Variables["port"].Default);
    }

    [Fact]
    public void OpenApiServerVariable_CanHaveSpecialCharactersInDefault()
    {
        // Arrange
        var variable = new OpenApiServerVariable
        {
            Default = "special-value_123",
            Description = "Variable with special characters"
        };

        // Act & Assert
        Assert.Equal("special-value_123", variable.Default);
        Assert.Equal("Variable with special characters", variable.Description);
    }

    [Fact]
    public void OpenApiServerVariable_EnumCanHaveDuplicates()
    {
        // Arrange
        var variable = new OpenApiServerVariable
        {
            Default = "a",
            Enum = ["a", "a", "b"]
        };

        // Act & Assert
        Assert.Equal(3, variable.Enum.Count);
        Assert.Equal(2, variable.Enum.Count(x => x == "a"));
        Assert.Single(variable.Enum.Where(x => x == "b"));
    }

    [Fact]
    public void OpenApiServerVariable_CanBeUsedInComplexServerConfiguration()
    {
        // Arrange - Create a complex server configuration
        var server = new OpenApiServer
        {
            Url = "{protocol}://{region}.{environment}.api.example.com:{port}/{version}",
            Description = "Multi-region, multi-environment API server"
        };

        // Protocol variable
        server.Variables["protocol"] = new OpenApiServerVariable
        {
            Default = "https",
            Description = "Communication protocol",
            Enum = ["http", "https"]
        };

        // Region variable
        server.Variables["region"] = new OpenApiServerVariable
        {
            Default = "us-east",
            Description = "AWS region",
            Enum = ["us-east", "us-west", "eu-central"]
        };

        // Environment variable
        server.Variables["environment"] = new OpenApiServerVariable
        {
            Default = "prod",
            Description = "Deployment environment",
            Enum = ["dev", "staging", "prod"]
        };

        // Port variable
        server.Variables["port"] = new OpenApiServerVariable
        {
            Default = "443",
            Description = "Server port"
        };

        // Version variable
        server.Variables["version"] = new OpenApiServerVariable
        {
            Default = "v1",
            Description = "API version",
            Enum = ["v1", "v2"]
        };

        // Act & Assert
        Assert.Equal(5, server.Variables.Count);
        Assert.True(server.Variables.ContainsKey("protocol"));
        Assert.True(server.Variables.ContainsKey("region"));
        Assert.True(server.Variables.ContainsKey("environment"));
        Assert.True(server.Variables.ContainsKey("port"));
        Assert.True(server.Variables.ContainsKey("version"));

        // Check defaults
        Assert.Equal("https", server.Variables["protocol"].Default);
        Assert.Equal("us-east", server.Variables["region"].Default);
        Assert.Equal("prod", server.Variables["environment"].Default);
        Assert.Equal("443", server.Variables["port"].Default);
        Assert.Equal("v1", server.Variables["version"].Default);

        // Check enums
        Assert.Equal(2, server.Variables["protocol"].Enum.Count);
        Assert.Equal(3, server.Variables["region"].Enum.Count);
        Assert.Equal(3, server.Variables["environment"].Enum.Count);
        Assert.Empty(server.Variables["port"].Enum); // No enum for port
        Assert.Equal(2, server.Variables["version"].Enum.Count);
    }
}