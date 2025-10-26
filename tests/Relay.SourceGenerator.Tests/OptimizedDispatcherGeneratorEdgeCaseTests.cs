using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Relay.SourceGenerator;
using Xunit;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

public class OptimizedDispatcherGeneratorEdgeCaseTests
{
    private RelayCompilationContext CreateTestContext()
    {
        var code = @"
namespace Test
{
    public class TestRequest : Relay.Core.IRequest<string> { }
    public class TestHandler
    {
        [Relay.Core.Attributes.Handle]
        public async System.Threading.Tasks.ValueTask<string> HandleAsync(TestRequest request, System.Threading.CancellationToken cancellationToken)
        {
            return ""test"";
        }
    }
}";

        var compilation = CreateCompilation(code);
        return new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static HandlerInfo CreateMockHandler(string requestType, string responseType, string handlerType = "TestHandler", string methodName = "HandleAsync", bool isStatic = false, string? handlerName = null, int priority = 0)
    {
        var compilation = CreateCompilation($@"
namespace Test {{
    public class {requestType} : Relay.Core.IRequest<{responseType}> {{ }}
    public class {handlerType} {{
        {(isStatic ? "public static" : "public")} async System.Threading.Tasks.ValueTask<{responseType}> {methodName}({requestType} request, System.Threading.CancellationToken cancellationToken) => default;
    }}
}}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{requestType}");
        var responseTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{responseType}");
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{handlerType}");

        var methodSymbol = handlerTypeSymbol?.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();

        HandlerInfo handler = new()
        {
            MethodSymbol = methodSymbol,
            HandlerTypeSymbol = handlerTypeSymbol,
            RequestTypeSymbol = requestTypeSymbol,
            ResponseTypeSymbol = responseTypeSymbol,
            Attributes =
            [
                new() {
                    Type = RelayAttributeType.Handle,
                    AttributeData = CreateMockAttributeData(handlerName, priority)
                }
            ]
        };

        return handler;
    }

    private static AttributeData CreateMockAttributeData(string? _, int __)
    {
        // For testing purposes, we'll create a mock attribute data
        // In a real scenario, this would be created from actual syntax
        return null!;
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithComplexTypeNames_ShouldSanitizeCorrectly()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create a handler with a complex type name
        var compilation = CreateCompilation(@"
namespace Test {
    public class ComplexRequest<T> : Relay.Core.IRequest<string> where T : class { }
    public class ComplexHandler {
        public async System.Threading.Tasks.ValueTask<string> HandleAsync(ComplexRequest<object> request, System.Threading.CancellationToken cancellationToken) => default;
    }
}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.ComplexRequest`1")?.Construct(semanticModel.Compilation.GetSpecialType(SpecialType.System_Object));
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.ComplexHandler");
        var methodSymbol = handlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();

        HandlerInfo handler = new()
        {
            MethodSymbol = methodSymbol,
            HandlerTypeSymbol = handlerTypeSymbol,
            RequestTypeSymbol = requestTypeSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };

        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("Dispatch_Test_ComplexRequest_object_", source); // Sanitized method name
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithGenericTypeParameters_ShouldHandleCorrectly()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler = CreateMockHandler("GenericRequest", "List<string>", "GenericHandler", "HandleAsync", false);
        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("ValueTask<List<string>> Dispatch_Test_GenericRequest(", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_ShouldIncludeErrorHandlingForUnknownTypes()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler = CreateMockHandler("TestRequest", "string", "TestHandler", "HandleAsync", false);
        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("No handler found for request type", source);
        // Note: "No handler found with name" only appears with multiple handlers
        // "No streaming handler found for request type" only appears with streaming handlers
    }

    [Fact]
    public void GenerateOptimizedDispatcher_ShouldUseAggressiveInlining()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler = CreateMockHandler("TestRequest", "string", "TestHandler", "HandleAsync", false);
        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        var inlineCount = source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
            .Count(line => line.Contains("MethodImpl(MethodImplOptions.AggressiveInlining)"));

        Assert.True(inlineCount >= 2); // Should have at least 2 methods with inlining
    }
}