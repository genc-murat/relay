using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class TemplateInfoTests
{
    [Fact]
    public void TemplateInfo_ShouldHaveId()
    {
        // Arrange & Act
        var template = new TemplateInfo { Id = "test-template" };

        // Assert
        template.Id.Should().Be("test-template");
    }

    [Fact]
    public void TemplateInfo_ShouldHaveName()
    {
        // Arrange & Act
        var template = new TemplateInfo { Name = "Test Template" };

        // Assert
        template.Name.Should().Be("Test Template");
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
        template.Description.Should().Be("A test template for testing purposes");
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
        template.BestFor.Should().Be("Testing and development");
    }

    [Fact]
    public void TemplateInfo_ShouldHaveTags()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Tags = new[] { "test", "development", "ci" }
        };

        // Assert
        template.Tags.Should().HaveCount(3);
        template.Tags.Should().Contain("test");
        template.Tags.Should().Contain("development");
        template.Tags.Should().Contain("ci");
    }

    [Fact]
    public void TemplateInfo_ShouldHaveFeatures()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Features = new[] { "auth", "swagger", "docker" }
        };

        // Assert
        template.Features.Should().HaveCount(3);
        template.Features.Should().Contain("auth");
        template.Features.Should().Contain("swagger");
        template.Features.Should().Contain("docker");
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
        template.Structure.Should().Be("clean-architecture");
    }

    [Fact]
    public void TemplateInfo_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var template = new TemplateInfo();

        // Assert
        template.Id.Should().BeEmpty();
        template.Name.Should().BeEmpty();
        template.Description.Should().BeEmpty();
        template.BestFor.Should().BeEmpty();
        template.Tags.Should().BeEmpty();
        template.Features.Should().BeEmpty();
        template.Structure.Should().BeEmpty();
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
            Tags = new[] { "web", "api", "rest" },
            Features = new[] { "auth", "swagger" },
            Structure = "clean-architecture"
        };

        // Assert
        template.Id.Should().Be("relay-webapi");
        template.Name.Should().Be("Clean Architecture Web API");
        template.Description.Should().Be("Production-ready REST API");
        template.BestFor.Should().Be("Enterprise REST APIs");
        template.Tags.Should().HaveCount(3);
        template.Features.Should().HaveCount(2);
        template.Structure.Should().Be("clean-architecture");
    }

    [Fact]
    public void TemplateInfo_Tags_CanBeEmpty()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Tags = Array.Empty<string>()
        };

        // Assert
        template.Tags.Should().BeEmpty();
    }

    [Fact]
    public void TemplateInfo_Features_CanBeEmpty()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Features = Array.Empty<string>()
        };

        // Assert
        template.Features.Should().BeEmpty();
    }

    [Fact]
    public void TemplateInfo_ShouldSupportMultipleTags()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Tags = new[]
            {
                "microservice",
                "events",
                "messaging",
                "kafka",
                "rabbitmq"
            }
        };

        // Assert
        template.Tags.Should().HaveCount(5);
        template.Tags.Should().Contain("microservice");
        template.Tags.Should().Contain("kafka");
    }

    [Fact]
    public void TemplateInfo_ShouldSupportMultipleFeatures()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Features = new[]
            {
                "auth",
                "swagger",
                "docker",
                "tests",
                "healthchecks",
                "logging",
                "monitoring"
            }
        };

        // Assert
        template.Features.Should().HaveCount(7);
        template.Features.Should().Contain("healthchecks");
        template.Features.Should().Contain("monitoring");
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
        template.Structure.Should().Be("clean-architecture");
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
        template.Structure.Should().Be("microservice");
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
        template.Structure.Should().Be("modular");
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
            Tags = new[] { "web", "api", "rest", "clean-architecture" },
            Features = new[] { "auth", "swagger", "docker", "tests", "healthchecks" },
            Structure = "clean-architecture"
        };

        // Assert
        template.Id.Should().Be("relay-webapi");
        template.Tags.Should().Contain("web");
        template.Tags.Should().Contain("api");
        template.Features.Should().Contain("swagger");
        template.Structure.Should().Be("clean-architecture");
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
            Tags = new[] { "microservice", "events", "messaging" },
            Features = new[] { "rabbitmq", "kafka", "k8s", "docker", "tracing" },
            Structure = "microservice"
        };

        // Assert
        template.Id.Should().Be("relay-microservice");
        template.Tags.Should().Contain("microservice");
        template.Features.Should().Contain("rabbitmq");
        template.Features.Should().Contain("kafka");
        template.Structure.Should().Be("microservice");
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
            Tags = new[] { "ddd", "domain", "enterprise" },
            Features = new[] { "aggregates", "events", "specifications" },
            Structure = "clean-architecture"
        };

        // Assert
        template.Id.Should().Be("relay-ddd");
        template.Tags.Should().Contain("ddd");
        template.Features.Should().Contain("aggregates");
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
            Tags = new[] { "cqrs", "event-sourcing", "audit" },
            Features = new[] { "eventstore", "projections", "snapshots" },
            Structure = "clean-architecture"
        };

        // Assert
        template.Id.Should().Be("relay-cqrs-es");
        template.Tags.Should().Contain("cqrs");
        template.Tags.Should().Contain("event-sourcing");
        template.Features.Should().Contain("eventstore");
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
            Tags = new[] { "graphql", "api", "hotchocolate" },
            Features = new[] { "subscriptions", "dataloader", "filtering" },
            Structure = "clean-architecture"
        };

        // Assert
        template.Id.Should().Be("relay-graphql");
        template.Tags.Should().Contain("graphql");
        template.Features.Should().Contain("subscriptions");
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
            Tags = new[] { "grpc", "protobuf", "performance" },
            Features = new[] { "streaming", "tls", "discovery" },
            Structure = "microservice"
        };

        // Assert
        template.Id.Should().Be("relay-grpc");
        template.Tags.Should().Contain("grpc");
        template.Features.Should().Contain("streaming");
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
            Tags = new[] { "serverless", "lambda", "functions" },
            Features = new[] { "aws", "azure", "api-gateway" },
            Structure = "simple"
        };

        // Assert
        template.Id.Should().Be("relay-serverless");
        template.Tags.Should().Contain("serverless");
        template.Features.Should().Contain("aws");
        template.Structure.Should().Be("simple");
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
            Tags = new[] { "blazor", "spa", "fullstack" },
            Features = new[] { "server", "wasm", "signalr", "pwa" },
            Structure = "clean-architecture"
        };

        // Assert
        template.Id.Should().Be("relay-blazor");
        template.Tags.Should().Contain("blazor");
        template.Features.Should().Contain("wasm");
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
            Tags = new[] { "maui", "mobile", "cross-platform" },
            Features = new[] { "ios", "android", "offline", "sqlite" },
            Structure = "mvvm"
        };

        // Assert
        template.Id.Should().Be("relay-maui");
        template.Tags.Should().Contain("mobile");
        template.Features.Should().Contain("ios");
        template.Features.Should().Contain("android");
        template.Structure.Should().Be("mvvm");
    }

    [Fact]
    public void TemplateInfo_ShouldBeReferenceType()
    {
        // Arrange & Act
        var template1 = new TemplateInfo { Id = "test" };
        var template2 = template1;
        template2.Id = "modified";

        // Assert
        template1.Id.Should().Be("modified");
    }

    [Fact]
    public void TemplateInfo_Arrays_ShouldNotBeNull()
    {
        // Arrange & Act
        var template = new TemplateInfo();

        // Assert
        template.Tags.Should().NotBeNull();
        template.Features.Should().NotBeNull();
    }

    [Fact]
    public void TemplateInfo_CanCheckIfFeatureExists()
    {
        // Arrange
        var template = new TemplateInfo
        {
            Features = new[] { "auth", "swagger", "docker" }
        };

        // Act
        var hasAuth = template.Features.Contains("auth");
        var hasGraphQL = template.Features.Contains("graphql");

        // Assert
        hasAuth.Should().BeTrue();
        hasGraphQL.Should().BeFalse();
    }

    [Fact]
    public void TemplateInfo_CanCheckIfTagExists()
    {
        // Arrange
        var template = new TemplateInfo
        {
            Tags = new[] { "web", "api", "rest" }
        };

        // Act
        var isWeb = template.Tags.Contains("web");
        var isMobile = template.Tags.Contains("mobile");

        // Assert
        isWeb.Should().BeTrue();
        isMobile.Should().BeFalse();
    }

    [Fact]
    public void TemplateInfo_WithNoFeatures_ShouldHaveEmptyArray()
    {
        // Arrange & Act
        var template = new TemplateInfo
        {
            Id = "minimal-template",
            Features = Array.Empty<string>()
        };

        // Assert
        template.Features.Should().BeEmpty();
        template.Features.Any().Should().BeFalse();
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
        template.Description.Should().Contain("multi-line");
        template.Description.Should().Contain("multiple sentences");
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
        template.BestFor.Should().Contain("Enterprise REST APIs");
        template.BestFor.Should().Contain("Backend services");
        template.BestFor.Should().Contain("Microservices");
    }
}
