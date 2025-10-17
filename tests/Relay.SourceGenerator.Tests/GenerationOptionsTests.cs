using Relay.SourceGenerator.Generators;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for GenerationOptions class to verify generator enable/disable logic.
    /// </summary>
    public class GenerationOptionsTests
    {
        #region IsGeneratorEnabled Tests

        [Fact]
        public void IsGeneratorEnabled_DIRegistrationGenerator_WhenEnabled_ReturnsTrue()
        {
            // Arrange
            var options = new GenerationOptions { EnableDIGeneration = true };

            // Act
            var result = options.IsGeneratorEnabled("DI Registration Generator");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_DIRegistrationGenerator_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            var options = new GenerationOptions { EnableDIGeneration = false };

            // Act
            var result = options.IsGeneratorEnabled("DI Registration Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_HandlerRegistryGenerator_WhenEnabled_ReturnsTrue()
        {
            // Arrange
            var options = new GenerationOptions { EnableHandlerRegistry = true };

            // Act
            var result = options.IsGeneratorEnabled("Handler Registry Generator");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_HandlerRegistryGenerator_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            var options = new GenerationOptions { EnableHandlerRegistry = false };

            // Act
            var result = options.IsGeneratorEnabled("Handler Registry Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_OptimizedDispatcherGenerator_WhenEnabled_ReturnsTrue()
        {
            // Arrange
            var options = new GenerationOptions { EnableOptimizedDispatcher = true };

            // Act
            var result = options.IsGeneratorEnabled("Optimized Dispatcher Generator");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_OptimizedDispatcherGenerator_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            var options = new GenerationOptions { EnableOptimizedDispatcher = false };

            // Act
            var result = options.IsGeneratorEnabled("Optimized Dispatcher Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_NotificationDispatcherGenerator_WhenEnabled_ReturnsTrue()
        {
            // Arrange
            var options = new GenerationOptions { EnableNotificationDispatcher = true };

            // Act
            var result = options.IsGeneratorEnabled("Notification Dispatcher Generator");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_NotificationDispatcherGenerator_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            var options = new GenerationOptions { EnableNotificationDispatcher = false };

            // Act
            var result = options.IsGeneratorEnabled("Notification Dispatcher Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_PipelineRegistryGenerator_WhenEnabled_ReturnsTrue()
        {
            // Arrange
            var options = new GenerationOptions { EnablePipelineRegistry = true };

            // Act
            var result = options.IsGeneratorEnabled("Pipeline Registry Generator");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_PipelineRegistryGenerator_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            var options = new GenerationOptions { EnablePipelineRegistry = false };

            // Act
            var result = options.IsGeneratorEnabled("Pipeline Registry Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_EndpointMetadataGenerator_WhenEnabled_ReturnsTrue()
        {
            // Arrange
            var options = new GenerationOptions { EnableEndpointMetadata = true };

            // Act
            var result = options.IsGeneratorEnabled("Endpoint Metadata Generator");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_EndpointMetadataGenerator_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            var options = new GenerationOptions { EnableEndpointMetadata = false };

            // Act
            var result = options.IsGeneratorEnabled("Endpoint Metadata Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_UnknownGenerator_ReturnsTrue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            var result = options.IsGeneratorEnabled("Unknown Generator");

            // Assert - Unknown generators are enabled by default
            Assert.True(result);
        }

        #endregion

        #region Default Values Tests

        [Fact]
        public void GenerationOptions_Default_AllGeneratorsEnabled()
        {
            // Arrange & Act
            var options = GenerationOptions.Default;

            // Assert
            Assert.True(options.EnableDIGeneration);
            Assert.True(options.EnableHandlerRegistry);
            Assert.True(options.EnableOptimizedDispatcher);
            Assert.True(options.EnableNotificationDispatcher);
            Assert.True(options.EnablePipelineRegistry);
            Assert.True(options.EnableEndpointMetadata);
        }

        [Fact]
        public void GenerationOptions_Default_GeneralOptionsSetCorrectly()
        {
            // Arrange & Act
            var options = GenerationOptions.Default;

            // Assert
            Assert.False(options.IncludeDebugInfo); // Default is false
            Assert.True(options.IncludeDocumentation); // Default is true
            Assert.True(options.EnableNullableContext); // Default is true
            Assert.True(options.UseAggressiveInlining); // Default is true
            Assert.Null(options.CustomNamespace); // Default is null
            Assert.Equal("Relay.Generated", options.AssemblyName); // Default assembly name
        }

        [Fact]
        public void GenerationOptions_New_HasSameDefaultsAsDefault()
        {
            // Arrange & Act
            var newOptions = new GenerationOptions();
            var defaultOptions = GenerationOptions.Default;

            // Assert - General options
            Assert.Equal(defaultOptions.IncludeDebugInfo, newOptions.IncludeDebugInfo);
            Assert.Equal(defaultOptions.IncludeDocumentation, newOptions.IncludeDocumentation);
            Assert.Equal(defaultOptions.EnableNullableContext, newOptions.EnableNullableContext);
            Assert.Equal(defaultOptions.UseAggressiveInlining, newOptions.UseAggressiveInlining);
            Assert.Equal(defaultOptions.AssemblyName, newOptions.AssemblyName);

            // Assert - Generator flags
            Assert.Equal(defaultOptions.EnableDIGeneration, newOptions.EnableDIGeneration);
            Assert.Equal(defaultOptions.EnableHandlerRegistry, newOptions.EnableHandlerRegistry);
            Assert.Equal(defaultOptions.EnableOptimizedDispatcher, newOptions.EnableOptimizedDispatcher);
            Assert.Equal(defaultOptions.EnableNotificationDispatcher, newOptions.EnableNotificationDispatcher);
            Assert.Equal(defaultOptions.EnablePipelineRegistry, newOptions.EnablePipelineRegistry);
            Assert.Equal(defaultOptions.EnableEndpointMetadata, newOptions.EnableEndpointMetadata);
        }

        #endregion

        #region Custom Configuration Tests

        [Fact]
        public void GenerationOptions_CustomConfiguration_MinimalBuild()
        {
            // Arrange - Minimal build: only DI and Dispatcher
            var options = new GenerationOptions
            {
                EnableDIGeneration = true,
                EnableHandlerRegistry = false,
                EnableOptimizedDispatcher = true,
                EnableNotificationDispatcher = false,
                EnablePipelineRegistry = false,
                EnableEndpointMetadata = false
            };

            // Act & Assert
            Assert.True(options.IsGeneratorEnabled("DI Registration Generator"));
            Assert.False(options.IsGeneratorEnabled("Handler Registry Generator"));
            Assert.True(options.IsGeneratorEnabled("Optimized Dispatcher Generator"));
            Assert.False(options.IsGeneratorEnabled("Notification Dispatcher Generator"));
            Assert.False(options.IsGeneratorEnabled("Pipeline Registry Generator"));
            Assert.False(options.IsGeneratorEnabled("Endpoint Metadata Generator"));
        }

        [Fact]
        public void GenerationOptions_CustomConfiguration_ApiOnlyProject()
        {
            // Arrange - API-only: No notifications or pipelines
            var options = new GenerationOptions
            {
                EnableDIGeneration = true,
                EnableHandlerRegistry = true,
                EnableOptimizedDispatcher = true,
                EnableNotificationDispatcher = false,
                EnablePipelineRegistry = false,
                EnableEndpointMetadata = true
            };

            // Act & Assert
            Assert.True(options.IsGeneratorEnabled("DI Registration Generator"));
            Assert.True(options.IsGeneratorEnabled("Handler Registry Generator"));
            Assert.True(options.IsGeneratorEnabled("Optimized Dispatcher Generator"));
            Assert.False(options.IsGeneratorEnabled("Notification Dispatcher Generator"));
            Assert.False(options.IsGeneratorEnabled("Pipeline Registry Generator"));
            Assert.True(options.IsGeneratorEnabled("Endpoint Metadata Generator"));
        }

        [Fact]
        public void GenerationOptions_CustomConfiguration_LibraryProject()
        {
            // Arrange - Library project: No DI or endpoint metadata
            var options = new GenerationOptions
            {
                EnableDIGeneration = false,
                EnableHandlerRegistry = true,
                EnableOptimizedDispatcher = true,
                EnableNotificationDispatcher = true,
                EnablePipelineRegistry = true,
                EnableEndpointMetadata = false
            };

            // Act & Assert
            Assert.False(options.IsGeneratorEnabled("DI Registration Generator"));
            Assert.True(options.IsGeneratorEnabled("Handler Registry Generator"));
            Assert.True(options.IsGeneratorEnabled("Optimized Dispatcher Generator"));
            Assert.True(options.IsGeneratorEnabled("Notification Dispatcher Generator"));
            Assert.True(options.IsGeneratorEnabled("Pipeline Registry Generator"));
            Assert.False(options.IsGeneratorEnabled("Endpoint Metadata Generator"));
        }

        [Fact]
        public void GenerationOptions_CustomNamespace_ReturnsCustomValue()
        {
            // Arrange
            var options = new GenerationOptions
            {
                CustomNamespace = "MyCompany.MyApp.Generated"
            };

            // Act & Assert
            Assert.Equal("MyCompany.MyApp.Generated", options.CustomNamespace);
        }

        [Fact]
        public void GenerationOptions_AssemblyName_ReturnsCustomValue()
        {
            // Arrange
            var options = new GenerationOptions
            {
                AssemblyName = "MyCompany.MyApp"
            };

            // Act & Assert
            Assert.Equal("MyCompany.MyApp", options.AssemblyName);
        }

        #endregion

        #region Multiple Generators Tests

        [Fact]
        public void IsGeneratorEnabled_MultipleGenerators_MixedConfiguration()
        {
            // Arrange
            var options = new GenerationOptions
            {
                EnableDIGeneration = true,
                EnableHandlerRegistry = false,
                EnableOptimizedDispatcher = true,
                EnableNotificationDispatcher = false,
                EnablePipelineRegistry = true,
                EnableEndpointMetadata = false
            };

            // Act & Assert - Check all generators
            Assert.True(options.IsGeneratorEnabled("DI Registration Generator"));
            Assert.False(options.IsGeneratorEnabled("Handler Registry Generator"));
            Assert.True(options.IsGeneratorEnabled("Optimized Dispatcher Generator"));
            Assert.False(options.IsGeneratorEnabled("Notification Dispatcher Generator"));
            Assert.True(options.IsGeneratorEnabled("Pipeline Registry Generator"));
            Assert.False(options.IsGeneratorEnabled("Endpoint Metadata Generator"));
        }

        [Fact]
        public void IsGeneratorEnabled_AllDisabled_AllReturnFalse()
        {
            // Arrange - Disable all generators
            var options = new GenerationOptions
            {
                EnableDIGeneration = false,
                EnableHandlerRegistry = false,
                EnableOptimizedDispatcher = false,
                EnableNotificationDispatcher = false,
                EnablePipelineRegistry = false,
                EnableEndpointMetadata = false
            };

            // Act & Assert
            Assert.False(options.IsGeneratorEnabled("DI Registration Generator"));
            Assert.False(options.IsGeneratorEnabled("Handler Registry Generator"));
            Assert.False(options.IsGeneratorEnabled("Optimized Dispatcher Generator"));
            Assert.False(options.IsGeneratorEnabled("Notification Dispatcher Generator"));
            Assert.False(options.IsGeneratorEnabled("Pipeline Registry Generator"));
            Assert.False(options.IsGeneratorEnabled("Endpoint Metadata Generator"));
        }

        #endregion
    }
}
