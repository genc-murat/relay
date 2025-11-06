using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Discovery;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for HandlerDiscoveryEngine methods.
/// </summary>
public class HandlerDiscoveryEngineTests
{
    private readonly Mock<IDiagnosticReporter> _mockReporter;

    public HandlerDiscoveryEngineTests()
    {
        _mockReporter = new Mock<IDiagnosticReporter>();
    }



    [Fact]
    public void DiscoverHandlers_WithEmptyMethodList_ReturnsEmptyResult()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var engine = new HandlerDiscoveryEngine(context);
        var emptyMethods = Array.Empty<MethodDeclarationSyntax>();

        // Act
        var result = engine.DiscoverHandlers(emptyMethods, _mockReporter.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
    }









    [Fact]
    public void DiscoverHandlers_Integration_WithSequentialProcessing_CallsPrivateMethods()
    {
        // Arrange - Create a small list (< 10) to trigger sequential processing
        var source = @"
public class TestRequest : IRequest<string> { }
public class TestHandler
{
    [Handle]
    public string Handle(TestRequest request) => """";
}
";
        var compilation = CreateCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        var engine = new HandlerDiscoveryEngine(context);

        var methodSyntax = compilation.SyntaxTrees.First()
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First();

        // Act
        var result = engine.DiscoverHandlers([methodSyntax], _mockReporter.Object);

        // Assert - Verify the integration works end-to-end
        Assert.Single(result.Handlers);
        var handler = result.Handlers[0];
        Assert.Equal("Handle", handler.MethodName);
        Assert.Single(handler.Attributes);
        Assert.Equal(RelayAttributeType.Handle, handler.Attributes[0].Type);
    }







    [Fact]
    public void DiscoverHandlers_Integration_WithParallelProcessing_CallsPrivateMethods()
    {
        // Arrange - Create a large list (>= 10) to trigger parallel processing
        var source = @"
public class TestRequest : IRequest<string> { }
";
        for (int i = 0; i < 12; i++)
        {
            source += $@"
public class TestHandler{i}
{{
    [Handle]
    public string Handle{i}(TestRequest request) => """";
}}
";
        }
        var compilation = CreateCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        var engine = new HandlerDiscoveryEngine(context);

        var methods = compilation.SyntaxTrees.First()
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .ToList();

        // Act
        var result = engine.DiscoverHandlers(methods, _mockReporter.Object);

        // Assert - Verify parallel processing worked
        Assert.Equal(12, result.Handlers.Count);
        foreach (var handler in result.Handlers)
        {
            Assert.Single(handler.Attributes);
            Assert.Equal(RelayAttributeType.Handle, handler.Attributes[0].Type);
        }
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText($@"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Relay.Core
{{
    public interface IRequest<out TResponse> {{ }}
    public interface IStreamRequest<out TResponse> {{ }}
    public interface INotification {{ }}
    public class HandleAttribute : Attribute
    {{
        public string? Name {{ get; set; }}
        public int Priority {{ get; set; }}
    }}
    public class PipelineAttribute : Attribute
    {{
        public int Order {{ get; set; }}
    }}
    public class NotificationAttribute : Attribute
    {{
        public int Priority {{ get; set; }}
    }}
}}

{source}
");

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}