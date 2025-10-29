using Microsoft.CodeAnalysis;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for new diagnostic descriptors added in Task 2.
/// </summary>
public class DiagnosticDescriptorsNewTests
{
    #region InvalidConfigurationValue Tests

    [Fact]
    public void InvalidConfigurationValue_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.InvalidConfigurationValue;

        // Assert
        Assert.Equal("RELAY_GEN_213", descriptor.Id);
        Assert.Equal("Invalid Configuration Value", descriptor.Title.ToString());
        Assert.Equal("Relay.Generator", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
    }

    [Fact]
    public void InvalidConfigurationValue_ShouldHaveDescription()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.InvalidConfigurationValue;

        // Assert
        Assert.NotNull(descriptor.Description);
        Assert.NotEmpty(descriptor.Description.ToString());
        Assert.Contains("configuration property", descriptor.Description.ToString(), System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidConfigurationValue_ShouldHaveHelpLink()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.InvalidConfigurationValue;

        // Assert
        Assert.NotNull(descriptor.HelpLinkUri);
        Assert.Contains("RELAY_GEN_213", descriptor.HelpLinkUri);
        Assert.StartsWith("https://", descriptor.HelpLinkUri);
    }

    [Fact]
    public void InvalidConfigurationValue_MessageFormat_ShouldHaveThreePlaceholders()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.InvalidConfigurationValue;
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "Property", "Value", "Reason");

        // Assert
        var message = diagnostic.GetMessage();
        Assert.Contains("Property", message);
        Assert.Contains("Value", message);
        Assert.Contains("Reason", message);
    }

    #endregion

    #region MissingRequiredAttribute Tests

    [Fact]
    public void MissingRequiredAttribute_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.MissingRequiredAttribute;

        // Assert
        Assert.Equal("RELAY_GEN_214", descriptor.Id);
        Assert.Equal("Missing Required Attribute", descriptor.Title.ToString());
        Assert.Equal("Relay.Generator", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
    }

    [Fact]
    public void MissingRequiredAttribute_ShouldHaveDescription()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.MissingRequiredAttribute;

        // Assert
        Assert.NotNull(descriptor.Description);
        Assert.NotEmpty(descriptor.Description.ToString());
        Assert.Contains("missing a required attribute", descriptor.Description.ToString(), System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingRequiredAttribute_ShouldHaveHelpLink()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.MissingRequiredAttribute;

        // Assert
        Assert.NotNull(descriptor.HelpLinkUri);
        Assert.Contains("RELAY_GEN_214", descriptor.HelpLinkUri);
        Assert.StartsWith("https://", descriptor.HelpLinkUri);
    }

    [Fact]
    public void MissingRequiredAttribute_MessageFormat_ShouldHaveTwoPlaceholders()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.MissingRequiredAttribute;
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "HandleAsync", "Handle");

        // Assert
        var message = diagnostic.GetMessage();
        Assert.Contains("HandleAsync", message);
        Assert.Contains("Handle", message);
        Assert.Contains("Add the attribute", message);
    }

    #endregion

    #region ObsoleteHandlerPattern Tests

    [Fact]
    public void ObsoleteHandlerPattern_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.ObsoleteHandlerPattern;

        // Assert
        Assert.Equal("RELAY_GEN_215", descriptor.Id);
        Assert.Equal("Obsolete Handler Pattern", descriptor.Title.ToString());
        Assert.Equal("Relay.Generator", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
    }

    [Fact]
    public void ObsoleteHandlerPattern_ShouldHaveDescription()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.ObsoleteHandlerPattern;

        // Assert
        Assert.NotNull(descriptor.Description);
        Assert.NotEmpty(descriptor.Description.ToString());
        Assert.Contains("obsolete", descriptor.Description.ToString(), System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ObsoleteHandlerPattern_ShouldHaveHelpLink()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.ObsoleteHandlerPattern;

        // Assert
        Assert.NotNull(descriptor.HelpLinkUri);
        Assert.Contains("RELAY_GEN_215", descriptor.HelpLinkUri);
        Assert.StartsWith("https://", descriptor.HelpLinkUri);
    }

    [Fact]
    public void ObsoleteHandlerPattern_MessageFormat_ShouldHaveThreePlaceholders()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.ObsoleteHandlerPattern;
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "Handler", "OldPattern", "NewPattern");

        // Assert
        var message = diagnostic.GetMessage();
        Assert.Contains("Handler", message);
        Assert.Contains("OldPattern", message);
        Assert.Contains("NewPattern", message);
        Assert.Contains("Consider migrating", message);
    }

    #endregion

    #region PerformanceBottleneck Tests

    [Fact]
    public void PerformanceBottleneck_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.PerformanceBottleneck;

        // Assert
        Assert.Equal("RELAY_GEN_216", descriptor.Id);
        Assert.Equal("Performance Bottleneck Detected", descriptor.Title.ToString());
        Assert.Equal("Relay.Generator", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
    }

    [Fact]
    public void PerformanceBottleneck_ShouldHaveDescription()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.PerformanceBottleneck;

        // Assert
        Assert.NotNull(descriptor.Description);
        Assert.NotEmpty(descriptor.Description.ToString());
        Assert.Contains("performance", descriptor.Description.ToString(), System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PerformanceBottleneck_ShouldHaveHelpLink()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.PerformanceBottleneck;

        // Assert
        Assert.NotNull(descriptor.HelpLinkUri);
        Assert.Contains("RELAY_GEN_216", descriptor.HelpLinkUri);
        Assert.StartsWith("https://", descriptor.HelpLinkUri);
    }

    [Fact]
    public void PerformanceBottleneck_MessageFormat_ShouldHaveThreePlaceholders()
    {
        // Arrange & Act
        var descriptor = DiagnosticDescriptors.PerformanceBottleneck;
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "Handler", "Issue", "Suggestion");

        // Assert
        var message = diagnostic.GetMessage();
        Assert.Contains("Handler", message);
        Assert.Contains("Issue", message);
        Assert.Contains("Suggestion", message);
    }

    #endregion

    #region Consistency Tests

    [Fact]
    public void AllNewDiagnostics_ShouldHaveUniqueIds()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act
        var ids = descriptors.Select(d => d.Id).ToList();

        // Assert
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void AllNewDiagnostics_ShouldHaveRelayGeneratorCategory()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in descriptors)
        {
            Assert.Equal("Relay.Generator", descriptor.Category);
        }
    }

    [Fact]
    public void AllNewDiagnostics_ShouldBeEnabledByDefault()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in descriptors)
        {
            Assert.True(descriptor.IsEnabledByDefault, $"{descriptor.Id} should be enabled by default");
        }
    }

    [Fact]
    public void AllNewDiagnostics_ShouldHaveNonEmptyTitle()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in descriptors)
        {
            Assert.NotNull(descriptor.Title);
            Assert.NotEmpty(descriptor.Title.ToString());
        }
    }

    [Fact]
    public void AllNewDiagnostics_ShouldHaveNonEmptyMessageFormat()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in descriptors)
        {
            Assert.NotNull(descriptor.MessageFormat);
            Assert.NotEmpty(descriptor.MessageFormat.ToString());
        }
    }

    [Fact]
    public void AllNewDiagnostics_ShouldHaveDescription()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in descriptors)
        {
            Assert.NotNull(descriptor.Description);
            Assert.NotEmpty(descriptor.Description.ToString());
        }
    }

    [Fact]
    public void AllNewDiagnostics_ShouldHaveHelpLink()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in descriptors)
        {
            Assert.NotNull(descriptor.HelpLinkUri);
            Assert.NotEmpty(descriptor.HelpLinkUri);
            Assert.StartsWith("https://", descriptor.HelpLinkUri);
            Assert.Contains(descriptor.Id, descriptor.HelpLinkUri);
        }
    }

    [Fact]
    public void ErrorDiagnostics_ShouldHaveErrorSeverity()
    {
        // Arrange
        var errorDescriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute
        };

        // Act & Assert
        foreach (var descriptor in errorDescriptors)
        {
            Assert.Equal(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
        }
    }

    [Fact]
    public void WarningDiagnostics_ShouldHaveWarningSeverity()
    {
        // Arrange
        var warningDescriptors = new[]
        {
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in warningDescriptors)
        {
            Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        }
    }

    [Fact]
    public void AllNewDiagnostics_IdsShouldFollowConvention()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in descriptors)
        {
            Assert.StartsWith("RELAY_GEN_", descriptor.Id);
            Assert.Matches(@"^RELAY_GEN_\d{3}$", descriptor.Id);
        }
    }

    [Fact]
    public void AllNewDiagnostics_ShouldBeInCorrectRange()
    {
        // Arrange
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert - New diagnostics should be in 213-216 range
        var expectedIds = new[] { "RELAY_GEN_213", "RELAY_GEN_214", "RELAY_GEN_215", "RELAY_GEN_216" };
        var actualIds = descriptors.Select(d => d.Id).OrderBy(id => id).ToArray();
        Assert.Equal(expectedIds, actualIds);
    }

    #endregion

    #region Diagnostic Creation Tests

    [Fact]
    public void CreateDiagnostic_InvalidConfigurationValue_ShouldFormatCorrectly()
    {
        // Arrange
        var descriptor = DiagnosticDescriptors.InvalidConfigurationValue;

        // Act
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "MaxRetries", "abc", "Must be numeric");

        // Assert
        Assert.Equal("RELAY_GEN_213", diagnostic.Id);
        Assert.Contains("MaxRetries", diagnostic.GetMessage());
        Assert.Contains("abc", diagnostic.GetMessage());
        Assert.Contains("Must be numeric", diagnostic.GetMessage());
    }

    [Fact]
    public void CreateDiagnostic_MissingRequiredAttribute_ShouldFormatCorrectly()
    {
        // Arrange
        var descriptor = DiagnosticDescriptors.MissingRequiredAttribute;

        // Act
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "ProcessAsync", "HandleAttribute");

        // Assert
        Assert.Equal("RELAY_GEN_214", diagnostic.Id);
        Assert.Contains("ProcessAsync", diagnostic.GetMessage());
        Assert.Contains("HandleAttribute", diagnostic.GetMessage());
    }

    [Fact]
    public void CreateDiagnostic_ObsoleteHandlerPattern_ShouldFormatCorrectly()
    {
        // Arrange
        var descriptor = DiagnosticDescriptors.ObsoleteHandlerPattern;

        // Act
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "MyHandler", "Sync methods", "Use async");

        // Assert
        Assert.Equal("RELAY_GEN_215", diagnostic.Id);
        Assert.Contains("MyHandler", diagnostic.GetMessage());
        Assert.Contains("Sync methods", diagnostic.GetMessage());
        Assert.Contains("Use async", diagnostic.GetMessage());
    }

    [Fact]
    public void CreateDiagnostic_PerformanceBottleneck_ShouldFormatCorrectly()
    {
        // Arrange
        var descriptor = DiagnosticDescriptors.PerformanceBottleneck;

        // Act
        var diagnostic = Diagnostic.Create(descriptor, Location.None, "DataHandler", "N+1 queries", "Use batching");

        // Assert
        Assert.Equal("RELAY_GEN_216", diagnostic.Id);
        Assert.Contains("DataHandler", diagnostic.GetMessage());
        Assert.Contains("N+1 queries", diagnostic.GetMessage());
        Assert.Contains("Use batching", diagnostic.GetMessage());
    }

    #endregion

    #region Comparison with Existing Diagnostics

    [Fact]
    public void NewDiagnostics_ShouldNotConflictWithExistingIds()
    {
        // Arrange
        var existingDescriptors = new[]
        {
            DiagnosticDescriptors.GeneratorError,
            DiagnosticDescriptors.InvalidHandlerSignature,
            DiagnosticDescriptors.DuplicateHandler,
            DiagnosticDescriptors.MissingRelayCoreReference,
            DiagnosticDescriptors.NamedHandlerConflict,
            DiagnosticDescriptors.UnusedHandler,
            DiagnosticDescriptors.PerformanceWarning,
            DiagnosticDescriptors.DuplicatePipelineOrder,
            DiagnosticDescriptors.InvalidHandlerReturnType,
            DiagnosticDescriptors.HandlerMissingCancellationToken
        };

        var newDescriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act
        var existingIds = existingDescriptors.Select(d => d.Id).ToHashSet();
        var newIds = newDescriptors.Select(d => d.Id).ToList();

        // Assert
        foreach (var newId in newIds)
        {
            Assert.DoesNotContain(newId, existingIds);
        }
    }

    [Fact]
    public void NewDiagnostics_ShouldFollowSameCategoryAsExisting()
    {
        // Arrange
        var existingCategory = DiagnosticDescriptors.GeneratorError.Category;
        var newDescriptors = new[]
        {
            DiagnosticDescriptors.InvalidConfigurationValue,
            DiagnosticDescriptors.MissingRequiredAttribute,
            DiagnosticDescriptors.ObsoleteHandlerPattern,
            DiagnosticDescriptors.PerformanceBottleneck
        };

        // Act & Assert
        foreach (var descriptor in newDescriptors)
        {
            Assert.Equal(existingCategory, descriptor.Category);
        }
    }

    #endregion
}
