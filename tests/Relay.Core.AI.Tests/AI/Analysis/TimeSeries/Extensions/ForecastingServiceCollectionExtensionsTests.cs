using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries.Extensions;

/// <summary>
/// Tests for ForecastingServiceCollectionExtensions
/// </summary>
public class ForecastingServiceCollectionExtensionsTests
{
    #region AddForecasting Tests

    [Fact]
    public void AddForecasting_WithoutConfiguration_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        services.AddSingleton(Mock.Of<ITimeSeriesRepository>()); // Add mock repository

        // Act
        var result = services.AddForecasting();

        // Assert
        Assert.NotNull(result);
        Assert.Same(services, result);

        // Verify all services are registered
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<ForecastingConfiguration>());
        Assert.NotNull(serviceProvider.GetService<IForecastingModelManager>());
        Assert.NotNull(serviceProvider.GetService<IForecastingMethodManager>());
        Assert.NotNull(serviceProvider.GetService<IForecastingTrainer>());
        Assert.NotNull(serviceProvider.GetService<IForecastingPredictor>());
        Assert.NotNull(serviceProvider.GetService<IForecastingService>());
    }

    [Fact]
    public void AddForecasting_WithConfiguration_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        services.AddSingleton(Mock.Of<ITimeSeriesRepository>()); // Add mock repository
        var expectedHorizon = 48;

        // Act
        var result = services.AddForecasting(config =>
        {
            config.DefaultForecastHorizon = expectedHorizon;
            config.AutoTrainOnForecast = false;
        });

        // Assert
        Assert.NotNull(result);
        Assert.Same(services, result);

        // Verify all services are registered
        var serviceProvider = services.BuildServiceProvider();

        var config = serviceProvider.GetService<ForecastingConfiguration>();
        Assert.NotNull(config);
        Assert.Equal(expectedHorizon, config.DefaultForecastHorizon);
        Assert.False(config.AutoTrainOnForecast);

        Assert.NotNull(serviceProvider.GetService<IForecastingModelManager>());
        Assert.NotNull(serviceProvider.GetService<IForecastingMethodManager>());
        Assert.NotNull(serviceProvider.GetService<IForecastingTrainer>());
        Assert.NotNull(serviceProvider.GetService<IForecastingPredictor>());
        Assert.NotNull(serviceProvider.GetService<IForecastingService>());
    }

    [Fact]
    public void AddForecasting_RegistersServicesWithCorrectLifetimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddForecasting();

        // Assert - Check service descriptors
        var configDescriptor = Assert.Single(services, d => d.ServiceType == typeof(ForecastingConfiguration));
        Assert.Equal(ServiceLifetime.Singleton, configDescriptor.Lifetime);

        var modelManagerDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IForecastingModelManager));
        Assert.Equal(ServiceLifetime.Singleton, modelManagerDescriptor.Lifetime);

        var methodManagerDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IForecastingMethodManager));
        Assert.Equal(ServiceLifetime.Singleton, methodManagerDescriptor.Lifetime);

        var trainerDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IForecastingTrainer));
        Assert.Equal(ServiceLifetime.Transient, trainerDescriptor.Lifetime);

        var predictorDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IForecastingPredictor));
        Assert.Equal(ServiceLifetime.Transient, predictorDescriptor.Lifetime);

        var serviceDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IForecastingService));
        Assert.Equal(ServiceLifetime.Transient, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddForecasting_RegistersCorrectImplementationTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        services.AddSingleton(Mock.Of<ITimeSeriesRepository>()); // Add mock repository

        // Act
        services.AddForecasting();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var modelManager = serviceProvider.GetService<IForecastingModelManager>();
        Assert.IsType<ForecastingModelManager>(modelManager);

        var methodManager = serviceProvider.GetService<IForecastingMethodManager>();
        Assert.IsType<ForecastingMethodManager>(methodManager);

        var trainer = serviceProvider.GetService<IForecastingTrainer>();
        Assert.IsType<ForecastingTrainer>(trainer);

        var predictor = serviceProvider.GetService<IForecastingPredictor>();
        Assert.IsType<ForecastingPredictor>(predictor);

        var forecastingService = serviceProvider.GetService<IForecastingService>();
        Assert.IsType<ForecastingService>(forecastingService);
    }

    [Fact]
    public void AddForecasting_SingletonServices_ReturnSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        services.AddSingleton(Mock.Of<ITimeSeriesRepository>()); // Add mock repository
        services.AddForecasting();

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Singleton services should return same instance
        var config1 = serviceProvider.GetService<ForecastingConfiguration>();
        var config2 = serviceProvider.GetService<ForecastingConfiguration>();
        Assert.Same(config1, config2);

        var modelManager1 = serviceProvider.GetService<IForecastingModelManager>();
        var modelManager2 = serviceProvider.GetService<IForecastingModelManager>();
        Assert.Same(modelManager1, modelManager2);

        var methodManager1 = serviceProvider.GetService<IForecastingMethodManager>();
        var methodManager2 = serviceProvider.GetService<IForecastingMethodManager>();
        Assert.Same(methodManager1, methodManager2);
    }

    [Fact]
    public void AddForecasting_TransientServices_ReturnDifferentInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        services.AddSingleton(Mock.Of<ITimeSeriesRepository>()); // Add mock repository
        services.AddForecasting();

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Transient services should return different instances
        var trainer1 = serviceProvider.GetService<IForecastingTrainer>();
        var trainer2 = serviceProvider.GetService<IForecastingTrainer>();
        Assert.NotSame(trainer1, trainer2);

        var predictor1 = serviceProvider.GetService<IForecastingPredictor>();
        var predictor2 = serviceProvider.GetService<IForecastingPredictor>();
        Assert.NotSame(predictor1, predictor2);

        var service1 = serviceProvider.GetService<IForecastingService>();
        var service2 = serviceProvider.GetService<IForecastingService>();
        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void AddForecasting_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddForecasting());
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddForecasting_WithConfigurationNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<ForecastingConfiguration> configure = _ => { };

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddForecasting(configure));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddForecasting_WithNullConfigurationAction_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<ForecastingConfiguration> configure = null!;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddForecasting(configure));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public void AddForecasting_DefaultConfiguration_UsesExpectedDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddForecasting();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetService<ForecastingConfiguration>();

        Assert.NotNull(config);
        Assert.Equal(24, config.DefaultForecastHorizon);
        Assert.Equal(ForecastingMethod.SSA, config.DefaultForecastingMethod);
        Assert.Equal(10, config.MinimumDataPoints);
        Assert.Equal(7, config.TrainingDataWindowDays);
        Assert.True(config.AutoTrainOnForecast);
        Assert.Equal(42, config.MlContextSeed);
    }

    [Fact]
    public void AddForecasting_MultipleCalls_RegistersServicesMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<ITimeSeriesRepository>());

        // Act
        services.AddForecasting();
        services.AddForecasting(); // Call again

        // Assert
        // Multiple registrations should not prevent resolution
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<ForecastingConfiguration>());
        Assert.NotNull(serviceProvider.GetService<IForecastingModelManager>());
        Assert.NotNull(serviceProvider.GetService<IForecastingService>());

        // Check that we have multiple descriptors for the same service
        var configDescriptors = services.Where(d => d.ServiceType == typeof(ForecastingConfiguration)).ToList();
        Assert.Equal(2, configDescriptors.Count); // Two registrations
    }

    [Fact]
    public void AddForecasting_WithConfigureActionThrowingException_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddForecasting(config => throw new InvalidOperationException("Configure failed")));

        Assert.Equal("Configure failed", ex.Message);
    }

    [Fact]
    public void AddForecasting_WithExtremeConfigurationValues_AppliesConfigurationCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddForecasting(config =>
        {
            config.DefaultForecastHorizon = int.MaxValue;
            config.MinimumDataPoints = 1;
            config.TrainingDataWindowDays = 0;
            config.AutoTrainOnForecast = false;
            config.MlContextSeed = int.MinValue;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetService<ForecastingConfiguration>();

        Assert.NotNull(config);
        Assert.Equal(int.MaxValue, config.DefaultForecastHorizon);
        Assert.Equal(1, config.MinimumDataPoints);
        Assert.Equal(0, config.TrainingDataWindowDays);
        Assert.False(config.AutoTrainOnForecast);
        Assert.Equal(int.MinValue, config.MlContextSeed);
    }

    [Fact]
    public void AddForecasting_AllServicesCanBeResolvedAndAreUsable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<ITimeSeriesRepository>());

        // Act
        services.AddForecasting();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify all services can be resolved
        var config = serviceProvider.GetService<ForecastingConfiguration>();
        var modelManager = serviceProvider.GetService<IForecastingModelManager>();
        var methodManager = serviceProvider.GetService<IForecastingMethodManager>();
        var trainer = serviceProvider.GetService<IForecastingTrainer>();
        var predictor = serviceProvider.GetService<IForecastingPredictor>();
        var forecastingService = serviceProvider.GetService<IForecastingService>();

        // Ensure they are not null and of expected types
        Assert.NotNull(config);
        Assert.IsType<ForecastingModelManager>(modelManager);
        Assert.IsType<ForecastingMethodManager>(methodManager);
        Assert.IsType<ForecastingTrainer>(trainer);
        Assert.IsType<ForecastingPredictor>(predictor);
        Assert.IsType<ForecastingService>(forecastingService);

        // Verify they have required dependencies (basic usability check)
        // This ensures the services were constructed properly
        Assert.NotNull(modelManager);
        Assert.NotNull(methodManager);
        Assert.NotNull(trainer);
        Assert.NotNull(predictor);
        Assert.NotNull(forecastingService);
    }

    #endregion
}