using System;
using System.Linq;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.Configuration;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Configuration;

public class ContractValidationOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValidOptions_ReturnsNoErrors()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationTimeout = TimeSpan.FromSeconds(5),
            MaxErrorCount = 100,
            ValidationStrategy = "Strict"
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithNullOptions_ReturnsError()
    {
        // Act
        var errors = ContractValidationOptionsValidator.Validate(null!).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Contains("cannot be null", errors[0]);
    }

    [Fact]
    public void Validate_WithZeroValidationTimeout_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationTimeout = TimeSpan.Zero
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("ValidationTimeout must be greater than zero"));
    }

    [Fact]
    public void Validate_WithNegativeValidationTimeout_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationTimeout = TimeSpan.FromSeconds(-1)
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("ValidationTimeout must be greater than zero"));
    }

    [Fact]
    public void Validate_WithExcessiveValidationTimeout_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationTimeout = TimeSpan.FromMinutes(10)
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("ValidationTimeout should not exceed 5 minutes"));
    }

    [Fact]
    public void Validate_WithZeroMaxErrorCount_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            MaxErrorCount = 0
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("MaxErrorCount must be greater than zero"));
    }

    [Fact]
    public void Validate_WithNegativeMaxErrorCount_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            MaxErrorCount = -1
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("MaxErrorCount must be greater than zero"));
    }

    [Fact]
    public void Validate_WithExcessiveMaxErrorCount_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            MaxErrorCount = 20000
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("MaxErrorCount should not exceed 10000"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidValidationStrategy_ReturnsError(string? strategy)
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationStrategy = strategy!
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("ValidationStrategy cannot be null or empty"));
    }

    [Fact]
    public void Validate_WithUnknownValidationStrategy_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationStrategy = "Unknown"
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("ValidationStrategy must be one of"));
    }

    [Theory]
    [InlineData("Strict")]
    [InlineData("Lenient")]
    [InlineData("Custom")]
    [InlineData("strict")]
    [InlineData("LENIENT")]
    public void Validate_WithValidValidationStrategy_ReturnsNoError(string strategy)
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationStrategy = strategy
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.DoesNotContain(errors, e => e.Contains("ValidationStrategy"));
    }

    [Fact]
    public void Validate_WithInvalidSchemaCacheMaxSize_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaCache = new SchemaCacheOptions
            {
                MaxCacheSize = 0
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("SchemaCache.MaxCacheSize must be greater than zero"));
    }

    [Fact]
    public void Validate_WithExcessiveSchemaCacheMaxSize_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaCache = new SchemaCacheOptions
            {
                MaxCacheSize = 200000
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("SchemaCache.MaxCacheSize should not exceed 100000"));
    }

    [Fact]
    public void Validate_WithInvalidMetricsReportingInterval_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaCache = new SchemaCacheOptions
            {
                MetricsReportingInterval = TimeSpan.Zero
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("SchemaCache.MetricsReportingInterval must be greater than zero"));
    }

    [Fact]
    public void Validate_WithCacheWarmingEnabledButNoTypes_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaCache = new SchemaCacheOptions
            {
                EnableCacheWarming = true,
                WarmupTypes = new()
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("EnableCacheWarming is true but WarmupTypes is empty"));
    }

    [Fact]
    public void Validate_WithEmptyNamingConvention_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaDiscovery = new SchemaDiscoveryOptions
            {
                NamingConvention = ""
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("SchemaDiscovery.NamingConvention cannot be null or empty"));
    }

    [Fact]
    public void Validate_WithNamingConventionMissingPlaceholder_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaDiscovery = new SchemaDiscoveryOptions
            {
                NamingConvention = "schema.json"
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("NamingConvention must contain the {TypeName} placeholder"));
    }

    [Fact]
    public void Validate_WithInvalidHttpSchemaTimeout_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaDiscovery = new SchemaDiscoveryOptions
            {
                HttpSchemaTimeout = TimeSpan.Zero
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("SchemaDiscovery.HttpSchemaTimeout must be greater than zero"));
    }

    [Fact]
    public void Validate_WithExcessiveHttpSchemaTimeout_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaDiscovery = new SchemaDiscoveryOptions
            {
                HttpSchemaTimeout = TimeSpan.FromMinutes(5)
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("HttpSchemaTimeout should not exceed 1 minute"));
    }

    [Fact]
    public void Validate_WithHttpSchemasEnabledButNoEndpoints_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaDiscovery = new SchemaDiscoveryOptions
            {
                EnableHttpSchemas = true,
                HttpSchemaEndpoints = new()
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("EnableHttpSchemas is true but HttpSchemaEndpoints is empty"));
    }

    [Fact]
    public void Validate_WithInvalidHttpEndpoint_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaDiscovery = new SchemaDiscoveryOptions
            {
                HttpSchemaEndpoints = new() { "not-a-valid-url" }
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("contains invalid URL"));
    }

    [Fact]
    public void Validate_WithValidHttpEndpoint_ReturnsNoError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            SchemaDiscovery = new SchemaDiscoveryOptions
            {
                HttpSchemaEndpoints = new() { "https://example.com/schemas" }
            }
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.DoesNotContain(errors, e => e.Contains("invalid URL"));
    }

    [Fact]
    public void Validate_WithSchemaDiscoveryEnabledButEnhancedValidationDisabled_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = false,
            EnableSchemaDiscovery = true
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("EnableSchemaDiscovery requires EnableEnhancedValidation"));
    }

    [Fact]
    public void Validate_WithCustomValidatorsEnabledButEnhancedValidationDisabled_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            EnableEnhancedValidation = false,
            EnableCustomValidators = true
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("EnableCustomValidators requires EnableEnhancedValidation"));
    }

    [Fact]
    public void Validate_WithBothValidateRequestsAndResponsesDisabled_ReturnsError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidateRequests = false,
            ValidateResponses = false
        };

        // Act
        var errors = ContractValidationOptionsValidator.Validate(options).ToList();

        // Assert
        Assert.Contains(errors, e => e.Contains("At least one of ValidateRequests or ValidateResponses must be true"));
    }

    [Fact]
    public void ValidateAndThrow_WithValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new ContractValidationOptions();

        // Act & Assert
        var exception = Record.Exception(() => ContractValidationOptionsValidator.ValidateAndThrow(options));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateAndThrow_WithInvalidOptions_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationTimeout = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ContractValidationOptionsValidator.ValidateAndThrow(options));
        Assert.Contains("Contract validation configuration is invalid", exception.Message);
    }
}
