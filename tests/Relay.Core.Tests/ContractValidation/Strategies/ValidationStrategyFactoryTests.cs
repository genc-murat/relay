using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.ContractValidation.Strategies;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Strategies;

public class ValidationStrategyFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ValidationStrategyFactory _factory;

    public ValidationStrategyFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
        _factory = new ValidationStrategyFactory(_serviceProvider);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationStrategyFactory(null!));
    }

    [Fact]
    public void CreateStrategy_WithStrictName_ShouldReturnStrictStrategy()
    {
        // Act
        var strategy = _factory.CreateStrategy("Strict");

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<StrictValidationStrategy>(strategy);
        Assert.Equal("Strict", strategy.Name);
    }

    [Fact]
    public void CreateStrategy_WithLenientName_ShouldReturnLenientStrategy()
    {
        // Act
        var strategy = _factory.CreateStrategy("Lenient");

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<LenientValidationStrategy>(strategy);
        Assert.Equal("Lenient", strategy.Name);
    }

    [Theory]
    [InlineData("strict")]
    [InlineData("STRICT")]
    [InlineData("StRiCt")]
    public void CreateStrategy_WithStrictNameCaseInsensitive_ShouldReturnStrictStrategy(string strategyName)
    {
        // Act
        var strategy = _factory.CreateStrategy(strategyName);

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<StrictValidationStrategy>(strategy);
    }

    [Theory]
    [InlineData("lenient")]
    [InlineData("LENIENT")]
    [InlineData("LeNiEnT")]
    public void CreateStrategy_WithLenientNameCaseInsensitive_ShouldReturnLenientStrategy(string strategyName)
    {
        // Act
        var strategy = _factory.CreateStrategy(strategyName);

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<LenientValidationStrategy>(strategy);
    }

    [Fact]
    public void CreateStrategy_WithNullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _factory.CreateStrategy(null!));
        Assert.Contains("Strategy name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void CreateStrategy_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _factory.CreateStrategy(string.Empty));
        Assert.Contains("Strategy name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void CreateStrategy_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _factory.CreateStrategy("   "));
        Assert.Contains("Strategy name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void CreateStrategy_WithUnknownName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _factory.CreateStrategy("Unknown"));
        Assert.Contains("Unknown validation strategy", exception.Message);
        Assert.Contains("Unknown", exception.Message);
    }

    [Fact]
    public void CreateStrictStrategy_ShouldReturnStrictStrategy()
    {
        // Act
        var strategy = _factory.CreateStrictStrategy();

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<StrictValidationStrategy>(strategy);
        Assert.Equal("Strict", strategy.Name);
    }

    [Fact]
    public void CreateLenientStrategy_ShouldReturnLenientStrategy()
    {
        // Act
        var strategy = _factory.CreateLenientStrategy();

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<LenientValidationStrategy>(strategy);
        Assert.Equal("Lenient", strategy.Name);
    }

    [Fact]
    public void GetDefaultStrategy_ShouldReturnStrictStrategy()
    {
        // Act
        var strategy = _factory.GetDefaultStrategy();

        // Assert
        Assert.NotNull(strategy);
        Assert.IsType<StrictValidationStrategy>(strategy);
        Assert.Equal("Strict", strategy.Name);
    }

    [Fact]
    public void CreateStrategy_CalledMultipleTimes_ShouldReturnNewInstances()
    {
        // Act
        var strategy1 = _factory.CreateStrategy("Strict");
        var strategy2 = _factory.CreateStrategy("Strict");

        // Assert
        Assert.NotSame(strategy1, strategy2);
    }

    [Fact]
    public void CreateLenientStrategy_WithoutLoggingService_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emptyServices = new ServiceCollection();
        var emptyServiceProvider = emptyServices.BuildServiceProvider();
        var factoryWithoutLogging = new ValidationStrategyFactory(emptyServiceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factoryWithoutLogging.CreateLenientStrategy());
    }
}
