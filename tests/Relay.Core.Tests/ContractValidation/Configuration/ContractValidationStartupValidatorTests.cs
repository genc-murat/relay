using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.Configuration;
using Relay.Core.ContractValidation.SchemaDiscovery;
using System;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Configuration;

public class ContractValidationStartupValidatorTests
{
    private readonly Mock<IOptions<ContractValidationOptions>> _mockOptions;
    private readonly Mock<ILogger<ContractValidationStartupValidator>> _mockLogger;

    public ContractValidationStartupValidatorTests()
    {
        _mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        _mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ContractValidationStartupValidator(null, mockLogger.Object));
        
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        // Set up the mock to return a mock ContractValidationOptions
        var options = new ContractValidationOptions();
        mockOptions.Setup(o => o.Value).Returns(options);
        
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ContractValidationStartupValidator(mockOptions.Object, null));
        
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Validate_WithValidOptions_ReturnsTrue()
    {
        // Arrange
        var options = CreateValidOptions();
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        var validator = new ContractValidationStartupValidator(mockOptions.Object, mockLogger.Object);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.True(result);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Validating contract validation configuration...")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Contract validation configuration is valid.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WithInvalidOptions_ReturnsFalse()
    {
        // Arrange
        var options = CreateInvalidOptions();
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        var validator = new ContractValidationStartupValidator(mockOptions.Object, mockLogger.Object);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.False(result);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Contract validation configuration validation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WithInvalidOptionsAndFailFast_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateInvalidOptions();
        options.FailFastOnInvalidConfiguration = true; // Enable fail fast
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        var validator = new ContractValidationStartupValidator(mockOptions.Object, mockLogger.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("Contract validation configuration is invalid", exception.Message);
    }

    [Fact]
    public void Validate_WithValidationErrors_LogsEachError()
    {
        // Arrange
        var options = new ContractValidationOptions
        {
            ValidationTimeout = TimeSpan.Zero, // Invalid value
            MaxErrorCount = -1, // Invalid value
            ValidationStrategy = "InvalidStrategy", // Invalid value
            EnableEnhancedValidation = true, // Required to avoid additional validation errors
            EnableSchemaDiscovery = false, // Required to avoid additional validation errors
            EnableCustomValidators = false, // Required to avoid additional validation errors
            FailFastOnInvalidConfiguration = false // Don't throw exception
        };
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        var validator = new ContractValidationStartupValidator(mockOptions.Object, mockLogger.Object);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.False(result);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ValidationTimeout must be greater than zero")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MaxErrorCount must be greater than zero")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ValidationStrategy must be one of: Strict, Lenient, Custom")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Validate_WithValidOptions_LogsConfigurationSummary()
    {
        // Arrange
        var options = CreateValidOptions();
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        var validator = new ContractValidationStartupValidator(mockOptions.Object, mockLogger.Object);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.True(result);
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Contract Validation Configuration Summary:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        
        // Verify that various configuration values are logged in the summary
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("EnableAutomaticContractValidation:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ValidationStrategy:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MaxErrorCount:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private ContractValidationOptions CreateValidOptions()
    {
        return new ContractValidationOptions
        {
            EnableAutomaticContractValidation = true,
            ValidationStrategy = "Strict",
            ValidationTimeout = TimeSpan.FromSeconds(5),
            MaxErrorCount = 100,
            EnableEnhancedValidation = true,
            EnableSchemaDiscovery = true,
            EnableCustomValidators = true,
            SchemaCache = new SchemaCacheOptions
            {
                MaxCacheSize = 1000,
                EnableCacheWarming = false
            },
            SchemaDiscovery = new SchemaDiscoveryOptions
            {
                EnableEmbeddedResources = true,
                EnableFileSystemWatcher = false,
                EnableHttpSchemas = false,
                NamingConvention = "schemas/{TypeName}.json"
            }
        };
    }

    private ContractValidationOptions CreateInvalidOptions()
    {
        return new ContractValidationOptions
        {
            ValidationTimeout = TimeSpan.Zero, // Invalid
            MaxErrorCount = 0, // Invalid
            ValidationStrategy = "", // Invalid
            EnableEnhancedValidation = true, // Set to true to avoid additional validation conflicts
            EnableSchemaDiscovery = false, // Disable to avoid conflicts
            EnableCustomValidators = false, // Disable to avoid conflicts
            FailFastOnInvalidConfiguration = false // Don't fail fast for this test
        };
    }
}
