using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class PipelineRegistryGeneratorTests
    {
        [Fact]
        public void GeneratePipelineRegistry_WithNoPipelineHandlers_ReturnsEmptyString()
        {
            // Arrange
            var compilation = CreateTestCompilation("");
            var context = new RelayCompilationContext(compilation, default);
            var generator = new PipelineRegistryGenerator(context);
            var discoveryResult = new HandlerDiscoveryResult();

            // Act
            var result = generator.GeneratePipelineRegistry(discoveryResult);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GeneratePipelineRegistry_WithPipelineHandlers_GeneratesValidCode()
        {
            // Arrange
            var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestPipelineHandler
{
    [Pipeline(Order = 1, Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> HandlePipeline<TRequest, TResponse>(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        return await next();
    }
}";

            var compilation = CreateTestCompilation(sourceCode);
            var context = new RelayCompilationContext(compilation, default);
            var generator = new PipelineRegistryGenerator(context);
            
            // Create discovery result with pipeline handler
            var discoveryResult = CreateDiscoveryResultWithPipelineHandler(compilation, sourceCode);

            // Act
            var result = generator.GeneratePipelineRegistry(discoveryResult);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("namespace Relay.Generated", result);
            Assert.Contains("internal static class PipelineRegistry", result);
            Assert.Contains("PipelineMetadata", result);
            Assert.Contains("GetPipelineBehaviors", result);
            Assert.Contains("GetStreamPipelineBehaviors", result);
        }

        [Fact]
        public void GeneratePipelineRegistry_WithStreamPipelineHandler_GeneratesStreamSupport()
        {
            // Arrange
            var sourceCode = @"
using Relay.Core;
using System.Collections.Generic;
using System.Threading;

public class TestStreamPipelineHandler
{
    [Pipeline(Order = 2, Scope = PipelineScope.Streams)]
    public async IAsyncEnumerable<TResponse> HandleStreamPipeline<TRequest, TResponse>(
        TRequest request, 
        StreamHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        await foreach (var item in next())
        {
            yield return item;
        }
    }
}";

            var compilation = CreateTestCompilation(sourceCode);
            var context = new RelayCompilationContext(compilation, default);
            var generator = new PipelineRegistryGenerator(context);
            
            // Create discovery result with stream pipeline handler
            var discoveryResult = CreateDiscoveryResultWithPipelineHandler(compilation, sourceCode);

            // Act
            var result = generator.GeneratePipelineRegistry(discoveryResult);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("IsStreamPipeline = true", result);
            Assert.Contains("GeneratedStreamPipelineBehavior", result);
        }

        [Fact]
        public void GeneratePipelineRegistry_WithMultiplePipelineHandlers_OrdersByPriority()
        {
            // Arrange
            var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestPipelineHandlers
{
    [Pipeline(Order = 10)]
    public async ValueTask<TResponse> LowPriorityPipeline<TRequest, TResponse>(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        return await next();
    }

    [Pipeline(Order = 1)]
    public async ValueTask<TResponse> HighPriorityPipeline<TRequest, TResponse>(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        return await next();
    }
}";

            var compilation = CreateTestCompilation(sourceCode);
            var context = new RelayCompilationContext(compilation, default);
            var generator = new PipelineRegistryGenerator(context);
            
            // Create discovery result with multiple pipeline handlers
            var discoveryResult = CreateDiscoveryResultWithPipelineHandler(compilation, sourceCode);

            // Act
            var result = generator.GeneratePipelineRegistry(discoveryResult);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Order = 1", result);
            Assert.Contains("Order = 10", result);
            Assert.Contains("OrderBy(m => m.Order)", result);
        }

        private Compilation CreateTestCompilation(string sourceCode)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private HandlerDiscoveryResult CreateDiscoveryResultWithPipelineHandler(Compilation compilation, string sourceCode)
        {
            var result = new HandlerDiscoveryResult();
            
            // Parse the source and find methods with Pipeline attributes
            var syntaxTree = compilation.SyntaxTrees.First();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();
            
            var methods = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists.Any())
                .ToList();

            foreach (var method in methods)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol != null)
                {
                    var handlerInfo = new HandlerInfo
                    {
                        Method = method,
                        MethodSymbol = methodSymbol,
                        Attributes = new System.Collections.Generic.List<RelayAttributeInfo>
                        {
                            new RelayAttributeInfo
                            {
                                Type = RelayAttributeType.Pipeline,
                                AttributeData = CreateMockAttributeData()
                            }
                        }
                    };
                    result.Handlers.Add(handlerInfo);
                }
            }

            return result;
        }

        private AttributeData CreateMockAttributeData()
        {
            // Create a mock AttributeData for testing
            // This is a simplified version for testing purposes
            return new MockAttributeData();
        }

        private class MockAttributeData : AttributeData
        {
            public override INamedTypeSymbol? AttributeClass => null;
            public override IMethodSymbol? AttributeConstructor => null;
            public override SyntaxReference? ApplicationSyntaxReference => null;

            protected override System.Collections.Immutable.ImmutableArray<TypedConstant> CommonConstructorArguments =>
                System.Collections.Immutable.ImmutableArray<TypedConstant>.Empty;

            protected override System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<string, TypedConstant>> CommonNamedArguments =>
                System.Collections.Immutable.ImmutableArray.Create(
                    new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Order", new TypedConstant()),
                    new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Scope", new TypedConstant())
                );
        }
    }
}