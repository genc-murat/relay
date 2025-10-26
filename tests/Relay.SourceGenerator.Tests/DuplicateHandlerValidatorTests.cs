using Microsoft.CodeAnalysis;
#pragma warning disable CS0618 // CompilationAnalysisContext constructor is obsolete but still used in tests
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Moq;
using Relay.SourceGenerator.Validators;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for DuplicateHandlerValidator methods.
/// </summary>
public class DuplicateHandlerValidatorTests
{
    private readonly Mock<IDiagnosticReporter> _mockReporter;

    public DuplicateHandlerValidatorTests()
    {
        _mockReporter = new Mock<IDiagnosticReporter>();
    }

    [Fact]
    public void ValidateDuplicateHandlers_WithDuplicateUnnamedHandlers_ReportsDiagnostic()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            public class TestRequest : IRequest<string> { }
            public class TestHandler
            {
                [Handle] public string H1(TestRequest r, CancellationToken ct) => """";
                [Handle] public string H2(TestRequest r, CancellationToken ct) => """";
            }
        ");

        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method1 = handlerType.GetMembers("H1").OfType<IMethodSymbol>().First();
        var method2 = handlerType.GetMembers("H2").OfType<IMethodSymbol>().First();
        var methodDecl1 = GetMethodDeclaration(compilation, "H1");
        var methodDecl2 = GetMethodDeclaration(compilation, "H2");
        var attribute1 = method1.GetAttributes().First();
        var attribute2 = method2.GetAttributes().First();

        var registry = new HandlerRegistry();
        registry.AddHandler(method1, attribute1, methodDecl1);
        registry.AddHandler(method2, attribute2, methodDecl2);

        var options = new AnalyzerOptions([]);
        var context = new CompilationAnalysisContext(compilation, options, d => _mockReporter.Object.ReportDiagnostic(d), _ => true, default);

        // Act
        DuplicateHandlerValidator.ValidateDuplicateHandlers(context, registry);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_003")), Times.Exactly(2)); // DuplicateHandler
    }

    [Fact]
    public void ValidateDuplicateHandlers_WithDuplicateNamedHandlers_ReportsDiagnostic()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            namespace Test
            {
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Name = ""SameName"")] public string H1(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""SameName"")] public string H2(TestRequest r, CancellationToken ct) => """";
                }
            }
        ");

        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method1 = handlerType.GetMembers("H1").OfType<IMethodSymbol>().First();
        var method2 = handlerType.GetMembers("H2").OfType<IMethodSymbol>().First();
        var methodDecl1 = GetMethodDeclaration(compilation, "H1");
        var methodDecl2 = GetMethodDeclaration(compilation, "H2");
        var attribute1 = method1.GetAttributes().First();
        var attribute2 = method2.GetAttributes().First();

        var registry = new HandlerRegistry();
        registry.AddHandler(method1, attribute1, methodDecl1);
        registry.AddHandler(method2, attribute2, methodDecl2);

        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
        var context = new CompilationAnalysisContext(compilation, options, d => _mockReporter.Object.ReportDiagnostic(d), _ => true, default);

        // Act
        DuplicateHandlerValidator.ValidateDuplicateHandlers(context, registry);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_003")), Times.Exactly(2)); // DuplicateHandler
    }

    [Fact]
    public void ValidateDuplicateHandlers_WithMixedNamedAndUnnamedHandlers_ReportsDiagnostic()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            public class TestRequest : IRequest<string> { }
            public class TestHandler
            {
                [Handle] public string H1(TestRequest r, CancellationToken ct) => """";
                [Handle(""Named"")] public string H2(TestRequest r, CancellationToken ct) => """";
            }
        ");

        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method1 = handlerType.GetMembers("H1").OfType<IMethodSymbol>().First();
        var method2 = handlerType.GetMembers("H2").OfType<IMethodSymbol>().First();
        var methodDecl1 = GetMethodDeclaration(compilation, "H1");
        var methodDecl2 = GetMethodDeclaration(compilation, "H2");
        var attribute1 = method1.GetAttributes().First();
        var attribute2 = method2.GetAttributes().First();

        var registry = new HandlerRegistry();
        registry.AddHandler(method1, attribute1, methodDecl1);
        registry.AddHandler(method2, attribute2, methodDecl2);

        // Manually set names since attribute parsing doesn't work in test
        registry.Handlers[0].Name = "default";
        registry.Handlers[1].Name = "Named";

        var options = new AnalyzerOptions([]);
        var context = new CompilationAnalysisContext(compilation, options, d => _mockReporter.Object.ReportDiagnostic(d), _ => true, default);

        // Act
        DuplicateHandlerValidator.ValidateDuplicateHandlers(context, registry);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_211")), Times.Once); // ConfigurationConflict
    }

    [Fact]
    public void ValidateDuplicateHandlers_WithVeryLowPriority_ReportsWarning()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            public class TestRequest : IRequest<string> { }
            public class TestHandler
            {
                [Handle(Priority = -1500)] public string H1(TestRequest r, CancellationToken ct) => """";
            }
        ");

        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("H1").OfType<IMethodSymbol>().First();
        var methodDecl = GetMethodDeclaration(compilation, "H1");
        var attribute = method.GetAttributes().First();

        var registry = new HandlerRegistry();
        registry.AddHandler(method, attribute, methodDecl);

        // Manually set priority since attribute parsing doesn't work in test
        registry.Handlers[0].Priority = -1500;

        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
        var context = new CompilationAnalysisContext(compilation, options, d => _mockReporter.Object.ReportDiagnostic(d), _ => true, default);

        // Act
        DuplicateHandlerValidator.ValidateDuplicateHandlers(context, registry);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_102" && d.GetMessage().Contains("Very low priority"))), Times.Once);
    }

    [Fact]
    public void ValidateDuplicateHandlers_WithVeryHighPriority_ReportsWarning()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            public class TestRequest : IRequest<string> { }
            public class TestHandler
            {
                [Handle(Priority = 1500)] public string H1(TestRequest r, CancellationToken ct) => """";
            }
        ");

        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("H1").OfType<IMethodSymbol>().First();
        var methodDecl = GetMethodDeclaration(compilation, "H1");
        var attribute = method.GetAttributes().First();

        var registry = new HandlerRegistry();
        registry.AddHandler(method, attribute, methodDecl);

        // Manually set priority since attribute parsing doesn't work in test
        registry.Handlers[0].Priority = 1500;

        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
        var context = new CompilationAnalysisContext(compilation, options, d => _mockReporter.Object.ReportDiagnostic(d), _ => true, default);

        // Act
        DuplicateHandlerValidator.ValidateDuplicateHandlers(context, registry);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_102" && d.GetMessage().Contains("Very high priority"))), Times.Once);
    }

    [Fact]
    public void ValidateDuplicateHandlers_WithCommonName_ReportsWarning()
    {
        // Arrange
        var compilation = CreateCompilation(@"
            public class TestRequest : IRequest<string> { }
            public class TestHandler
            {
                [Handle] public string H1(TestRequest r, CancellationToken ct) => """";
            }
        ");

        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("H1").OfType<IMethodSymbol>().First();
        var methodDecl = GetMethodDeclaration(compilation, "H1");
        var attribute = method.GetAttributes().First();

        var registry = new HandlerRegistry();
        registry.AddHandler(method, attribute, methodDecl);

        // Manually set name since attribute parsing doesn't work in test
        registry.Handlers[0].Name = "main";

        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
        var context = new CompilationAnalysisContext(compilation, options, d => _mockReporter.Object.ReportDiagnostic(d), _ => true, default);

        // Act
        DuplicateHandlerValidator.ValidateDuplicateHandlers(context, registry);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_102" && d.GetMessage().Contains("might conflict with common naming patterns"))), Times.Once);
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText($@"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Relay.Core;

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

    private static INamedTypeSymbol GetTypeSymbol(Compilation compilation, string typeName)
    {
        // Handle C# builtin aliases
        return typeName switch
        {
            "string" => (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_String),
            "int" => (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32),
            "bool" => (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Boolean),
            "void" => (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Void),
            _ => compilation.GetTypeByMetadataName(typeName) ??
                           compilation.GetSymbolsWithName(typeName).OfType<INamedTypeSymbol>().First(),
        };
    }

    private static MethodDeclarationSyntax GetMethodDeclaration(Compilation compilation, string methodName)
    {
        return compilation.SyntaxTrees.First()
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == methodName);
    }
}