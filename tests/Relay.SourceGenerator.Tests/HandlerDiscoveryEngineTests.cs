using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;

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
    public void AnalyzeHandlerMethodWithSymbol_NullMethodSymbol_ReportsDiagnostic()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var engine = new HandlerDiscoveryEngine(context);

        var methodSyntax = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
            "TestMethod")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBody(SyntaxFactory.Block());

        // Act
        var methodInfo = engine.GetType().GetMethod("AnalyzeHandlerMethodWithSymbol",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new System.Type[] { typeof(Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax), typeof(Microsoft.CodeAnalysis.IMethodSymbol), typeof(Relay.SourceGenerator.IDiagnosticReporter) },
            null);
        var result = methodInfo!.Invoke(engine, new object[] { methodSyntax, null!, _mockReporter.Object });

        // Assert
        Assert.Null(result);
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_001" && d.GetMessage().Contains("Could not get symbol for method"))), Times.Once);
    }

    [Fact]
    public void AnalyzeHandlerMethodWithSymbol_ValidMethodSymbol_ReturnsHandlerInfo()
    {
        // Arrange
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
            .First(m => m.Identifier.ValueText == "Handle");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax) as IMethodSymbol;

        // Act
        var result = engine.GetType().GetMethod("AnalyzeHandlerMethodWithSymbol",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(engine, new object[] { methodSyntax, methodSymbol, _mockReporter.Object });

        // Assert
        Assert.NotNull(result);
        Assert.IsType<HandlerInfo>(result);
    }

    private static Compilation CreateCompilation(string source)
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
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}