using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Relay.SourceGenerator.Tests;

public class HandlerDiscoveryEngineMethodSymbolNullTest
{
    [Fact]
    public void AnalyzeHandlerMethodWithSymbol_Should_Handle_Null_MethodSymbol()
    {
        // Arrange: Use the test helper method to directly test the null method symbol branch
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);

        // Set up the RelayCompilationContext and HandlerDiscoveryEngine
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        
        // Get the method declaration syntax - find the syntax tree containing our test code
        var syntaxTree = compilation.SyntaxTrees.First(st => st.ToString().Contains("TestProject"));
        var root = syntaxTree.GetRoot();
        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == "HandleAsync");

        // Create a diagnostic reporter to capture diagnostics
        var diagnosticReporter = new TestDiagnosticReporter();
        
        // Act: Call the public test helper method with a null method symbol
        // This directly tests the branch where methodSymbol == null
        var result = discoveryEngine.AnalyzeHandlerMethodWithSymbol(methodDeclaration, null, diagnosticReporter);
        
        // Assert: Check that it returns null and reports a diagnostic
        Assert.Null(result);
        Assert.Contains(diagnosticReporter.Diagnostics, 
            d => d.Id == "RELAY_GEN_001" && 
                 d.GetMessage().Contains("Could not get symbol for method"));
    }

    private static Compilation CreateTestCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest<out TResponse> { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandleAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}
");

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { relayCoreStubs, syntaxTree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

// Simple test diagnostic reporter that captures diagnostics
internal class TestDiagnosticReporter : IDiagnosticReporter
{
    public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();
    
    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
    }
}