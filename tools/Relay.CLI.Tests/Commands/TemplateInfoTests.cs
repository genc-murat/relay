using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Template;

namespace Relay.CLI.Tests.Commands;

public class TemplateInfoTests
{
    [Fact]
    public void TemplateInfo_ShouldHaveId()
    {
        // Arrange & Act
        var template = new TemplateInfo { Id = "test-template" };

        // Assert
        Assert.Equal("test-template", template.Id);
    }

    [Fact]
    public void TemplateInfo_ShouldHaveName()
    {
        // Arrange & Act
        var template = new TemplateInfo { Name = "Test Template" };

        // Assert
        Assert.Equal("Test Template", template.Name);
    }

    [Fact]
    public void TemplateInfo_ShouldHaveDescription()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Description = "A test template for testing purposes"
        };

        // Assert
        Assert.Equal("A test template for testing purposes", template.Description);
    }

    [Fact]
    public void TemplateInfo_ShouldHaveBestFor()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            BestFor = "Testing and development"
        };

        // Assert
        Assert.Equal("Testing and development", template.BestFor);
    }

    [Fact]
    public void TemplateInfo_ShouldHaveTags()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Tags = ["test", "development", "ci"]
        };

        // Assert
        Assert.Equal(3, template.Tags.Length);
        Assert.Contains("test", template.Tags);
        Assert.Contains("development", template.Tags);
        Assert.Contains("ci", template.Tags);
    }

    [Fact]
    public void TemplateInfo_ShouldHaveFeatures()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Features = ["auth", "swagger", "docker"]
        };

        // Assert
        Assert.Equal(3, template.Features.Length);
        Assert.Contains("auth", template.Features);
        Assert.Contains("swagger", template.Features);
        Assert.Contains("docker", template.Features);
    }

    [Fact]
    public void TemplateInfo_ShouldHaveStructure()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Structure = "clean-architecture"
        };

        // Assert
        Assert.Equal("clean-architecture", template.Structure);
    }

    [Fact]
    public void TemplateInfo_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var template = new TemplateInfo();

        // Assert
        Assert.Empty(template.Id);
        Assert.Empty(template.Name);
        Assert.Empty(template.Description);
        Assert.Empty(template.BestFor);
        Assert.Empty(template.Tags);
        Assert.Empty(template.Features);
        Assert.Empty(template.Structure);
    }

    [Fact]
    public void TemplateInfo_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Name = "Clean Architecture Web API",
            Description = "Production-ready REST API",
            BestFor = "Enterprise REST APIs",
            Tags = ["web", "api", "rest"],
            Features = ["auth", "swagger"],
            Structure = "clean-architecture"
        };

        // Assert
        Assert.Equal("relay-webapi", template.Id);
        Assert.Equal("Clean Architecture Web API", template.Name);
        Assert.Equal("Production-ready REST API", template.Description);
        Assert.Equal("Enterprise REST APIs", template.BestFor);
        Assert.Equal(3, template.Tags.Length);
        Assert.Equal(2, template.Features.Length);
        Assert.Equal("clean-architecture", template.Structure);
    }

    [Fact]
    public void TemplateInfo_Tags_CanBeEmpty()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Tags = []
        };

        // Assert
        Assert.Empty(template.Tags);
    }

    [Fact]
    public void TemplateInfo_Features_CanBeEmpty()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Features = []
        };

        // Assert
        Assert.Empty(template.Features);
    }

    [Fact]
    public void TemplateInfo_ShouldSupportMultipleTags()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Tags =
            [
                "microservice",
                "events",
                "messaging",
                "kafka",
                "rabbitmq"
            ]
        };

        // Assert
        Assert.Equal(5, template.Tags.Length);
        Assert.Contains("microservice", template.Tags);
        Assert.Contains("kafka", template.Tags);
    }

    [Fact]
    public void TemplateInfo_ShouldSupportMultipleFeatures()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Features =
            [
                "auth",
                "swagger",
                "docker",
                "tests",
                "healthchecks",
                "logging",
                "monitoring"
            ]
        };

        // Assert
        Assert.Equal(7, template.Features.Length);
        Assert.Contains("healthchecks", template.Features);
        Assert.Contains("monitoring", template.Features);
    }

    [Fact]
    public void TemplateInfo_WithCleanArchitecture_ShouldHaveCorrectStructure()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Structure = "clean-architecture"
        };

        // Assert
        Assert.Equal("clean-architecture", template.Structure);
    }

    [Fact]
    public void TemplateInfo_WithMicroserviceStructure_ShouldHaveCorrectStructure()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-microservice",
            Structure = "microservice"
        };

        // Assert
        Assert.Equal("microservice", template.Structure);
    }

    [Fact]
    public void TemplateInfo_WithModularStructure_ShouldHaveCorrectStructure()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-modular",
            Structure = "modular"
        };

        // Assert
        Assert.Equal("modular", template.Structure);
    }

    [Fact]
    public void TemplateInfo_WebApiTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-webapi",
            Name = "Clean Architecture Web API",
            Description = "Production-ready REST API following Clean Architecture principles",
            BestFor = "Enterprise REST APIs, Backend services",
            Tags = ["web", "api", "rest", "clean-architecture"],
            Features = ["auth", "swagger", "docker", "tests", "healthchecks"],
            Structure = "clean-architecture"
        };

        // Assert
        Assert.Equal("relay-webapi", template.Id);
        Assert.Contains("web", template.Tags);
        Assert.Contains("api", template.Tags);
        Assert.Contains("swagger", template.Features);
        Assert.Equal("clean-architecture", template.Structure);
    }

    [Fact]
    public void TemplateInfo_MicroserviceTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-microservice",
            Name = "Event-Driven Microservice",
            Description = "Microservice template with message broker integration",
            BestFor = "Microservices architecture, Event-driven systems",
            Tags = ["microservice", "events", "messaging"],
            Features = ["rabbitmq", "kafka", "k8s", "docker", "tracing"],
            Structure = "microservice"
        };

        // Assert
        Assert.Equal("relay-microservice", template.Id);
        Assert.Contains("microservice", template.Tags);
        Assert.Contains("rabbitmq", template.Features);
        Assert.Contains("kafka", template.Features);
        Assert.Equal("microservice", template.Structure);
    }

    [Fact]
    public void TemplateInfo_DddTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-ddd",
            Name = "Domain-Driven Design",
            Description = "DDD tactical patterns with Relay",
            BestFor = "Complex business domains, Enterprise applications",
            Tags = ["ddd", "domain", "enterprise"],
            Features = ["aggregates", "events", "specifications"],
            Structure = "clean-architecture"
        };

        // Assert
        Assert.Equal("relay-ddd", template.Id);
        Assert.Contains("ddd", template.Tags);
        Assert.Contains("aggregates", template.Features);
    }

    [Fact]
    public void TemplateInfo_CqrsEsTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-cqrs-es",
            Name = "CQRS + Event Sourcing",
            Description = "Complete CQRS with Event Sourcing implementation",
            BestFor = "Systems requiring full audit trail, Financial applications",
            Tags = ["cqrs", "event-sourcing", "audit"],
            Features = ["eventstore", "projections", "snapshots"],
            Structure = "clean-architecture"
        };

        // Assert
        Assert.Equal("relay-cqrs-es", template.Id);
        Assert.Contains("cqrs", template.Tags);
        Assert.Contains("event-sourcing", template.Tags);
        Assert.Contains("eventstore", template.Features);
    }

    [Fact]
    public void TemplateInfo_GraphQLTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-graphql",
            Name = "GraphQL API",
            Description = "GraphQL API with Hot Chocolate",
            BestFor = "Flexible APIs, Mobile/SPA backends",
            Tags = ["graphql", "api", "hotchocolate"],
            Features = ["subscriptions", "dataloader", "filtering"],
            Structure = "clean-architecture"
        };

        // Assert
        Assert.Equal("relay-graphql", template.Id);
        Assert.Contains("graphql", template.Tags);
        Assert.Contains("subscriptions", template.Features);
    }

    [Fact]
    public void TemplateInfo_GrpcTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-grpc",
            Name = "gRPC Service",
            Description = "High-performance gRPC service",
            BestFor = "Microservice communication, High-performance APIs",
            Tags = ["grpc", "protobuf", "performance"],
            Features = ["streaming", "tls", "discovery"],
            Structure = "microservice"
        };

        // Assert
        Assert.Equal("relay-grpc", template.Id);
        Assert.Contains("grpc", template.Tags);
        Assert.Contains("streaming", template.Features);
    }

    [Fact]
    public void TemplateInfo_ServerlessTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-serverless",
            Name = "Serverless Functions",
            Description = "AWS Lambda / Azure Functions template",
            BestFor = "Serverless applications, Cost-sensitive workloads",
            Tags = ["serverless", "lambda", "functions"],
            Features = ["aws", "azure", "api-gateway"],
            Structure = "simple"
        };

        // Assert
        Assert.Equal("relay-serverless", template.Id);
        Assert.Contains("serverless", template.Tags);
        Assert.Contains("aws", template.Features);
        Assert.Equal("simple", template.Structure);
    }

    [Fact]
    public void TemplateInfo_BlazorTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-blazor",
            Name = "Blazor Application",
            Description = "Full-stack Blazor app with Relay",
            BestFor = "Full-stack .NET applications, Internal tools",
            Tags = ["blazor", "spa", "fullstack"],
            Features = ["server", "wasm", "signalr", "pwa"],
            Structure = "clean-architecture"
        };

        // Assert
        Assert.Equal("relay-blazor", template.Id);
        Assert.Contains("blazor", template.Tags);
        Assert.Contains("wasm", template.Features);
    }

    [Fact]
    public void TemplateInfo_MauiTemplate_ShouldHaveExpectedProperties()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "relay-maui",
            Name = "MAUI Mobile App",
            Description = "Cross-platform mobile app",
            BestFor = "Mobile applications, Cross-platform apps",
            Tags = ["maui", "mobile", "cross-platform"],
            Features = ["ios", "android", "offline", "sqlite"],
            Structure = "mvvm"
        };

        // Assert
        Assert.Equal("relay-maui", template.Id);
        Assert.Contains("mobile", template.Tags);
        Assert.Contains("ios", template.Features);
        Assert.Contains("android", template.Features);
        Assert.Equal("mvvm", template.Structure);
    }

    [Fact]
    public void TemplateInfo_ShouldBeReferenceType()
    {
        // Arrange & Act
        var template1 = new TemplateInfo { Id = "test" };
        var template2 = template1;
        template2.Id = "modified";

        // Assert
        Assert.Equal("modified", template1.Id);
    }

    [Fact]
    public void TemplateInfo_Arrays_ShouldNotBeNull()
    {
        // Arrange & Act
        var template = new TemplateInfo();

        // Assert
        Assert.NotNull(template.Tags);
        Assert.NotNull(template.Features);
    }

    [Fact]
    public void TemplateInfo_CanCheckIfFeatureExists()
    {
        // Arrange
        var template = new TemplateInfo
        {
            Features = ["auth", "swagger", "docker"]
        };

        // Act
        var hasAuth = template.Features.Contains("auth");
        var hasGraphQL = template.Features.Contains("graphql");

        // Assert
        Assert.True(hasAuth);
        Assert.False(hasGraphQL);
    }

    [Fact]
    public void TemplateInfo_CanCheckIfTagExists()
    {
        // Arrange
        var template = new TemplateInfo
        {
            Tags = ["web", "api", "rest"]
        };

        // Act
        var isWeb = template.Tags.Contains("web");
        var isMobile = template.Tags.Contains("mobile");

        // Assert
        Assert.True(isWeb);
        Assert.False(isMobile);
    }

    [Fact]
    public void TemplateInfo_WithNoFeatures_ShouldHaveEmptyArray()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "minimal-template",
            Features = []
        };

        // Assert
        Assert.Empty(template.Features);
        Assert.Empty(template.Features);
    }

    [Fact]
    public void TemplateInfo_Description_CanBeMultiline()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Description = @"This is a multi-line description.
It can contain multiple sentences.
And explain the template in detail."
        };

        // Assert
        Assert.Contains("multi-line", template.Description);
        Assert.Contains("multiple sentences", template.Description);
    }

    [Fact]
    public void TemplateInfo_BestFor_CanContainMultipleSuggestions()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            BestFor = "Enterprise REST APIs, Backend services, Microservices"
        };

        // Assert
        Assert.Contains("Enterprise REST APIs", template.BestFor);
        Assert.Contains("Backend services", template.BestFor);
        Assert.Contains("Microservices", template.BestFor);
    }
}
