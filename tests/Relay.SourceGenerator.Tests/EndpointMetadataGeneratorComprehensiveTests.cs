using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

public class EndpointMetadataGeneratorComprehensiveTests
{
    [Fact]
    public void EndpointMetadataGenerator_Generate_UsesProtectedMethods()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(Route = ""/api/test"", HttpMethod = ""GET"")]
    public string HandleTest(TestRequest request)
    {
        return ""test"";
    }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var diagnosticReporter = new TestDiagnosticReporter();

        var syntaxTrees = compilation.SyntaxTrees.ToList();
        var candidateMethods = syntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Count > 0)
            .ToList();

        var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        var options = new GenerationOptions();

        // Act - Use the public Generate method which internally calls AppendUsings and GenerateContent
        var result = generator.Generate(discoveryResult, options);

        // Assert - This tests that the protected methods are used correctly through the public interface
        Assert.Contains("using System;", result);
        Assert.Contains("using System.Collections.Generic;", result);
        Assert.Contains("using Relay.Core;", result);
        Assert.Contains("namespace Relay.Generated", result);
        Assert.Contains("internal static class GeneratedEndpointMetadata", result);
        Assert.Contains("public static void RegisterEndpoints()", result);
        Assert.Contains("EndpointMetadataRegistry.RegisterEndpoint", result);
        // The BaseCodeGenerator implementation uses different formatting, but should contain the relevant data
        Assert.NotEmpty(result); // Should generate content
    }

    [Fact]
    public void GetAttributeValue_WithNamedArguments_ExtractsValues()
    {
        // Test with named arguments which is how ExposeAsEndpoint is typically used
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(Route = ""custom/route"", HttpMethod = ""PUT"")] // Using named arguments
    public string HandleTest(TestRequest request) => ""test"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var diagnosticReporter = new TestDiagnosticReporter();

        var syntaxTrees = compilation.SyntaxTrees.ToList();
        var candidateMethods = syntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Count > 0)
            .ToList();

        var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act
        var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

        // Assert - the method should handle named arguments properly
        Assert.NotEmpty(result);
        // The actual format might be different - check for the route value in some form
        Assert.Contains("custom/route", result);
        Assert.Contains("PUT", result);
    }

    [Fact]
    public void ToKebabCase_ConvertsVariousInputs()
    {
        // Arrange - Use reflection to test the private ToKebabCase method
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleTest(TestRequest request) => ""test"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var diagnosticReporter = new TestDiagnosticReporter();
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Use reflection to access the private ToKebabCase method
        var toKebabCaseMethod = typeof(EndpointMetadataGenerator).GetMethod("ToKebabCase", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act & Assert
        Assert.Equal("simple", toKebabCaseMethod.Invoke(generator, new object[] { "Simple" }));
        Assert.Equal("hello-world", toKebabCaseMethod.Invoke(generator, new object[] { "HelloWorld" }));
        Assert.Equal("convert-camel-case", toKebabCaseMethod.Invoke(generator, new object[] { "ConvertCamelCase" }));
        Assert.Equal("", toKebabCaseMethod.Invoke(generator, new object[] { "" }));
        Assert.Null(toKebabCaseMethod.Invoke(generator, new object[] { null }));  // Method returns input if null/whitespace
        Assert.Equal("a", toKebabCaseMethod.Invoke(generator, new object[] { "A" }));
        Assert.Equal("multiple-upper-case", toKebabCaseMethod.Invoke(generator, new object[] { "MultipleUpperCase" }));
    }

    [Fact]
    public void GetResponseType_HandlesVariousReturnTypes()
    {
        // Arrange - We'll test the GetResponseType method through the actual generator behavior
        var source = @"
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public System.Threading.Tasks.Task<System.Collections.Generic.List<string>> HandleTest(TestRequest request) 
    { 
        return System.Threading.Tasks.Task.FromResult(new System.Collections.Generic.List<string>());
    }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var diagnosticReporter = new TestDiagnosticReporter();

        var syntaxTrees = compilation.SyntaxTrees.ToList();
        var candidateMethods = syntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Count > 0)
            .ToList();

        var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act
        var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

        // Assert - This tests the GetResponseType method's ability to handle complex generic return types
        Assert.NotEmpty(result);
        Assert.Contains("ResponseType = typeof(List<string>)", result);
    }

    [Fact]
    public void FormatType_HandlesVariousTypeFormats()
    {
        // This method is private, so we'll test it indirectly through the generated output
        var source = @"
using System.Collections.Generic;
using Relay.Core;

public class ComplexRequest : IRequest<Dictionary<string, List<int>>> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public Dictionary<string, List<int>> HandleComplex(ComplexRequest request) => new Dictionary<string, List<int>>();
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var diagnosticReporter = new TestDiagnosticReporter();

        var syntaxTrees = compilation.SyntaxTrees.ToList();
        var candidateMethods = syntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Count > 0)
            .ToList();

        var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act
        var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

        // Assert - This tests the FormatType method's ability to format complex types
        Assert.NotEmpty(result);
        Assert.Contains("RequestType = typeof(ComplexRequest)", result);
        Assert.Contains("ResponseType = typeof(Dictionary<string, List<int>>)", result);
    }

    [Fact]
    public void GenerateJsonSchemas_HandlesVoidResponseType()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public void HandleTest(TestRequest request) { }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var diagnosticReporter = new TestDiagnosticReporter();

        var syntaxTrees = compilation.SyntaxTrees.ToList();
        var candidateMethods = syntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Count > 0)
            .ToList();

        var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act
        var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

        // Assert - Should generate request schema but not response schema for void return
        Assert.NotEmpty(result);
        Assert.Contains("RequestSchema =", result);
        Assert.DoesNotContain("ResponseSchema =", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithInterfaceParameter_GeneratesCorrectly()
    {
        // Arrange
        var source = @"
using Relay.Core;

public interface ITestRequest : IRequest<string> { }

public class TestRequestImpl : ITestRequest { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleTest(ITestRequest request) => ""test"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var diagnosticReporter = new TestDiagnosticReporter();

        var syntaxTrees = compilation.SyntaxTrees.ToList();
        var candidateMethods = syntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Count > 0)
            .ToList();

        var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act
        var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("RequestType = typeof(ITestRequest)", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithEnumParameter_GeneratesCorrectly()
    {
        // Arrange
        var source = @"
using Relay.Core;

public enum TestEnum { Value1, Value2 }

public class TestRequest : IRequest<string> 
{ 
    public TestEnum EnumValue { get; set; }
}

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleTest(TestRequest request) => ""test"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var diagnosticReporter = new TestDiagnosticReporter();

        var syntaxTrees = compilation.SyntaxTrees.ToList();
        var candidateMethods = syntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Count > 0)
            .ToList();

        var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act
        var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("RequestType = typeof(TestRequest)", result);
    }
    
    [Fact]
    public void GetRequestType_HandlesMissingParameters()
    {
        // Arrange - create a method without parameters to test fallback
        var source = @"
using Relay.Core;

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleTest() => ""test"";  // No parameter
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var context = new RelayCompilationContext(compilation, default);
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var diagnosticReporter = new TestDiagnosticReporter();

        var syntaxTrees = compilation.SyntaxTrees.ToList();
        var candidateMethods = syntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Count > 0)
            .ToList();

        var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act
        var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

        // Assert - should handle gracefully, though this case might not pass validation in HandlerDiscovery
        Assert.Empty(result); // Since the method signature would be invalid for a handler, discovery would not include it
    }
}