using System;
using System.Linq;
using System.Text.Json;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.Endpoints;
using Relay.Core.Metadata.OpenApi;
using Xunit;

namespace Relay.Core.Tests.Endpoints;

/// <summary>
/// Tests for OpenAPI document generation with custom options
/// </summary>
public class OpenApiOptionsTests
{
    public OpenApiOptionsTests()
    {
        // Clear registry before each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public void GenerateDocument_WithCustomOptions_UsesCustomValues()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "GET",
            RequestType = typeof(string),
            HandlerType = typeof(OpenApiOptionsTests),
            HandlerMethodName = "TestMethod"
        };

        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        var options = new OpenApiGenerationOptions
        {
            Title = "Custom API",
            Description = "Custom description",
            Version = "2.0.0",
            Contact = new OpenApiContact
            {
                Name = "Test Contact",
                Email = "test@example.com",
                Url = "https://example.com"
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = "https://opensource.org/licenses/MIT"
            }
        };

        options.Servers.Clear();
        options.Servers.Add(new OpenApiServer
        {
            Url = "https://api.example.com",
            Description = "Production server"
        });

        // Act
        var document = OpenApiDocumentGenerator.GenerateDocument(options);

        // Assert
        Assert.Equal("Custom API", document.Info.Title);
        Assert.Equal("Custom description", document.Info.Description);
        Assert.Equal("2.0.0", document.Info.Version);

        Assert.NotNull(document.Info.Contact);
        Assert.Equal("Test Contact", document.Info.Contact.Name);
        Assert.Equal("test@example.com", document.Info.Contact.Email);
        Assert.Equal("https://example.com", document.Info.Contact.Url);

        Assert.NotNull(document.Info.License);
        Assert.Equal("MIT", document.Info.License.Name);
        Assert.Equal("https://opensource.org/licenses/MIT", document.Info.License.Url);

        Assert.Single(document.Servers);
        Assert.Equal("https://api.example.com", document.Servers[0].Url);
        Assert.Equal("Production server", document.Servers[0].Description);
    }
}
