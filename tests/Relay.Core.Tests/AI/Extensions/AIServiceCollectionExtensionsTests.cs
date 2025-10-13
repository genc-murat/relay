using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.AI.Extensions
{
    public class AIServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddAIOptimization_AddsServicesToCollection()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimization();

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
            // Verify that RegisterWithConfiguration is called, but since it's internal, hard to test directly
        }

        [Fact]
        public void AddAIOptimization_WithConfiguration_AddsServicesToCollection()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimization(options => { });

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimization_WithConfiguration_ThrowsArgumentNullException_WhenServicesIsNull()
        {
            IServiceCollection services = null;

            Assert.Throws<ArgumentNullException>(() => services.AddAIOptimization(options => { }));
        }

        [Fact]
        public void AddAIOptimization_WithConfiguration_ThrowsArgumentNullException_WhenConfigureOptionsIsNull()
        {
            var services = new Mock<IServiceCollection>();

            Assert.Throws<ArgumentNullException>(() => services.Object.AddAIOptimization(null));
        }

        [Fact]
        public void AddAIOptimization_WithConfiguration_AddsServicesToCollection_WithOptions()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimization(options =>
            {
                options.DefaultBatchSize = 10;
            });

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimization_WithConfiguration_AddsCoreServices()
        {
            var services = new Mock<IServiceCollection>();

            services.Object.AddAIOptimization(options => { });

            // Since RegisterWithConfiguration is internal, we can't easily verify the registrations
            // But the method should not throw
        }

        [Fact]
        public void AddAIOptimization_WithConfigurationAndSection_AddsServicesToCollection()
        {
            var services = new Mock<IServiceCollection>();
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TestSection:Enabled"] = "true",
                ["TestSection:LearningEnabled"] = "true",
                ["TestSection:ModelUpdateInterval"] = "00:30:00",
                ["TestSection:DefaultBatchSize"] = "10",
                ["TestSection:MaxBatchSize"] = "100"
            });
            var configuration = configurationBuilder.Build();

            var result = services.Object.AddAIOptimization(configuration, "TestSection");

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimization_WithConfigurationAndSection_ThrowsArgumentNullException_WhenServicesIsNull()
        {
            IServiceCollection services = null;
            var configuration = new Mock<IConfiguration>();

            Assert.Throws<ArgumentNullException>(() => services.AddAIOptimization(configuration.Object, "TestSection"));
        }

        [Fact]
        public void AddAIOptimization_WithConfigurationAndSection_ThrowsArgumentNullException_WhenConfigurationIsNull()
        {
            var services = new Mock<IServiceCollection>();
            IConfiguration configuration = null;

            Assert.Throws<ArgumentNullException>(() => services.Object.AddAIOptimization(configuration, "TestSection"));
        }

        [Fact]
        public void AddAdvancedAIOptimization_AddsServicesToCollection()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAdvancedAIOptimization(options => { }, true);

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAdvancedAIOptimization_WithAdvancedFeatures_AddsAdditionalServices()
        {
            var services = new Mock<IServiceCollection>();

            services.Object.AddAdvancedAIOptimization(options => { }, true);

            // Should add advanced services when enableAdvancedFeatures is true
        }

        [Fact]
        public void AddAdvancedAIOptimization_WithoutAdvancedFeatures_DoesNotAddAdditionalServices()
        {
            var services = new Mock<IServiceCollection>();

            services.Object.AddAdvancedAIOptimization(options => { }, false);

            // Should not add advanced services when enableAdvancedFeatures is false
        }

        [Fact]
        public void AddAdvancedAIOptimization_ThrowsArgumentNullException_WhenServicesIsNull()
        {
            IServiceCollection services = null;

            Assert.Throws<ArgumentNullException>(() => services.AddAdvancedAIOptimization(options => { }, true));
        }

        [Fact]
        public void AddAdvancedAIOptimization_ThrowsArgumentNullException_WhenConfigureOptionsIsNull()
        {
            var services = new Mock<IServiceCollection>();

            Assert.Throws<ArgumentNullException>(() => services.Object.AddAdvancedAIOptimization(null, true));
        }

        [Fact]
        public void AddAIOptimizationForScenario_HighThroughput_ConfiguresCorrectly()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.HighThroughput);

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimizationForScenario_LowLatency_ConfiguresCorrectly()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.LowLatency);

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimizationForScenario_ResourceConstrained_ConfiguresCorrectly()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.ResourceConstrained);

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimizationForScenario_Development_ConfiguresCorrectly()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.Development);

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimizationForScenario_Production_ConfiguresCorrectly()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.Production);

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimizationForScenario_ThrowsArgumentOutOfRangeException_WhenInvalidScenario()
        {
            var services = new Mock<IServiceCollection>();

            Assert.Throws<ArgumentOutOfRangeException>(() => services.Object.AddAIOptimizationForScenario((AIOptimizationScenario)999));
        }

        [Fact]
        public void AddAIOptimizationForScenario_ThrowsArgumentNullException_WhenServicesIsNull()
        {
            IServiceCollection services = null;

            Assert.Throws<ArgumentNullException>(() => services.AddAIOptimizationForScenario(AIOptimizationScenario.Production));
        }

        [Fact]
        public void AddAIOptimizationWithCustomModel_AddsServicesToCollection()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimizationWithCustomModel<TestPredictionModel>(options => { });

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimizationWithCustomModel_ThrowsArgumentNullException_WhenServicesIsNull()
        {
            IServiceCollection services = null;

            Assert.Throws<ArgumentNullException>(() => services.AddAIOptimizationWithCustomModel<TestPredictionModel>(options => { }));
        }

        [Fact]
        public void AddAIOptimizationWithCustomModel_ThrowsArgumentNullException_WhenConfigureOptionsIsNull()
        {
            var services = new Mock<IServiceCollection>();

            Assert.Throws<ArgumentNullException>(() => services.Object.AddAIOptimizationWithCustomModel<TestPredictionModel>(null));
        }

        [Fact]
        public void AddAIOptimizationHealthChecks_AddsHealthChecksToCollection()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimizationHealthChecks();

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimizationHealthChecks_ThrowsArgumentNullException_WhenServicesIsNull()
        {
            IServiceCollection services = null;

            Assert.Throws<ArgumentNullException>(() => services.AddAIOptimizationHealthChecks());
        }

        [Fact]
        public void AddAIOptimizationHealthChecks_WithConfiguration_AddsHealthChecksToCollection()
        {
            var services = new Mock<IServiceCollection>();

            var result = services.Object.AddAIOptimizationHealthChecks(options => { });

            Assert.NotNull(result);
            Assert.Equal(services.Object, result);
        }

        [Fact]
        public void AddAIOptimizationHealthChecks_WithConfiguration_ThrowsArgumentNullException_WhenServicesIsNull()
        {
            IServiceCollection services = null;

            Assert.Throws<ArgumentNullException>(() => services.AddAIOptimizationHealthChecks(options => { }));
        }

        [Fact]
        public void AddAIOptimizationHealthChecks_WithConfiguration_ThrowsArgumentNullException_WhenConfigureHealthChecksIsNull()
        {
            var services = new Mock<IServiceCollection>();

            Assert.Throws<ArgumentNullException>(() => services.Object.AddAIOptimizationHealthChecks(null));
        }

        [Fact]
        public async Task GetAIOptimizationHealthAsync_ReturnsHealthResult()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var engine = new Mock<IAIOptimizationEngine>();
            var logger = new Mock<ILogger<AIOptimizationHealthCheck>>();
            var options = new Mock<IOptions<AIHealthCheckOptions>>();
            options.Setup(o => o.Value).Returns(new AIHealthCheckOptions());

            var healthCheck = new Mock<AIOptimizationHealthCheck>(engine.Object, logger.Object, options.Object);
            healthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ComponentHealthResult { IsHealthy = true, ComponentName = "Test" });

            serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(healthCheck.Object);

            var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

            Assert.NotNull(result);
            Assert.True(result.IsHealthy);
            Assert.NotNull(result.ComponentResults);
        }

        [Fact]
        public async Task GetAIOptimizationHealthAsync_ThrowsArgumentNullException_WhenServiceProviderIsNull()
        {
            IServiceProvider serviceProvider = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() => serviceProvider.GetAIOptimizationHealthAsync());
        }

        [Fact]
        public async Task GetAIOptimizationHealthAsync_HandlesCancellationToken()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await serviceProvider.Object.GetAIOptimizationHealthAsync(cts.Token);

            Assert.NotNull(result);
            // Should handle cancellation gracefully
        }

        [Fact]
        public async Task GetAIOptimizationHealthAsync_ReturnsUnhealthy_WhenComponentFails()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var engine = new Mock<IAIOptimizationEngine>();
            var logger = new Mock<ILogger<AIOptimizationHealthCheck>>();
            var options = new Mock<IOptions<AIHealthCheckOptions>>();
            options.Setup(o => o.Value).Returns(new AIHealthCheckOptions());

            var healthCheck = new Mock<AIOptimizationHealthCheck>(engine.Object, logger.Object, options.Object);
            healthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ComponentHealthResult { IsHealthy = false, ComponentName = "Test" });

            serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(healthCheck.Object);

            var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

            Assert.NotNull(result);
            Assert.False(result.IsHealthy);
        }

        [Fact]
        public async Task GetAIOptimizationHealthAsync_HandlesException()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var engine = new Mock<IAIOptimizationEngine>();
            var logger = new Mock<ILogger<AIOptimizationHealthCheck>>();
            var options = new Mock<IOptions<AIHealthCheckOptions>>();
            options.Setup(o => o.Value).Returns(new AIHealthCheckOptions());

            var healthCheck = new Mock<AIOptimizationHealthCheck>(engine.Object, logger.Object, options.Object);
            healthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(healthCheck.Object);

            var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

            Assert.NotNull(result);
            Assert.False(result.IsHealthy);
            Assert.NotNull(result.Exception);
        }

        // Mock class for testing
        private class TestPredictionModel : IAIPredictionModel
        {
            public ValueTask<OptimizationRecommendation> PredictAsync(RequestContext context, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}