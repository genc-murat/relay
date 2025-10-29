using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.ContractValidation;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.CustomValidators;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.ContractValidation.Strategies;
using Relay.Core.ContractValidation.Testing;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Integration;

/// <summary>
/// End-to-end integration tests for the contract validation system.
/// Tests the complete validation pipeline with real schemas and components.
/// </summary>
public sealed class ContractValidationIntegrationTests : IDisposable
{
    private readonly string _testSchemaDirectory;
    private readonly ContractValidationTestFixture _fixture;

    public ContractValidationIntegrationTests()
    {
        _fixture = new ContractValidationTestFixture();
        
        // Use the existing TestSchemas directory
        var currentDirectory = Directory.GetCurrentDirectory();
        _testSchemaDirectory = Path.Combine(currentDirectory, "ContractValidation", "TestSchemas");
        
        // Normalize path
        _testSchemaDirectory = Path.GetFullPath(_testSchemaDirectory);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task EndToEnd_ValidRequest_ShouldPassValidation()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        var schemaProvider = CreateFileSystemSchemaProvider();
        var schemaResolver = new DefaultSchemaResolver(new[] { schemaProvider }, schemaCache);
        var validator = _fixture.CreateValidatorWithComponents(schemaCache, schemaResolver);

        var request = new SimpleRequest
        {
            Name = "Test User",
            Value = 42
        };

        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };
        var schema = await schemaResolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task EndToEnd_InvalidRequest_ShouldReturnValidationErrors()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var schema = _fixture.CreateTestSchema(@"{
            ""type"": ""object"",
            ""properties"": {
                ""Name"": { ""type"": ""string"" },
                ""Value"": { ""type"": ""integer"" }
            },
            ""required"": [""Name"", ""Value"", ""MissingProperty""]
        }");

        var request = new { Name = "Test", Value = 42 }; // Missing required property

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task EndToEnd_ComplexRequest_WithNestedValidation_ShouldValidateCorrectly()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        var schemaProvider = CreateFileSystemSchemaProvider();
        var schemaResolver = new DefaultSchemaResolver(new[] { schemaProvider }, schemaCache);
        var validator = _fixture.CreateValidatorWithComponents(schemaCache, schemaResolver);

        var request = new UserTestRequest
        {
            UserId = 1,
            Username = "testuser",
            Email = "test@example.com",
            Age = 25,
            IsActive = true
        };

        var context = new SchemaContext { RequestType = typeof(UserTestRequest), IsRequest = true };
        var schema = await schemaResolver.ResolveSchemaAsync(typeof(UserTestRequest), context, CancellationToken.None);

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task EndToEnd_WithCustomValidator_ShouldApplyBothSchemaAndCustomValidation()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        var schemaProvider = CreateFileSystemSchemaProvider();
        var schemaResolver = new DefaultSchemaResolver(new[] { schemaProvider }, schemaCache);
        
        // Create custom validator that checks for specific business rules
        var customValidator = _fixture.CreateTestValidator(
            appliesTo: type => type == typeof(UserTestRequest),
            validateFunc: (obj, ctx) =>
            {
                var user = (UserTestRequest)obj;
                var errors = new List<ValidationError>();
                
                if (user.Username.Contains("admin"))
                {
                    errors.Add(ValidationError.Create("CUSTOM001", "Username cannot contain 'admin'", "Username"));
                }
                
                return errors;
            },
            priority: 100);

        var validationEngine = _fixture.CreateValidationEngine(customValidator);
        var validator = _fixture.CreateValidatorWithComponents(schemaCache, schemaResolver, validationEngine);

        var request = new UserTestRequest
        {
            UserId = 1,
            Username = "admin_user", // Should fail custom validation
            Email = "test@example.com",
            Age = 25,
            IsActive = true
        };

        var context = new SchemaContext { RequestType = typeof(UserTestRequest), IsRequest = true };
        var schema = await schemaResolver.ResolveSchemaAsync(typeof(UserTestRequest), context, CancellationToken.None);

        // Act
        var validationContext = ValidationContext.ForRequest(typeof(UserTestRequest), request, schema);
        var result = await validationEngine.ValidateAsync(request, validationContext, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "CUSTOM001");
    }

    [Fact]
    public async Task EndToEnd_WithCaching_ShouldReuseSchemas()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        var schemaProvider = CreateFileSystemSchemaProvider();
        var schemaResolver = new DefaultSchemaResolver(new[] { schemaProvider }, schemaCache);
        var validator = _fixture.CreateValidatorWithComponents(schemaCache, schemaResolver);

        var context = new SchemaContext { RequestType = typeof(SimpleTestRequest), IsRequest = true };

        // Act - First resolution (cache miss)
        var schema1 = await schemaResolver.ResolveSchemaAsync(typeof(SimpleTestRequest), context, CancellationToken.None);
        var metrics1 = schemaCache.GetMetrics();
        
        // Act - Second resolution (cache hit)
        var schema2 = await schemaResolver.ResolveSchemaAsync(typeof(SimpleTestRequest), context, CancellationToken.None);
        var metrics2 = schemaCache.GetMetrics();

        // Assert
        Assert.NotNull(schema1);
        Assert.NotNull(schema2);
        
        // Compare schemas semantically rather than as strings to avoid formatting differences
        var options = new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        var schema1Json = System.Text.Json.JsonDocument.Parse(schema1.Schema);
        var schema2Json = System.Text.Json.JsonDocument.Parse(schema2.Schema);
        
        var normalizedSchema1 = System.Text.Json.JsonSerializer.Serialize(schema1Json.RootElement, options);
        var normalizedSchema2 = System.Text.Json.JsonSerializer.Serialize(schema2Json.RootElement, options);
        
        Assert.Equal(normalizedSchema1, normalizedSchema2);
        
        // Verify cache metrics improved
        Assert.True(metrics2.CacheHits > metrics1.CacheHits);
        Assert.True(metrics2.HitRate > metrics1.HitRate);
    }

    [Fact]
    public async Task EndToEnd_WithPipelineBehavior_ShouldValidateInPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Configure validation options
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ValidateResponses = true,
                ThrowOnValidationFailure = true,
                ValidationStrategy = "Strict",
                SchemaDiscovery = new SchemaDiscoveryOptions
                {
                    SchemaDirectories = new List<string> { _testSchemaDirectory },
                    NamingConvention = "{TypeName}.schema.json"
                }
            }
        };

        services.AddSingleton(Options.Create(relayOptions));
        services.AddSingleton(Options.Create(relayOptions.DefaultContractValidationOptions.SchemaCache));
        services.AddSingleton(Options.Create(relayOptions.DefaultContractValidationOptions.SchemaDiscovery));
        services.AddSingleton(relayOptions.DefaultContractValidationOptions.SchemaDiscovery);
        services.AddSingleton<ISchemaCache, LruSchemaCache>();
        services.AddSingleton<ISchemaProvider, FileSystemSchemaProvider>();
        services.AddSingleton<ISchemaResolver, DefaultSchemaResolver>();
        services.AddSingleton<IContractValidator, DefaultContractValidator>();
        services.AddSingleton<ValidationStrategyFactory>();
        services.AddSingleton<ILogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>>(
            NullLogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>.Instance);

        var serviceProvider = services.BuildServiceProvider();

        var validator = serviceProvider.GetRequiredService<IContractValidator>();
        var logger = serviceProvider.GetRequiredService<ILogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>>();
        var options = serviceProvider.GetRequiredService<IOptions<RelayOptions>>();
        var schemaResolver = serviceProvider.GetRequiredService<ISchemaResolver>();
        var strategyFactory = serviceProvider.GetRequiredService<ValidationStrategyFactory>();

        var behavior = new ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>(
            validator,
            logger,
            options,
            schemaResolver,
            strategyFactory);

        var request = new SimpleTestRequest
        {
            Name = "Test",
            Value = 100
        };

        var response = new SimpleTestResponse { Success = true };
        var next = new RequestHandlerDelegate<SimpleTestResponse>(() => new ValueTask<SimpleTestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(response, result);
    }

    [Fact]
    public async Task EndToEnd_WithInvalidRequest_InPipeline_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ValidateResponses = true,
                ThrowOnValidationFailure = true,
                ValidationStrategy = "Strict",
                SchemaDiscovery = new SchemaDiscoveryOptions
                {
                    SchemaDirectories = new List<string> { _testSchemaDirectory },
                    NamingConvention = "{TypeName}.schema.json"
                }
            }
        };

        services.AddSingleton(Options.Create(relayOptions));
        services.AddSingleton(Options.Create(relayOptions.DefaultContractValidationOptions.SchemaCache));
        services.AddSingleton(Options.Create(relayOptions.DefaultContractValidationOptions.SchemaDiscovery));
        services.AddSingleton(relayOptions.DefaultContractValidationOptions.SchemaDiscovery);
        services.AddSingleton<ISchemaCache, LruSchemaCache>();
        services.AddSingleton<ISchemaProvider, FileSystemSchemaProvider>();
        services.AddSingleton<ISchemaResolver, DefaultSchemaResolver>();
        services.AddSingleton<IContractValidator, DefaultContractValidator>();
        services.AddSingleton<ValidationStrategyFactory>();
        services.AddSingleton<ILogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>>(
            NullLogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>.Instance);

        var serviceProvider = services.BuildServiceProvider();

        var validator = serviceProvider.GetRequiredService<IContractValidator>();
        var logger = serviceProvider.GetRequiredService<ILogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>>();
        var options = serviceProvider.GetRequiredService<IOptions<RelayOptions>>();
        var schemaResolver = serviceProvider.GetRequiredService<ISchemaResolver>();
        var strategyFactory = serviceProvider.GetRequiredService<ValidationStrategyFactory>();

        var behavior = new ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>(
            validator,
            logger,
            options,
            schemaResolver,
            strategyFactory);

        var request = new SimpleTestRequest
        {
            Name = "", // Invalid
            Value = 2000 // Invalid
        };

        var response = new SimpleTestResponse { Success = true };
        var next = new RequestHandlerDelegate<SimpleTestResponse>(() => new ValueTask<SimpleTestResponse>(response));

        // Act & Assert
        await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));
    }

    [Fact]
    public async Task EndToEnd_WithLenientStrategy_ShouldNotThrowOnValidationFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ValidateResponses = true,
                ThrowOnValidationFailure = false,
                ValidationStrategy = "Lenient",
                SchemaDiscovery = new SchemaDiscoveryOptions
                {
                    SchemaDirectories = new List<string> { _testSchemaDirectory },
                    NamingConvention = "{TypeName}.schema.json"
                }
            }
        };

        services.AddSingleton(Options.Create(relayOptions));
        services.AddSingleton(Options.Create(relayOptions.DefaultContractValidationOptions.SchemaCache));
        services.AddSingleton(Options.Create(relayOptions.DefaultContractValidationOptions.SchemaDiscovery));
        services.AddSingleton(relayOptions.DefaultContractValidationOptions.SchemaDiscovery);
        services.AddSingleton<ISchemaCache, LruSchemaCache>();
        services.AddSingleton<ISchemaProvider, FileSystemSchemaProvider>();
        services.AddSingleton<ISchemaResolver, DefaultSchemaResolver>();
        services.AddSingleton<IContractValidator, DefaultContractValidator>();
        services.AddSingleton<ValidationStrategyFactory>();
        services.AddSingleton<ILogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>>(
            NullLogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>.Instance);
        services.AddSingleton<ILogger<LenientValidationStrategy>>(
            NullLogger<LenientValidationStrategy>.Instance);

        var serviceProvider = services.BuildServiceProvider();

        var validator = serviceProvider.GetRequiredService<IContractValidator>();
        var logger = serviceProvider.GetRequiredService<ILogger<ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>>>();
        var options = serviceProvider.GetRequiredService<IOptions<RelayOptions>>();
        var schemaResolver = serviceProvider.GetRequiredService<ISchemaResolver>();
        var strategyFactory = serviceProvider.GetRequiredService<ValidationStrategyFactory>();

        var behavior = new ContractValidationPipelineBehavior<SimpleTestRequest, SimpleTestResponse>(
            validator,
            logger,
            options,
            schemaResolver,
            strategyFactory);

        var request = new SimpleTestRequest
        {
            Name = "", // Invalid
            Value = 2000 // Invalid
        };

        var response = new SimpleTestResponse { Success = true };
        var next = new RequestHandlerDelegate<SimpleTestResponse>(() => new ValueTask<SimpleTestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert - Should not throw, should return response
        Assert.NotNull(result);
        Assert.Equal(response, result);
    }

    private FileSystemSchemaProvider CreateFileSystemSchemaProvider()
    {
        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        return new FileSystemSchemaProvider(options);
    }

    private void ConfigureServices(ServiceCollection services, RelayOptions relayOptions)
    {
        services.AddSingleton(Options.Create(relayOptions));
        services.AddSingleton(Options.Create(relayOptions.DefaultContractValidationOptions.SchemaCache));
        services.AddSingleton(Options.Create(relayOptions.DefaultContractValidationOptions.SchemaDiscovery));
        services.AddSingleton(relayOptions.DefaultContractValidationOptions.SchemaDiscovery);
    }

    // Test request/response types
    public sealed class SimpleRequest : IRequest<SimpleResponse>
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public sealed class SimpleResponse
    {
        public bool Success { get; set; }
    }

    public sealed class SimpleTestRequest : IRequest<SimpleTestResponse>
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public sealed class SimpleTestResponse
    {
        public bool Success { get; set; }
    }

    public sealed class UserTestRequest : IRequest<UserTestResponse>
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class UserTestResponse
    {
        public bool Success { get; set; }
    }
}




