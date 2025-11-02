using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation.Configuration;
using Relay.Core.Configuration.Options.ContractValidation;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Configuration;

public class ContractValidationStartupValidatorExtensionsTests
{
    [Fact]
    public void ValidateContractValidationConfiguration_WithValidator_InvokesValidate()
    {
        // Arrange
        var options = new ContractValidationOptions();
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        
        var validator = new ContractValidationStartupValidator(mockOptions.Object, mockLogger.Object);
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(validator);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act & Assert
        // The Validate method is not virtual so we can't verify it was called directly
        // However, we can test that the extension method runs without error
        var result = serviceProvider.ValidateContractValidationConfiguration();
        
        Assert.Same(serviceProvider, result);
    }

    [Fact]
    public void ValidateContractValidationConfiguration_WithoutValidator_DoesNotThrow()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act & Assert
        var result = serviceProvider.ValidateContractValidationConfiguration();
        
        // Ensure the same serviceProvider is returned
        Assert.Equal(serviceProvider, result);
    }

    [Fact]
    public void ValidateContractValidationConfiguration_ReturnsSameServiceProvider()
    {
        // Arrange
        var options = new ContractValidationOptions();
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        
        var validator = new ContractValidationStartupValidator(mockOptions.Object, mockLogger.Object);
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(validator);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var result = serviceProvider.ValidateContractValidationConfiguration();

        // Assert
        Assert.Same(serviceProvider, result);
    }
    
    [Fact]
    public void ValidateContractValidationConfiguration_WithFailingValidator_ThrowsWhenFailFastEnabled()
    {
        // Arrange
        var options = new ContractValidationOptions 
        { 
            FailFastOnInvalidConfiguration = true,
            ValidationTimeout = TimeSpan.Zero, // This will cause validation to fail
            ValidationStrategy = "Strict"
        };
        var mockOptions = new Mock<IOptions<ContractValidationOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        
        var mockLogger = new Mock<ILogger<ContractValidationStartupValidator>>();
        
        var validator = new ContractValidationStartupValidator(mockOptions.Object, mockLogger.Object);
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(validator);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => serviceProvider.ValidateContractValidationConfiguration());
            
        Assert.Contains("Contract validation configuration is invalid", exception.Message);
    }
}