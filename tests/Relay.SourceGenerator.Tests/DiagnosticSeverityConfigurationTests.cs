using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using System.Collections.Immutable;
using System.Collections.Generic;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Comprehensive tests for DiagnosticSeverityConfiguration class.
/// </summary>
public class DiagnosticSeverityConfigurationTests
{
    #region CreateFromOptions Tests

    [Fact]
    public void CreateFromOptions_WithNoConfiguration_ShouldReturnEmptyConfiguration()
    {
        // Arrange
        var options = CreateEmptyOptionsProvider();

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.NotNull(config);
        Assert.False(config.IsSuppressed("RELAY_GEN_001"));
        Assert.Null(config.GetConfiguredSeverity("RELAY_GEN_001"));
    }

    [Fact]
    public void CreateFromOptions_WithSingleSeverityOverride_ShouldApplyOverride()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_104"] = "error"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.Equal(DiagnosticSeverity.Error, config.GetConfiguredSeverity("RELAY_GEN_104"));
    }

    [Fact]
    public void CreateFromOptions_WithMultipleSeverityOverrides_ShouldApplyAll()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_104"] = "error",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_105"] = "warning",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_106"] = "info"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.Equal(DiagnosticSeverity.Error, config.GetConfiguredSeverity("RELAY_GEN_104"));
        Assert.Equal(DiagnosticSeverity.Warning, config.GetConfiguredSeverity("RELAY_GEN_105"));
        Assert.Equal(DiagnosticSeverity.Info, config.GetConfiguredSeverity("RELAY_GEN_106"));
    }

    [Fact]
    public void CreateFromOptions_WithHiddenSeverity_ShouldSuppressDiagnostic()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_106"] = "hidden"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_106"));
    }

    [Fact]
    public void CreateFromOptions_WithNoneSeverity_ShouldSuppressDiagnostic()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_107"] = "none"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_107"));
    }

    [Fact]
    public void CreateFromOptions_WithSuppressionList_ShouldSuppressAllListed()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106,RELAY_GEN_107,RELAY_GEN_108"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_106"));
        Assert.True(config.IsSuppressed("RELAY_GEN_107"));
        Assert.True(config.IsSuppressed("RELAY_GEN_108"));
        Assert.False(config.IsSuppressed("RELAY_GEN_109"));
    }

    [Fact]
    public void CreateFromOptions_WithSemicolonSeparatedSuppressionList_ShouldSuppressAll()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106;RELAY_GEN_107;RELAY_GEN_108"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_106"));
        Assert.True(config.IsSuppressed("RELAY_GEN_107"));
        Assert.True(config.IsSuppressed("RELAY_GEN_108"));
    }

    [Fact]
    public void CreateFromOptions_WithMixedSeparators_ShouldHandleCorrectly()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106,RELAY_GEN_107;RELAY_GEN_108"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_106"));
        Assert.True(config.IsSuppressed("RELAY_GEN_107"));
        Assert.True(config.IsSuppressed("RELAY_GEN_108"));
    }

    [Fact]
    public void CreateFromOptions_WithWhitespaceInSuppressionList_ShouldTrimAndParse()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = " RELAY_GEN_106 , RELAY_GEN_107 , RELAY_GEN_108 "
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_106"));
        Assert.True(config.IsSuppressed("RELAY_GEN_107"));
        Assert.True(config.IsSuppressed("RELAY_GEN_108"));
    }

    [Fact]
    public void CreateFromOptions_WithEmptySuppressionList_ShouldNotSuppressAnything()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = ""
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.False(config.IsSuppressed("RELAY_GEN_106"));
    }

    [Fact]
    public void CreateFromOptions_WithInvalidSeverityValue_ShouldIgnore()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_104"] = "invalid"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.Null(config.GetConfiguredSeverity("RELAY_GEN_104"));
    }

    [Fact]
    public void CreateFromOptions_WithCaseInsensitiveSeverity_ShouldParse()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_104"] = "ERROR",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_105"] = "Warning",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_106"] = "InFo"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.Equal(DiagnosticSeverity.Error, config.GetConfiguredSeverity("RELAY_GEN_104"));
        Assert.Equal(DiagnosticSeverity.Warning, config.GetConfiguredSeverity("RELAY_GEN_105"));
        Assert.Equal(DiagnosticSeverity.Info, config.GetConfiguredSeverity("RELAY_GEN_106"));
    }

    [Fact]
    public void CreateFromOptions_WithAllNewDiagnostics_ShouldHandleCorrectly()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_213"] = "warning",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_214"] = "info",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_215"] = "error",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_216"] = "hidden"
        };
        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        Assert.Equal(DiagnosticSeverity.Warning, config.GetConfiguredSeverity("RELAY_GEN_213"));
        Assert.Equal(DiagnosticSeverity.Info, config.GetConfiguredSeverity("RELAY_GEN_214"));
        Assert.Equal(DiagnosticSeverity.Error, config.GetConfiguredSeverity("RELAY_GEN_215"));
        Assert.True(config.IsSuppressed("RELAY_GEN_216"));
    }

    #endregion

    #region ApplyConfiguration Tests

    [Fact]
    public void ApplyConfiguration_WithNoOverride_ShouldReturnOriginalDescriptor()
    {
        // Arrange
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateEmptyOptionsProvider());
        var descriptor = DiagnosticDescriptors.InvalidHandlerSignature;

        // Act
        var result = config.ApplyConfiguration(descriptor);

        // Assert
        Assert.Same(descriptor, result);
    }

    [Fact]
    public void ApplyConfiguration_WithSuppressedDiagnostic_ShouldReturnHiddenDescriptor()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));
        var descriptor = DiagnosticDescriptors.PrivateHandler;

        // Act
        var result = config.ApplyConfiguration(descriptor);

        // Assert
        Assert.NotSame(descriptor, result);
        Assert.Equal(DiagnosticSeverity.Hidden, result.DefaultSeverity);
        Assert.False(result.IsEnabledByDefault);
    }

    [Fact]
    public void ApplyConfiguration_WithSeverityOverride_ShouldReturnModifiedDescriptor()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_207"] = "error"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));
        var descriptor = DiagnosticDescriptors.HandlerMissingCancellationToken;

        // Act
        var result = config.ApplyConfiguration(descriptor);

        // Assert
        Assert.NotSame(descriptor, result);
        Assert.Equal(DiagnosticSeverity.Error, result.DefaultSeverity);
        Assert.Equal(descriptor.Id, result.Id);
        Assert.Equal(descriptor.Title.ToString(), result.Title.ToString());
    }

    [Fact]
    public void ApplyConfiguration_WithNullDescriptor_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateEmptyOptionsProvider());

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => config.ApplyConfiguration(null!));
    }

    [Fact]
    public void ApplyConfiguration_ShouldPreserveDescriptorProperties()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_213"] = "warning"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));
        var descriptor = DiagnosticDescriptors.InvalidConfigurationValue;

        // Act
        var result = config.ApplyConfiguration(descriptor);

        // Assert
        Assert.Equal(descriptor.Id, result.Id);
        Assert.Equal(descriptor.Title.ToString(), result.Title.ToString());
        Assert.Equal(descriptor.MessageFormat.ToString(), result.MessageFormat.ToString());
        Assert.Equal(descriptor.Category, result.Category);
        Assert.Equal(DiagnosticSeverity.Warning, result.DefaultSeverity); // Changed
        Assert.Equal(descriptor.IsEnabledByDefault, result.IsEnabledByDefault);
    }

    [Fact]
    public void ApplyConfiguration_WithMultipleDescriptors_ShouldApplyIndependently()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_213"] = "warning",
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_214"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));
        var descriptor1 = DiagnosticDescriptors.InvalidConfigurationValue;
        var descriptor2 = DiagnosticDescriptors.MissingRequiredAttribute;
        var descriptor3 = DiagnosticDescriptors.ObsoleteHandlerPattern;

        // Act
        var result1 = config.ApplyConfiguration(descriptor1);
        var result2 = config.ApplyConfiguration(descriptor2);
        var result3 = config.ApplyConfiguration(descriptor3);

        // Assert
        Assert.Equal(DiagnosticSeverity.Warning, result1.DefaultSeverity);
        Assert.Equal(DiagnosticSeverity.Hidden, result2.DefaultSeverity);
        Assert.Same(descriptor3, result3); // No change
    }

    #endregion

    #region IsSuppressed Tests

    [Fact]
    public void IsSuppressed_WithSuppressedDiagnostic_ShouldReturnTrue()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act & Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_106"));
    }

    [Fact]
    public void IsSuppressed_WithNonSuppressedDiagnostic_ShouldReturnFalse()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act & Assert
        Assert.False(config.IsSuppressed("RELAY_GEN_107"));
    }

    [Fact]
    public void IsSuppressed_WithEmptyConfiguration_ShouldReturnFalse()
    {
        // Arrange
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateEmptyOptionsProvider());

        // Act & Assert
        Assert.False(config.IsSuppressed("RELAY_GEN_106"));
    }

    [Fact]
    public void IsSuppressed_WithNullDiagnosticId_ShouldReturnFalse()
    {
        // Arrange
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateEmptyOptionsProvider());

        // Act & Assert
        Assert.False(config.IsSuppressed(null!));
    }

    [Fact]
    public void IsSuppressed_WithEmptyDiagnosticId_ShouldReturnFalse()
    {
        // Arrange
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateEmptyOptionsProvider());

        // Act & Assert
        Assert.False(config.IsSuppressed(""));
    }

    #endregion

    #region GetConfiguredSeverity Tests

    [Fact]
    public void GetConfiguredSeverity_WithConfiguredDiagnostic_ShouldReturnSeverity()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_104"] = "error"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act
        var severity = config.GetConfiguredSeverity("RELAY_GEN_104");

        // Assert
        Assert.NotNull(severity);
        Assert.Equal(DiagnosticSeverity.Error, severity.Value);
    }

    [Fact]
    public void GetConfiguredSeverity_WithNonConfiguredDiagnostic_ShouldReturnNull()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_104"] = "error"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act
        var severity = config.GetConfiguredSeverity("RELAY_GEN_105");

        // Assert
        Assert.Null(severity);
    }

    [Fact]
    public void GetConfiguredSeverity_WithEmptyConfiguration_ShouldReturnNull()
    {
        // Arrange
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateEmptyOptionsProvider());

        // Act
        var severity = config.GetConfiguredSeverity("RELAY_GEN_104");

        // Assert
        Assert.Null(severity);
    }

    [Fact]
    public void GetConfiguredSeverity_WithAllSeverityLevels_ShouldReturnCorrectValues()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_001"] = "error",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_002"] = "warning",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_003"] = "info",
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_004"] = "hidden"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act & Assert
        Assert.Equal(DiagnosticSeverity.Error, config.GetConfiguredSeverity("RELAY_GEN_001"));
        Assert.Equal(DiagnosticSeverity.Warning, config.GetConfiguredSeverity("RELAY_GEN_002"));
        Assert.Equal(DiagnosticSeverity.Info, config.GetConfiguredSeverity("RELAY_GEN_003"));
        Assert.Null(config.GetConfiguredSeverity("RELAY_GEN_004")); // Hidden goes to suppression
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void Configuration_WithBothSuppressionAndSeverityOverride_SuppressionShouldTakePrecedence()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_106"] = "error",
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act & Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_106"));
        // Severity override should not be applied if suppressed
    }

    [Fact]
    public void Configuration_WithAllDiagnosticIds_ShouldHandleCorrectly()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>();
        var allIds = new[]
        {
            "RELAY_GEN_001", "RELAY_GEN_002", "RELAY_GEN_003", "RELAY_GEN_004", "RELAY_GEN_005",
            "RELAY_GEN_101", "RELAY_GEN_102", "RELAY_GEN_104", "RELAY_GEN_105", "RELAY_GEN_106",
            "RELAY_GEN_107", "RELAY_GEN_108", "RELAY_GEN_109",
            "RELAY_GEN_201", "RELAY_GEN_202", "RELAY_GEN_203", "RELAY_GEN_204", "RELAY_GEN_205",
            "RELAY_GEN_206", "RELAY_GEN_207", "RELAY_GEN_208", "RELAY_GEN_209", "RELAY_GEN_210",
            "RELAY_GEN_211", "RELAY_GEN_212", "RELAY_GEN_213", "RELAY_GEN_214", "RELAY_GEN_215",
            "RELAY_GEN_216"
        };

        foreach (var id in allIds)
        {
            optionsDict[$"build_property.RelayDiagnosticSeverity_{id}"] = "warning";
        }

        var options = CreateOptionsProvider(optionsDict);

        // Act
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(options);

        // Assert
        foreach (var id in allIds)
        {
            Assert.Equal(DiagnosticSeverity.Warning, config.GetConfiguredSeverity(id));
        }
    }

    [Fact]
    public void Configuration_WithDuplicateSuppressionEntries_ShouldHandleGracefully()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106,RELAY_GEN_106,RELAY_GEN_107"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act & Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_106"));
        Assert.True(config.IsSuppressed("RELAY_GEN_107"));
    }

    [Fact]
    public void Configuration_WithVeryLongSuppressionList_ShouldHandleCorrectly()
    {
        // Arrange
        var suppressionList = string.Join(",", System.Linq.Enumerable.Range(1, 100).Select(i => $"RELAY_GEN_{i:D3}"));
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelaySuppressDiagnostics"] = suppressionList
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act & Assert
        Assert.True(config.IsSuppressed("RELAY_GEN_001"));
        Assert.True(config.IsSuppressed("RELAY_GEN_050"));
        Assert.True(config.IsSuppressed("RELAY_GEN_100"));
    }

    [Fact]
    public void Configuration_ThreadSafety_MultipleConcurrentReads()
    {
        // Arrange
        var optionsDict = new Dictionary<string, string>
        {
            ["build_property.RelayDiagnosticSeverity_RELAY_GEN_104"] = "error",
            ["build_property.RelaySuppressDiagnostics"] = "RELAY_GEN_106,RELAY_GEN_107"
        };
        var config = DiagnosticSeverityConfiguration.CreateFromOptions(CreateOptionsProvider(optionsDict));

        // Act - Concurrent reads
        var tasks = new System.Threading.Tasks.Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    Assert.Equal(DiagnosticSeverity.Error, config.GetConfiguredSeverity("RELAY_GEN_104"));
                    Assert.True(config.IsSuppressed("RELAY_GEN_106"));
                    Assert.False(config.IsSuppressed("RELAY_GEN_108"));
                }
            });
        }

        // Assert - No exceptions thrown
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    #endregion

    #region Helper Methods

    private static AnalyzerConfigOptionsProvider CreateEmptyOptionsProvider()
    {
        return new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>());
    }

    private static AnalyzerConfigOptionsProvider CreateOptionsProvider(Dictionary<string, string> options)
    {
        return new TestAnalyzerConfigOptionsProvider(options);
    }

    private class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestAnalyzerConfigOptions _globalOptions;

        public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> options)
        {
            _globalOptions = new TestAnalyzerConfigOptions(options);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _globalOptions;
    }

    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public TestAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value!);
        }
    }

    #endregion
}
