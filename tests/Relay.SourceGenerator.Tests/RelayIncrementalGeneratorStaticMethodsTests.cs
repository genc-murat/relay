using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Core;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Unit tests for static methods in RelayIncrementalGenerator that are not fully covered.
/// Focuses on edge cases and error conditions.
/// </summary>
public class RelayIncrementalGeneratorStaticMethodsTests
{
    #region IsRelayAttributeName Tests

    [Fact]
    public void IsRelayAttributeName_WithNullInput_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithWhitespaceOnly_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithHandleAttribute_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("HandleAttribute");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithNotificationAttribute_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("NotificationAttribute");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithPipelineAttribute_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("PipelineAttribute");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithExposeAsEndpointAttribute_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("ExposeAsEndpointAttribute");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithHandle_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Handle");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithNotification_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Notification");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithPipeline_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Pipeline");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithExposeAsEndpoint_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("ExposeAsEndpoint");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithUnknownAttribute_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("UnknownAttribute");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithUnknownAttributeWithoutSuffix_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Unknown");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithJustAttributeSuffix_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Attribute");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsHandlerInterface Tests

    [Fact]
    public void IsHandlerInterface_WithRequestHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IRequestHandler");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithNotificationHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("INotificationHandler");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithStreamHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IStreamHandler");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithGenericRequestHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IRequestHandler<string>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithGenericNotificationHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("INotificationHandler<MyEvent>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithGenericStreamHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IStreamHandler<Request, Response>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithNonHandlerInterface_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IDisposable");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHandlerInterface_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface(string.Empty);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Interface Detection Methods Tests

    [Fact]
    public void IsRequestHandlerInterface_WithValidInterface_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IRequestHandler`2");

        // Act
        var result = RelayIncrementalGenerator.IsRequestHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNotificationHandlerInterface_WithValidInterface_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.INotificationHandler`1");

        // Act
        var result = RelayIncrementalGenerator.IsNotificationHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsStreamHandlerInterface_WithValidInterface_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IStreamHandler`2");

        // Act
        var result = RelayIncrementalGenerator.IsStreamHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRequestHandlerInterface_WithNonHandlerInterface_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var interfaceSymbol = compilation.GetTypeByMetadataName("System.IDisposable");

        // Act
        var result = RelayIncrementalGenerator.IsRequestHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNotificationHandlerInterface_WithNonHandlerInterface_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var interfaceSymbol = compilation.GetTypeByMetadataName("System.IDisposable");

        // Act
        var result = RelayIncrementalGenerator.IsNotificationHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStreamHandlerInterface_WithNonHandlerInterface_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var interfaceSymbol = compilation.GetTypeByMetadataName("System.IDisposable");

        // Act
        var result = RelayIncrementalGenerator.IsStreamHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    private static CSharpCompilation CreateTestCompilation(string source)
    {
        var syntaxTree = string.IsNullOrEmpty(source)
            ? null
            : CSharpSyntaxTree.ParseText(source);

        var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Contracts.Handlers
{
    public interface IRequestHandler<TRequest, TResponse>
    {
        ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public interface INotificationHandler<TNotification>
    {
        ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }

    public interface IStreamHandler<TRequest, TResponse>
    {
        IAsyncEnumerable<TResponse> HandleStreamAsync(TRequest request, CancellationToken cancellationToken);
    }
}
");

        var trees = syntaxTree == null
            ? new[] { relayCoreStubs }
            : new[] { relayCoreStubs, syntaxTree };

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: trees,
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    #endregion

    #region ParseConfiguration Tests

    [Fact]
    public void ParseConfiguration_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => RelayIncrementalGenerator.ParseConfiguration(null!));
    }

    [Fact]
    public void ParseConfiguration_WithEmptyOptions_ReturnsDefaultConfiguration()
    {
        // Arrange
        var options = new TestAnalyzerConfigOptionsProvider();

        // Act
        var result = RelayIncrementalGenerator.ParseConfiguration(options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ParseConfiguration_WithMSBuildProperties_SetsOptionsCorrectly()
    {
        // Arrange
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.RelayEnableDIGeneration", "false");
        options.Options.Add("build_property.RelayEnableOptimizedDispatcher", "true");
        options.Options.Add("build_property.RelayIncludeDocumentation", "false");

        // Act
        var result = RelayIncrementalGenerator.ParseConfiguration(options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.False(result.Options.EnableDIGeneration);
        Assert.True(result.Options.EnableOptimizedDispatcher);
        Assert.False(result.Options.IncludeDocumentation);
    }

    #endregion

    #region Helper Classes

    private class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public Dictionary<string, string> Options { get; } = new();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return new TestAnalyzerConfigOptions(Options);
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new TestAnalyzerConfigOptions(Options);
        }

        public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(this.Options);
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