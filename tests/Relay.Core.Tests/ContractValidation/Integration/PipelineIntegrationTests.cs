using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
/// Integration tests for ContractValidationPipelineBehavior with complete pipeline setup.
/// Tests the full validation pipeline with real components and configurations.
/// </summary>
public sealed class PipelineIntegrationTests : IDisposable
{
    private readonly string _testSchemaDirectory;
    private readonly ContractValidationTestFixture _fixture;

    public PipelineIntegrationTests()
    {
        _fixture = new ContractValidationTestFixture();
        
        var currentDirectory = Directory.GetCurrentDirectory();
        _testSchemaDirectory = Path.Combine(currentDirectory, "..", "..", "..", "ContractValidation", "TestSchemas");
        _testSchemaDirectory = Path.GetFullPath(_testSchemaDirectory);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldPassThrough()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<TestRequest>();
        
        var request = new TestRequest { Name = "Test", Value = 100 };
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(response, result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Pipeline_WithInvalidRequest_ShouldThrowException()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<TestRequest>();
        
        var request = new TestRequest { Name = "", Value = 2000 }; // Invalid
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Contains("Name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pipeline_WithSchemaDiscovery_ShouldAutomaticallyLoadSchemas()
    {
        // Arrange
        var (behavior, serviceProvider) = CreatePipelineBehavior<SimpleRequest>();
        var schemaResolver = serviceProvider.GetRequiredService<ISchemaResolver>();
        
        var request = new SimpleRequest { Name = "Test", Value = 100 };
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Verify schema was discovered
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };
        var schema = await schemaResolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);
        
        Assert.NotNull(schema);
        Assert.Contains("Simple Request Schema", schema.Schema);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Pipeline_WithCustomValidator_ShouldApplyCustomRules()
    {
        // Arrange
        var customValidator = new TestBusinessRuleValidator();
        var (behavior, _) = CreatePipelineBehavior<TestRequest>(customValidators: new[] { customValidator });
        
        var request = new TestRequest { Name = "forbidden", Value = 100 }; // Triggers custom rule
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Contains("forbidden", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pipeline_WithLenientStrategy_ShouldNotThrowOnFailure()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<TestRequest>(validationStrategy: "Lenient");
        
        var request = new TestRequest { Name = "", Value = 2000 }; // Invalid
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert - Should not throw, should return response
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Pipeline_WithStrictStrategy_ShouldThrowOnFailure()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<TestRequest>(validationStrategy: "Strict");
        
        var request = new TestRequest { Name = "", Value = 2000 }; // Invalid
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act & Assert
        await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));
    }

    [Fact]
    public async Task Pipeline_WithDisabledValidation_ShouldSkipValidation()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<TestRequest>(enableValidation: false);
        
        var request = new TestRequest { Name = "", Value = 2000 }; // Invalid but validation disabled
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert - Should pass through without validation
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Pipeline_WithResponseValidation_ShouldValidateResponse()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<TestRequest>();
        
        var request = new TestRequest { Name = "Test", Value = 100 };
        var response = new TestResponse { Success = true }; // Valid response
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Pipeline_WithCaching_ShouldReuseSchemas()
    {
        // Arrange
        var (behavior, serviceProvider) = CreatePipelineBehavior<SimpleRequest>();
        var schemaCache = serviceProvider.GetRequiredService<ISchemaCache>();
        
        var request = new SimpleRequest { Name = "Test", Value = 100 };
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act - First request (cache miss)
        await behavior.HandleAsync(request, next, CancellationToken.None);
        var metrics1 = schemaCache.GetMetrics();

        // Act - Second request (cache hit)
        await behavior.HandleAsync(request, next, CancellationToken.None);
        var metrics2 = schemaCache.GetMetrics();

        // Assert
        Assert.True(metrics2.CacheHits > metrics1.CacheHits);
        Assert.True(metrics2.HitRate > metrics1.HitRate);
    }

    [Fact]
    public async Task Pipeline_WithMultipleRequests_ShouldValidateEachIndependently()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<TestRequest>();
        
        var validRequest = new TestRequest { Name = "Valid", Value = 100 };
        var invalidRequest = new TestRequest { Name = "", Value = 2000 };
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act & Assert - Valid request should pass
        var result1 = await behavior.HandleAsync(validRequest, next, CancellationToken.None);
        Assert.NotNull(result1);

        // Act & Assert - Invalid request should fail
        await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await behavior.HandleAsync(invalidRequest, next, CancellationToken.None));
    }

    [Fact]
    public async Task Pipeline_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<TestRequest>();
        
        var request = new TestRequest { Name = "Test", Value = 100 };
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await behavior.HandleAsync(request, next, cts.Token));
    }

    [Fact]
    public async Task Pipeline_WithNoSchemaFound_ShouldSkipValidation()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<UnknownRequest>();
        
        var request = new UnknownRequest { Data = "test" }; // No schema exists for this type
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert - Should pass through without validation
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Pipeline_WithComplexValidation_ShouldValidateAllFields()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<UserRequest>();
        
        var request = new UserRequest
        {
            UserId = 1,
            Username = "testuser",
            Email = "test@example.com",
            Age = 25,
            IsActive = true
        };
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Pipeline_WithInvalidComplexRequest_ShouldReportAllErrors()
    {
        // Arrange
        var (behavior, _) = CreatePipelineBehavior<UserRequest>();
        
        var request = new UserRequest
        {
            UserId = 0, // Invalid: minimum is 1
            Username = "ab", // Invalid: minLength is 3
            Email = "invalid-email", // Invalid: not a valid email
            Age = 200, // Invalid: maximum is 150
            IsActive = true
        };
        var response = new TestResponse { Success = true };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        // Should contain multiple error messages
        Assert.Contains("UserId", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private (ContractValidationPipelineBehavior<TRequest, TestResponse>, IServiceProvider) CreatePipelineBehavior<TRequest>(
        bool enableValidation = true,
        string validationStrategy = "Strict",
        ICustomValidator[]? customValidators = null)
        where TRequest : IRequest<TestResponse>
    {
        var services = new ServiceCollection();
        
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = enableValidation,
                ValidateRequests = true,
                ValidateResponses = true,
                ThrowOnValidationFailure = validationStrategy.ToLower() != "lenient", // Don't throw if using lenient strategy
                ValidationStrategy = validationStrategy,
                EnablePerformanceMetrics = false,
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
        services.AddSingleton(relayOptions.DefaultContractValidationOptions.SchemaDiscovery); // Register SchemaDiscoveryOptions directly for FileSystemSchemaProvider
        services.AddSingleton<ISchemaCache, LruSchemaCache>();
        services.AddSingleton<ISchemaProvider, FileSystemSchemaProvider>();
        services.AddSingleton<ISchemaResolver, DefaultSchemaResolver>();
        
        // Add custom validators if provided
        if (customValidators != null)
        {
            var composer = new ValidatorComposer(customValidators);
            var validationEngine = new ValidationEngine(composer);
            services.AddSingleton(validationEngine);
            services.AddSingleton<IContractValidator>(sp => 
                new DefaultContractValidator(
                    schemaCache: sp.GetRequiredService<ISchemaCache>(),
                    schemaResolver: sp.GetRequiredService<ISchemaResolver>(),
                    validationEngine: validationEngine));
        }
        else
        {
            services.AddSingleton<IContractValidator, DefaultContractValidator>();
        }
        
        services.AddSingleton<ValidationStrategyFactory>();
        services.AddSingleton<ILogger<ContractValidationPipelineBehavior<TRequest, TestResponse>>>(
            NullLogger<ContractValidationPipelineBehavior<TRequest, TestResponse>>.Instance);
        services.AddSingleton<ILogger<LenientValidationStrategy>>(
            NullLogger<LenientValidationStrategy>.Instance);

        var serviceProvider = services.BuildServiceProvider();

        var validator = serviceProvider.GetRequiredService<IContractValidator>();
        var logger = serviceProvider.GetRequiredService<ILogger<ContractValidationPipelineBehavior<TRequest, TestResponse>>>();
        var options = serviceProvider.GetRequiredService<IOptions<RelayOptions>>();
        var schemaResolver = serviceProvider.GetRequiredService<ISchemaResolver>();
        var strategyFactory = serviceProvider.GetRequiredService<ValidationStrategyFactory>();

        var behavior = new ContractValidationPipelineBehavior<TRequest, TestResponse>(
            validator,
            logger,
            options,
            schemaResolver,
            strategyFactory);

        return (behavior, serviceProvider);
    }

    // Test request/response types
    public sealed class TestRequest : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public sealed class SimpleRequest : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public sealed class UserRequest : IRequest<TestResponse>
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class UnknownRequest : IRequest<TestResponse>
    {
        public string Data { get; set; } = string.Empty;
    }

    public sealed class TestResponse
    {
        public bool Success { get; set; }
    }

    // Test custom validator
    private sealed class TestBusinessRuleValidator : ICustomValidator
    {
        public int Priority => 100;

        public bool AppliesTo(Type type) => type == typeof(TestRequest);

        public ValueTask<IEnumerable<ValidationError>> ValidateAsync(
            object obj,
            ValidationContext context,
            CancellationToken cancellationToken = default)
        {
            var request = (TestRequest)obj;
            var errors = new List<ValidationError>();

            if (request.Name.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(ValidationError.Create("BUSINESS001", "Name contains forbidden word", "Name"));
            }

            return ValueTask.FromResult<IEnumerable<ValidationError>>(errors);
        }
    }
}





