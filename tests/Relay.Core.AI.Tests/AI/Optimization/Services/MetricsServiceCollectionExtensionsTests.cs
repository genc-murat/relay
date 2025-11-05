using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.AI.Optimization.Services;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services
{
    public class MetricsServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddMetricsServices_ShouldRegisterCoreInterfaces()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddMetricsServices();

            // Assert
            Assert.Same(services, result);

            // Check that all core interfaces are registered as singletons
            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(IMetricsPublisher) && 
                descriptor.ImplementationType == typeof(DefaultMetricsPublisher) &&
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(IMetricsAggregator) && 
                descriptor.ImplementationType == typeof(DefaultMetricsAggregator) &&
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(ISystemAnalyzer) && 
                descriptor.ImplementationType == typeof(DefaultSystemAnalyzer) &&
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(IHealthScorer) && 
                descriptor.ImplementationType == typeof(CompositeHealthScorer) &&
                descriptor.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddMetricsServices_ShouldRegisterIndividualHealthScorers()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddMetricsServices();

            // Assert
            Assert.Same(services, result);

            // Check that all individual health scorers are registered as singletons
            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(PerformanceScorer) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(ReliabilityScorer) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(ScalabilityScorer) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(SecurityScorer) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(MaintainabilityScorer) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddMetricsServices_ShouldRegisterOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddMetricsServices();

            // Assert
            Assert.Same(services, result);

            // Check that options are registered as singletons
            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(MetricsCollectionOptions) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(HealthScoringOptions) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddMetricsServices_ShouldRegisterMainService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddMetricsServices();

            // Assert
            Assert.Same(services, result);

            // Check that the main service is registered as singleton
            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(SystemMetricsService) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddMetricsServices_WithCustomOptions_ShouldRegisterProvidedOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var metricsOptions = new MetricsCollectionOptions();
            var healthOptions = new HealthScoringOptions();

            // Act
            var result = services.AddMetricsServices(metricsOptions, healthOptions);

            // Assert
            Assert.Same(services, result);

            // Check that the provided options are registered as singletons
            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(MetricsCollectionOptions) && 
                descriptor.ImplementationInstance == metricsOptions &&
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(HealthScoringOptions) && 
                descriptor.ImplementationInstance == healthOptions &&
                descriptor.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddMetricsServices_WithCustomOptions_ShouldAlsoRegisterMainServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var metricsOptions = new MetricsCollectionOptions();
            var healthOptions = new HealthScoringOptions();

            // Act
            var result = services.AddMetricsServices(metricsOptions, healthOptions);

            // Assert
            Assert.Same(services, result);

            // Check that all the main services are still registered
            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(IMetricsPublisher) && 
                descriptor.ImplementationType == typeof(DefaultMetricsPublisher) &&
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => 
                descriptor.ServiceType == typeof(SystemMetricsService) && 
                descriptor.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddMetricsServices_ShouldNotRegisterDuplicateServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMetricsServices();
            services.AddMetricsServices(); // Call twice

            // Assert
            // Count how many times each service type is registered
            var publisherRegistrations = services.Count(d => d.ServiceType == typeof(IMetricsPublisher));
            var aggregatorRegistrations = services.Count(d => d.ServiceType == typeof(IMetricsAggregator));
            var analyzerRegistrations = services.Count(d => d.ServiceType == typeof(ISystemAnalyzer));
            var scorerRegistrations = services.Count(d => d.ServiceType == typeof(IHealthScorer));
            var metricsOptionsRegistrations = services.Count(d => d.ServiceType == typeof(MetricsCollectionOptions));
            var healthOptionsRegistrations = services.Count(d => d.ServiceType == typeof(HealthScoringOptions));
            var systemMetricsServiceRegistrations = services.Count(d => d.ServiceType == typeof(SystemMetricsService));

            // Each service should only be registered once
            Assert.Equal(1, publisherRegistrations);
            Assert.Equal(1, aggregatorRegistrations);
            Assert.Equal(1, analyzerRegistrations);
            Assert.Equal(1, scorerRegistrations);
            Assert.Equal(1, metricsOptionsRegistrations);
            Assert.Equal(1, healthOptionsRegistrations);
            Assert.Equal(1, systemMetricsServiceRegistrations);
        }

        [Fact]
        public void AddMetricsServices_WithCustomOptions_ShouldNotRegisterDuplicateOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var metricsOptions = new MetricsCollectionOptions();
            var healthOptions = new HealthScoringOptions();

            // Act
            services.AddMetricsServices(metricsOptions, healthOptions);
            services.AddMetricsServices(metricsOptions, healthOptions); // Call twice with same instances

            // Assert
            // Count how many times each option type is registered
            var metricsOptionsRegistrations = services.Count(d => d.ServiceType == typeof(MetricsCollectionOptions));
            var healthOptionsRegistrations = services.Count(d => d.ServiceType == typeof(HealthScoringOptions));

            // Each option should only be registered once
            Assert.Equal(1, metricsOptionsRegistrations);
            Assert.Equal(1, healthOptionsRegistrations);
        }
    }
}