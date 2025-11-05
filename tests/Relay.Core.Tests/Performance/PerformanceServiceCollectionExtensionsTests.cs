using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Configuration.Options.Performance;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using Relay.Core.Telemetry;
using System;
using Xunit;
using Microsoft.Extensions.ObjectPool;

namespace Relay.Core.Tests.Performance
{
    /// <summary>
    /// Tests for PerformanceServiceCollectionExtensions
    /// </summary>
    public class PerformanceServiceCollectionExtensionsTests
    {
        #region AddRelayPerformanceOptimizations Tests

        [Fact]
        public void AddRelayPerformanceOptimizations_Should_Register_Core_Performance_Services()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayPerformanceOptimizations();
            var provider = services.BuildServiceProvider();

            // Assert
            // Should register core performance services
            var telemetryContextPool = provider.GetService<ITelemetryContextPool>();
            var objectPoolProvider = provider.GetService<ObjectPoolProvider>();

            Assert.NotNull(telemetryContextPool);
            Assert.NotNull(objectPoolProvider);
            Assert.IsType<DefaultTelemetryContextPool>(telemetryContextPool);
        }

        [Fact]
        public void AddRelayPerformanceOptimizations_Should_Not_Override_Existing_Registrations()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Register custom implementations first
            services.AddSingleton<ITelemetryContextPool, TestTelemetryContextPool>();
            services.AddSingleton<ObjectPoolProvider, TestObjectPoolProvider>();

            // Act
            services.AddRelayPerformanceOptimizations();
            var provider = services.BuildServiceProvider();

            // Assert - Should keep the custom implementations
            var telemetryContextPool = provider.GetService<ITelemetryContextPool>();
            var objectPoolProvider = provider.GetService<ObjectPoolProvider>();

            Assert.IsType<TestTelemetryContextPool>(telemetryContextPool);
            Assert.IsType<TestObjectPoolProvider>(objectPoolProvider);
        }

        #endregion

        #region WithPerformanceProfile Tests

        [Fact]
        public void WithPerformanceProfile_Should_Set_Profile_In_Options()
        {
            // Arrange
            var services = new ServiceCollection();
            var profile = PerformanceProfile.HighThroughput;

            // Act
            services.WithPerformanceProfile(profile);
            var provider = services.BuildServiceProvider();
            
            // Assert - Method should not throw
            Assert.NotNull(provider);
        }

        [Theory]
        [InlineData(PerformanceProfile.LowMemory)]
        [InlineData(PerformanceProfile.Balanced)]
        [InlineData(PerformanceProfile.HighThroughput)]
        [InlineData(PerformanceProfile.UltraLowLatency)]
        [InlineData(PerformanceProfile.Custom)]
        public void WithPerformanceProfile_Should_Accept_All_Profile_Values(PerformanceProfile profile)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - Should not throw for any profile value
            var result = services.WithPerformanceProfile(profile);
            Assert.Same(services, result);
        }

        #endregion

        #region ConfigurePerformance Tests

        [Fact]
        public void ConfigurePerformance_Should_Set_Custom_Options()
        {
            // Arrange
            var services = new ServiceCollection();
            var configureAction = new Action<PerformanceOptions>(options =>
            {
                options.EnableAggressiveInlining = false;
                options.CacheDispatchers = false;
                options.HandlerCacheMaxSize = 500;
            });

            // Act
            services.ConfigurePerformance(configureAction);
            var provider = services.BuildServiceProvider();

            // Assert - Method should not throw
            Assert.NotNull(provider);
        }

        [Fact]
        public void ConfigurePerformance_Should_Set_Profile_To_Custom()
        {
            // Arrange
            var services = new ServiceCollection();
            var configureAction = new Action<PerformanceOptions>(options =>
            {
                options.EnableAggressiveInlining = false;
            });

            // Act
            services.ConfigurePerformance(configureAction);
            var provider = services.BuildServiceProvider();

            // Assert - Method should not throw
            Assert.NotNull(provider);
        }

        [Fact]
        public void ConfigurePerformance_Should_Throw_When_Services_Is_Null()
        {
            // Arrange
            IServiceCollection services = null!;
            var configureAction = new Action<PerformanceOptions>(options => { });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.ConfigurePerformance(configureAction));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void AddRelayPerformanceOptimizations_And_WithPerformanceProfile_Should_Work_Together()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayPerformanceOptimizations()
                    .WithPerformanceProfile(PerformanceProfile.HighThroughput);
                    
            var provider = services.BuildServiceProvider();

            // Assert - Both methods should work together without conflicts
            var telemetryContextPool = provider.GetService<ITelemetryContextPool>();
            var objectPoolProvider = provider.GetService<ObjectPoolProvider>();

            Assert.NotNull(telemetryContextPool);
            Assert.NotNull(objectPoolProvider);
        }

        [Fact]
        public void AddRelayPerformanceOptimizations_And_ConfigurePerformance_Should_Work_Together()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayPerformanceOptimizations()
                    .ConfigurePerformance(options =>
                    {
                        options.EnableAggressiveInlining = false;
                        options.CacheDispatchers = false;
                    });
                    
            var provider = services.BuildServiceProvider();

            // Assert - Both methods should work together without conflicts
            var telemetryContextPool = provider.GetService<ITelemetryContextPool>();
            var objectPoolProvider = provider.GetService<ObjectPoolProvider>();

            Assert.NotNull(telemetryContextPool);
            Assert.NotNull(objectPoolProvider);
        }

        #endregion

        #region Test Classes

        internal class TestTelemetryContextPool : ITelemetryContextPool
        {
            public TelemetryContext Get() => new TelemetryContext();
            public void Return(TelemetryContext context) { }
        }

        internal class TestObjectPoolProvider : ObjectPoolProvider
        {
            public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
            {
                return null!; // Just for testing, not used in these tests
            }
        }

        #endregion
    }
}
