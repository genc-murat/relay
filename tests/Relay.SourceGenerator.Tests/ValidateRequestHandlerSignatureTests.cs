using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Relay.SourceGenerator.Tests;

public class ValidateRequestHandlerSignatureTests
{
    [Fact]
    public void ValidateRequestHandlerSignature_InvalidReturnTypeAndNotEndpointHandler_Should_ReportDiagnostic()
    {
        // Arrange: Create a method that is a request handler (has [Handle]) but:
        // 1. Has invalid return type (void) - making !IsValidReturnType(method.ReturnType) = true
        // 2. Is NOT an endpoint handler (doesn't have [ExposeAsEndpoint]) - making !IsEndpointHandler(method) = true
        // So the condition: (!IsValidReturnType(method.ReturnType) && !IsEndpointHandler(method)) = true
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
        public void Handle(TestRequest request)
        {
            // This method has invalid return type (void) for a request handler
            // and is not marked as an endpoint handler, so it should fail validation
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(st => st.ToString().Contains("TestProject"));
        var root = syntaxTree.GetRoot();
        
        // Get the method declaration
        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == "Handle");

        // Create a RelayCompilationContext and HandlerDiscoveryEngine
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        
        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);

        // Create a diagnostic reporter to capture diagnostics
        var diagnosticReporter = new TestDiagnosticReporter();

        // Act: Call a private method using reflection to test the specific validation
        // Since ValidateRequestHandlerSignature is private, we'll use reflection to test it
        var validateMethod = typeof(HandlerDiscoveryEngine)
            .GetMethod("ValidateRequestHandlerSignature", 
                      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var location = methodDeclaration.GetLocation();
        var result = (bool)validateMethod?.Invoke(discoveryEngine, new object[] { methodSymbol, location, diagnosticReporter });

        // Assert: The validation should fail because void return type is invalid for request handlers
        // that are not endpoint handlers
        Assert.False(result); // Should return false due to invalid return type
        Assert.Contains(diagnosticReporter.Diagnostics, 
            d => d.Id == "RELAY_GEN_002" && // InvalidHandlerSignature diagnostic ID
                 d.GetMessage().Contains("Request handlers must return"));
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
    public interface IRequest { }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandleAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NotificationAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PipelineAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ExposeAsEndpointAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}");

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