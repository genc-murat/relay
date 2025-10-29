using Relay.SourceGenerator.Generators;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Comprehensive tests for GenerationOptions class covering all properties and methods.
    /// </summary>
    public class GenerationOptionsComprehensiveTests
    {
        #region Property Tests

        [Fact]
        public void IncludeDebugInfo_DefaultValue_ShouldBeFalse()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.False(options.IncludeDebugInfo);
        }

        [Fact]
        public void IncludeDebugInfo_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.IncludeDebugInfo = true;

            // Assert
            Assert.True(options.IncludeDebugInfo);
        }

        [Fact]
        public void IncludeDocumentation_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.IncludeDocumentation);
        }

        [Fact]
        public void IncludeDocumentation_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.IncludeDocumentation = false;

            // Assert
            Assert.False(options.IncludeDocumentation);
        }

        [Fact]
        public void EnableNullableContext_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnableNullableContext);
        }

        [Fact]
        public void EnableNullableContext_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnableNullableContext = false;

            // Assert
            Assert.False(options.EnableNullableContext);
        }

        [Fact]
        public void CustomNamespace_DefaultValue_ShouldBeNull()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.Null(options.CustomNamespace);
        }

        [Fact]
        public void CustomNamespace_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.CustomNamespace = "MyCustomNamespace";

            // Assert
            Assert.Equal("MyCustomNamespace", options.CustomNamespace);
        }

        [Fact]
        public void UseAggressiveInlining_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.UseAggressiveInlining);
        }

        [Fact]
        public void UseAggressiveInlining_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.UseAggressiveInlining = false;

            // Assert
            Assert.False(options.UseAggressiveInlining);
        }

        [Fact]
        public void EnableAggressiveInlining_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnableAggressiveInlining);
        }

        [Fact]
        public void EnableAggressiveInlining_SetValue_ShouldUpdateUseAggressiveInlining()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnableAggressiveInlining = false;

            // Assert
            Assert.False(options.EnableAggressiveInlining);
            Assert.False(options.UseAggressiveInlining);
        }

        [Fact]
        public void EnableAggressiveInlining_GetValue_ShouldReturnUseAggressiveInlining()
        {
            // Arrange
            var options = new GenerationOptions
            {
                UseAggressiveInlining = false
            };

            // Act & Assert
            Assert.False(options.EnableAggressiveInlining);
        }

        [Fact]
        public void AssemblyName_DefaultValue_ShouldBeRelayGenerated()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.Equal("Relay.Generated", options.AssemblyName);
        }

        [Fact]
        public void AssemblyName_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.AssemblyName = "MyAssembly";

            // Assert
            Assert.Equal("MyAssembly", options.AssemblyName);
        }

        [Fact]
        public void EnablePerformanceOptimizations_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnablePerformanceOptimizations);
        }

        [Fact]
        public void EnablePerformanceOptimizations_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnablePerformanceOptimizations = false;

            // Assert
            Assert.False(options.EnablePerformanceOptimizations);
        }

        [Fact]
        public void MaxDegreeOfParallelism_DefaultValue_ShouldBeFour()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.Equal(4, options.MaxDegreeOfParallelism);
        }

        [Fact]
        public void MaxDegreeOfParallelism_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.MaxDegreeOfParallelism = 8;

            // Assert
            Assert.Equal(8, options.MaxDegreeOfParallelism);
        }

        [Fact]
        public void MaxDegreeOfParallelism_SetToOne_ShouldAllowSequentialExecution()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.MaxDegreeOfParallelism = 1;

            // Assert
            Assert.Equal(1, options.MaxDegreeOfParallelism);
        }

        [Fact]
        public void EnableKeyedServices_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnableKeyedServices);
        }

        [Fact]
        public void EnableKeyedServices_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnableKeyedServices = false;

            // Assert
            Assert.False(options.EnableKeyedServices);
        }

        [Fact]
        public void EnableDIGeneration_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnableDIGeneration);
        }

        [Fact]
        public void EnableDIGeneration_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnableDIGeneration = false;

            // Assert
            Assert.False(options.EnableDIGeneration);
        }

        [Fact]
        public void EnableHandlerRegistry_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnableHandlerRegistry);
        }

        [Fact]
        public void EnableHandlerRegistry_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnableHandlerRegistry = false;

            // Assert
            Assert.False(options.EnableHandlerRegistry);
        }

        [Fact]
        public void EnableOptimizedDispatcher_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnableOptimizedDispatcher);
        }

        [Fact]
        public void EnableOptimizedDispatcher_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnableOptimizedDispatcher = false;

            // Assert
            Assert.False(options.EnableOptimizedDispatcher);
        }

        [Fact]
        public void EnableNotificationDispatcher_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnableNotificationDispatcher);
        }

        [Fact]
        public void EnableNotificationDispatcher_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnableNotificationDispatcher = false;

            // Assert
            Assert.False(options.EnableNotificationDispatcher);
        }

        [Fact]
        public void EnablePipelineRegistry_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnablePipelineRegistry);
        }

        [Fact]
        public void EnablePipelineRegistry_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnablePipelineRegistry = false;

            // Assert
            Assert.False(options.EnablePipelineRegistry);
        }

        [Fact]
        public void EnableEndpointMetadata_DefaultValue_ShouldBeTrue()
        {
            // Arrange & Act
            var options = new GenerationOptions();

            // Assert
            Assert.True(options.EnableEndpointMetadata);
        }

        [Fact]
        public void EnableEndpointMetadata_SetValue_ShouldReturnSetValue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            options.EnableEndpointMetadata = false;

            // Assert
            Assert.False(options.EnableEndpointMetadata);
        }

        #endregion

        #region IsGeneratorEnabled Tests

        [Fact]
        public void IsGeneratorEnabled_WithDIRegistrationGenerator_ShouldReturnEnableDIGeneration()
        {
            // Arrange
            var options = new GenerationOptions { EnableDIGeneration = false };

            // Act
            var result = options.IsGeneratorEnabled("DI Registration Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithHandlerRegistryGenerator_ShouldReturnEnableHandlerRegistry()
        {
            // Arrange
            var options = new GenerationOptions { EnableHandlerRegistry = false };

            // Act
            var result = options.IsGeneratorEnabled("Handler Registry Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithOptimizedDispatcherGenerator_ShouldReturnEnableOptimizedDispatcher()
        {
            // Arrange
            var options = new GenerationOptions { EnableOptimizedDispatcher = false };

            // Act
            var result = options.IsGeneratorEnabled("Optimized Dispatcher Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithNotificationDispatcherGenerator_ShouldReturnEnableNotificationDispatcher()
        {
            // Arrange
            var options = new GenerationOptions { EnableNotificationDispatcher = false };

            // Act
            var result = options.IsGeneratorEnabled("Notification Dispatcher Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithPipelineRegistryGenerator_ShouldReturnEnablePipelineRegistry()
        {
            // Arrange
            var options = new GenerationOptions { EnablePipelineRegistry = false };

            // Act
            var result = options.IsGeneratorEnabled("Pipeline Registry Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithEndpointMetadataGenerator_ShouldReturnEnableEndpointMetadata()
        {
            // Arrange
            var options = new GenerationOptions { EnableEndpointMetadata = false };

            // Act
            var result = options.IsGeneratorEnabled("Endpoint Metadata Generator");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithUnknownGenerator_ShouldReturnTrue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            var result = options.IsGeneratorEnabled("Unknown Generator");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithNullGeneratorName_ShouldReturnTrue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            var result = options.IsGeneratorEnabled(null!);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithEmptyGeneratorName_ShouldReturnTrue()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act
            var result = options.IsGeneratorEnabled(string.Empty);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsGeneratorEnabled_WithAllGeneratorsEnabled_ShouldReturnTrueForAll()
        {
            // Arrange
            var options = new GenerationOptions
            {
                EnableDIGeneration = true,
                EnableHandlerRegistry = true,
                EnableOptimizedDispatcher = true,
                EnableNotificationDispatcher = true,
                EnablePipelineRegistry = true,
                EnableEndpointMetadata = true
            };

            // Act & Assert
            Assert.True(options.IsGeneratorEnabled("DI Registration Generator"));
            Assert.True(options.IsGeneratorEnabled("Handler Registry Generator"));
            Assert.True(options.IsGeneratorEnabled("Optimized Dispatcher Generator"));
            Assert.True(options.IsGeneratorEnabled("Notification Dispatcher Generator"));
            Assert.True(options.IsGeneratorEnabled("Pipeline Registry Generator"));
            Assert.True(options.IsGeneratorEnabled("Endpoint Metadata Generator"));
        }

        [Fact]
        public void IsGeneratorEnabled_WithAllGeneratorsDisabled_ShouldReturnFalseForAll()
        {
            // Arrange
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

        #region Default Static Property Tests

        [Fact]
        public void Default_ShouldReturnNewInstanceWithDefaultValues()
        {
            // Act
            var options = GenerationOptions.Default;

            // Assert
            Assert.NotNull(options);
            Assert.False(options.IncludeDebugInfo);
            Assert.True(options.IncludeDocumentation);
            Assert.True(options.EnableNullableContext);
            Assert.Null(options.CustomNamespace);
            Assert.True(options.UseAggressiveInlining);
            Assert.Equal("Relay.Generated", options.AssemblyName);
            Assert.True(options.EnablePerformanceOptimizations);
            Assert.Equal(4, options.MaxDegreeOfParallelism);
            Assert.True(options.EnableKeyedServices);
        }

        [Fact]
        public void Default_ShouldReturnNewInstanceEachTime()
        {
            // Act
            var options1 = GenerationOptions.Default;
            var options2 = GenerationOptions.Default;

            // Assert
            Assert.NotSame(options1, options2);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void GenerationOptions_WithAllPropertiesSet_ShouldMaintainValues()
        {
            // Arrange & Act
            var options = new GenerationOptions
            {
                IncludeDebugInfo = true,
                IncludeDocumentation = false,
                EnableNullableContext = false,
                CustomNamespace = "Custom.Namespace",
                UseAggressiveInlining = false,
                AssemblyName = "CustomAssembly",
                EnablePerformanceOptimizations = false,
                MaxDegreeOfParallelism = 16,
                EnableKeyedServices = false,
                EnableDIGeneration = false,
                EnableHandlerRegistry = false,
                EnableOptimizedDispatcher = false,
                EnableNotificationDispatcher = false,
                EnablePipelineRegistry = false,
                EnableEndpointMetadata = false
            };

            // Assert
            Assert.True(options.IncludeDebugInfo);
            Assert.False(options.IncludeDocumentation);
            Assert.False(options.EnableNullableContext);
            Assert.Equal("Custom.Namespace", options.CustomNamespace);
            Assert.False(options.UseAggressiveInlining);
            Assert.Equal("CustomAssembly", options.AssemblyName);
            Assert.False(options.EnablePerformanceOptimizations);
            Assert.Equal(16, options.MaxDegreeOfParallelism);
            Assert.False(options.EnableKeyedServices);
            Assert.False(options.EnableDIGeneration);
            Assert.False(options.EnableHandlerRegistry);
            Assert.False(options.EnableOptimizedDispatcher);
            Assert.False(options.EnableNotificationDispatcher);
            Assert.False(options.EnablePipelineRegistry);
            Assert.False(options.EnableEndpointMetadata);
        }

        [Fact]
        public void GenerationOptions_EnableAggressiveInlining_ShouldSyncWithUseAggressiveInlining()
        {
            // Arrange
            var options = new GenerationOptions();

            // Act - Set via EnableAggressiveInlining
            options.EnableAggressiveInlining = false;

            // Assert
            Assert.False(options.UseAggressiveInlining);
            Assert.False(options.EnableAggressiveInlining);

            // Act - Set via UseAggressiveInlining
            options.UseAggressiveInlining = true;

            // Assert
            Assert.True(options.EnableAggressiveInlining);
            Assert.True(options.UseAggressiveInlining);
        }

        #endregion
    }
}
