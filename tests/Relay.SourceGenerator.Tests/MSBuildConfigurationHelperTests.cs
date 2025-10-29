using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for MSBuildConfigurationHelper to verify MSBuild property reading.
/// </summary>
public class MSBuildConfigurationHelperTests
{
    #region Test Helper Classes

    /// <summary>
    /// Mock implementation of AnalyzerConfigOptions for testing.
    /// </summary>
    private class MockAnalyzerConfigOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options = options ?? [];

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value!);
        }
    }

    /// <summary>
    /// Mock implementation of AnalyzerConfigOptionsProvider for testing.
    /// </summary>
    private class MockAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions = new MockAnalyzerConfigOptions(globalOptions);

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return new MockAnalyzerConfigOptions([]);
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new MockAnalyzerConfigOptions([]);
        }
    }

    #endregion

    #region Boolean Property Tests

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_TrueValue_ReturnsTrue()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = "true"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.True(result.IncludeDebugInfo);
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_FalseValue_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.IncludeDebugInfo);
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_YesValue_ReturnsTrue()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = "yes"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.True(result.IncludeDebugInfo);
    }

    [Fact]
    public void CreateFromMSBuildProperties_IntegerProperty_ValidValue_ReturnsValue()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayMaxDegreeOfParallelism"] = "8"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.Equal(8, result.MaxDegreeOfParallelism);
    }

    [Fact]
    public void CreateFromMSBuildProperties_IntegerProperty_InvalidValue_ReturnsDefault()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayMaxDegreeOfParallelism"] = "invalid"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.Equal(4, result.MaxDegreeOfParallelism); // Default value
    }

    [Fact]
    public void CreateFromMSBuildProperties_IntegerProperty_NotSet_ReturnsDefault()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider([]);

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.Equal(4, result.MaxDegreeOfParallelism); // Default value
    }

    [Fact]
    public void CreateFromMSBuildProperties_PerformanceOptimizations_True_ReturnsTrue()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnablePerformanceOptimizations"] = "true"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.True(result.EnablePerformanceOptimizations);
    }

    [Fact]
    public void CreateFromMSBuildProperties_KeyedServices_False_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableKeyedServices"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnableKeyedServices);
    }

    [Fact]
    public void CreateFromMSBuildProperties_AggressiveInlining_False_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableAggressiveInlining"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnableAggressiveInlining);
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_NoValue_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = "no"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.IncludeDebugInfo);
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_OneValue_ReturnsTrue()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = "1"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.True(result.IncludeDebugInfo);
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_ZeroValue_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = "0"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.IncludeDebugInfo);
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_NotSet_ReturnsDefault()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider([]);

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.IncludeDebugInfo); // Default is false
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_MalformedString_FallbackToDefault()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = "truee" // Malformed boolean string
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.IncludeDebugInfo); // Fallback to default false
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_EmptyString_FallbackToDefault()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = "" // Empty string
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.IncludeDebugInfo); // Fallback to default false
    }

    [Fact]
    public void CreateFromMSBuildProperties_BooleanProperty_NullString_FallbackToDefault()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDebugInfo"] = null! // Null string
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.IncludeDebugInfo); // Fallback to default false
    }

    #endregion

    #region String Property Tests

    [Fact]
    public void CreateFromMSBuildProperties_StringProperty_ReturnsValue()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayCustomNamespace"] = "MyApp.Generated"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.Equal("MyApp.Generated", result.CustomNamespace);
    }

    [Fact]
    public void CreateFromMSBuildProperties_StringProperty_NotSet_ReturnsNull()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider([]);

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.Null(result.CustomNamespace);
    }

    [Fact]
    public void CreateFromMSBuildProperties_AssemblyName_ReturnsValue()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.AssemblyName"] = "MyTestAssembly"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.Equal("MyTestAssembly", result.AssemblyName);
    }

    [Fact]
    public void CreateFromMSBuildProperties_AssemblyName_NotSet_ReturnsDefault()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider([]);

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.Equal("Relay.Generated", result.AssemblyName);
    }

    #endregion

    #region Generator Enable/Disable Flag Tests

    [Fact]
    public void CreateFromMSBuildProperties_DIGeneration_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableDIGeneration"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnableDIGeneration);
    }

    [Fact]
    public void CreateFromMSBuildProperties_HandlerRegistry_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableHandlerRegistry"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnableHandlerRegistry);
    }

    [Fact]
    public void CreateFromMSBuildProperties_OptimizedDispatcher_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableOptimizedDispatcher"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnableOptimizedDispatcher);
    }

    [Fact]
    public void CreateFromMSBuildProperties_NotificationDispatcher_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableNotificationDispatcher"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnableNotificationDispatcher);
    }

    [Fact]
    public void CreateFromMSBuildProperties_PipelineRegistry_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnablePipelineRegistry"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnablePipelineRegistry);
    }

    [Fact]
    public void CreateFromMSBuildProperties_EndpointMetadata_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableEndpointMetadata"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnableEndpointMetadata);
    }

    [Fact]
    public void CreateFromMSBuildProperties_AllGenerators_NotSet_ReturnsTrueByDefault()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider([]);

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert - All generators should be enabled by default
        Assert.True(result.EnableDIGeneration);
        Assert.True(result.EnableHandlerRegistry);
        Assert.True(result.EnableOptimizedDispatcher);
        Assert.True(result.EnableNotificationDispatcher);
        Assert.True(result.EnablePipelineRegistry);
        Assert.True(result.EnableEndpointMetadata);
    }

    #endregion

    #region General Options Tests

    [Fact]
    public void CreateFromMSBuildProperties_IncludeDocumentation_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayIncludeDocumentation"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.IncludeDocumentation);
    }

    [Fact]
    public void CreateFromMSBuildProperties_EnableNullableContext_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableNullableContext"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.EnableNullableContext);
    }

    [Fact]
    public void CreateFromMSBuildProperties_UseAggressiveInlining_Disabled_ReturnsFalse()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayUseAggressiveInlining"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert
        Assert.False(result.UseAggressiveInlining);
    }

    #endregion

    #region Complete Configuration Tests

    [Fact]
    public void CreateFromMSBuildProperties_CompleteConfiguration_AllValuesSet()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            // General options
            ["build_property.RelayIncludeDebugInfo"] = "true",
            ["build_property.RelayIncludeDocumentation"] = "false",
            ["build_property.RelayEnableNullableContext"] = "false",
            ["build_property.RelayUseAggressiveInlining"] = "false",
            ["build_property.RelayCustomNamespace"] = "MyCompany.Api.Generated",
            ["build_property.AssemblyName"] = "MyCompany.Api",

            // Generator flags
            ["build_property.RelayEnableDIGeneration"] = "true",
            ["build_property.RelayEnableHandlerRegistry"] = "false",
            ["build_property.RelayEnableOptimizedDispatcher"] = "true",
            ["build_property.RelayEnableNotificationDispatcher"] = "false",
            ["build_property.RelayEnablePipelineRegistry"] = "false",
            ["build_property.RelayEnableEndpointMetadata"] = "true"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert - General options
        Assert.True(result.IncludeDebugInfo);
        Assert.False(result.IncludeDocumentation);
        Assert.False(result.EnableNullableContext);
        Assert.False(result.UseAggressiveInlining);
        Assert.Equal("MyCompany.Api.Generated", result.CustomNamespace);
        Assert.Equal("MyCompany.Api", result.AssemblyName);

        // Assert - Generator flags
        Assert.True(result.EnableDIGeneration);
        Assert.False(result.EnableHandlerRegistry);
        Assert.True(result.EnableOptimizedDispatcher);
        Assert.False(result.EnableNotificationDispatcher);
        Assert.False(result.EnablePipelineRegistry);
        Assert.True(result.EnableEndpointMetadata);
    }

    [Fact]
    public void CreateFromMSBuildProperties_MinimalConfiguration_OnlyDisableOptionalGenerators()
    {
        // Arrange - Typical scenario: disable only endpoint metadata and pipeline registry
        var options = new MockAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.RelayEnableEndpointMetadata"] = "false",
            ["build_property.RelayEnablePipelineRegistry"] = "false"
        });

        // Act
        var result = MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        // Assert - Core generators still enabled
        Assert.True(result.EnableDIGeneration);
        Assert.True(result.EnableHandlerRegistry);
        Assert.True(result.EnableOptimizedDispatcher);
        Assert.True(result.EnableNotificationDispatcher);

        // Assert - Optional generators disabled
        Assert.False(result.EnablePipelineRegistry);
        Assert.False(result.EnableEndpointMetadata);
    }

    #endregion
}
