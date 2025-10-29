using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.Configuration;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Configuration;

public class ContractValidationFeatureFlagsTests
{
    [Fact]
    public void IsEnhancedValidationEnabled_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions { EnableEnhancedValidation = true };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsEnhancedValidationEnabled);
    }

    [Fact]
    public void IsEnhancedValidationEnabled_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var options = new ContractValidationOptions { EnableEnhancedValidation = false };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.False(flags.IsEnhancedValidationEnabled);
    }

    [Fact]
    public void IsSchemaDiscoveryEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            EnableSchemaDiscovery = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsSchemaDiscoveryEnabled);
    }

    [Fact]
    public void IsSchemaDiscoveryEnabled_WhenEnhancedValidationDisabled_ReturnsFalse()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = false,
            EnableSchemaDiscovery = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.False(flags.IsSchemaDiscoveryEnabled);
    }

    [Fact]
    public void IsCustomValidatorsEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            EnableCustomValidators = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsCustomValidatorsEnabled);
    }

    [Fact]
    public void IsCustomValidatorsEnabled_WhenEnhancedValidationDisabled_ReturnsFalse()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = false,
            EnableCustomValidators = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.False(flags.IsCustomValidatorsEnabled);
    }

    [Fact]
    public void IsDetailedErrorsEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            EnableDetailedErrors = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsDetailedErrorsEnabled);
    }

    [Fact]
    public void IsSuggestedFixesEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            EnableSuggestedFixes = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsSuggestedFixesEnabled);
    }

    [Fact]
    public void IsPerformanceMetricsEnabled_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions { EnablePerformanceMetrics = true };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsPerformanceMetricsEnabled);
    }

    [Fact]
    public void IsValidationResultCachingEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            EnableValidationResultCaching = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsValidationResultCachingEnabled);
    }

    [Fact]
    public void IsSchemaCachingEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            SchemaCache = new SchemaCacheOptions { EnableMetrics = true }
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsSchemaCachingEnabled);
    }

    [Fact]
    public void IsCacheWarmingEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            SchemaCache = new SchemaCacheOptions { EnableCacheWarming = true }
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsCacheWarmingEnabled);
    }

    [Fact]
    public void IsFileSystemWatcherEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            SchemaDiscovery = new SchemaDiscoveryOptions { EnableFileSystemWatcher = true }
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsFileSystemWatcherEnabled);
    }

    [Fact]
    public void IsHttpSchemasEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            SchemaDiscovery = new SchemaDiscoveryOptions { EnableHttpSchemas = true }
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsHttpSchemasEnabled);
    }

    [Fact]
    public void IsEmbeddedResourcesEnabled_WhenBothEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = true,
            SchemaDiscovery = new SchemaDiscoveryOptions { EnableEmbeddedResources = true }
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.IsEmbeddedResourcesEnabled);
    }

    [Fact]
    public void ShouldValidate_ForRequest_WhenAllEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableAutomaticContractValidation = true,
            ValidateRequests = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.ShouldValidate(isRequest: true));
    }

    [Fact]
    public void ShouldValidate_ForResponse_WhenAllEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableAutomaticContractValidation = true,
            ValidateResponses = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.ShouldValidate(isRequest: false));
    }

    [Fact]
    public void ShouldValidate_WhenAutomaticValidationDisabled_ReturnsFalse()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableAutomaticContractValidation = false,
            ValidateRequests = true,
            ValidateResponses = true
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.False(flags.ShouldValidate(isRequest: true));
        Assert.False(flags.ShouldValidate(isRequest: false));
    }

    [Fact]
    public void ShouldValidate_ForRequest_WhenValidateRequestsDisabled_ReturnsFalse()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableAutomaticContractValidation = true,
            ValidateRequests = false
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.False(flags.ShouldValidate(isRequest: true));
    }

    [Fact]
    public void ShouldValidate_ForResponse_WhenValidateResponsesDisabled_ReturnsFalse()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableAutomaticContractValidation = true,
            ValidateResponses = false
        };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.False(flags.ShouldValidate(isRequest: false));
    }

    [Fact]
    public void UseLegacyBehavior_WhenEnhancedValidationDisabled_ReturnsTrue()
    {
        // Arrange
        var options = new ContractValidationOptions { EnableEnhancedValidation = false };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.True(flags.UseLegacyBehavior);
    }

    [Fact]
    public void UseLegacyBehavior_WhenEnhancedValidationEnabled_ReturnsFalse()
    {
        // Arrange
        var options = new ContractValidationOptions { EnableEnhancedValidation = true };
        var flags = new ContractValidationFeatureFlags(options);

        // Act & Assert
        Assert.False(flags.UseLegacyBehavior);
    }
}
