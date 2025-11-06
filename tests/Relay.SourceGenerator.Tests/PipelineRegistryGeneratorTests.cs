using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

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

    [Fact]
    public void GeneratorName_ReturnsExpectedValue()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new PipelineRegistryGenerator(context);

        // Act
        var result = generator.GeneratorName;

        // Assert
        Assert.Equal("Pipeline Registry Generator", result);
    }

    [Fact]
    public void OutputFileName_ReturnsExpectedValue()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new PipelineRegistryGenerator(context);

        // Act
        var result = generator.OutputFileName;

        // Assert
        Assert.Equal("PipelineRegistry", result);
    }

    [Fact]
    public void Priority_ReturnsExpectedValue()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new PipelineRegistryGenerator(context);

        // Act
        var result = generator.Priority;

        // Assert
        Assert.Equal(50, result);
    }

    [Fact]
    public void CanGenerate_WithPipelineHandlers_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new PipelineRegistryGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();
        discoveryResult.Handlers.Add(new HandlerInfo
        {
            Attributes = [new() { Type = RelayAttributeType.Pipeline }]
        });

        // Act
        var result = generator.CanGenerate(discoveryResult);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanGenerate_WithoutPipelineHandlers_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new PipelineRegistryGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();
        discoveryResult.Handlers.Add(new HandlerInfo
        {
            Attributes = [new() { Type = RelayAttributeType.Handle }] // Not Pipeline
        });

        // Act
        var result = generator.CanGenerate(discoveryResult);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Generate_WithPipelineHandlers_GeneratesValidCode()
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
        var discoveryResult = CreateDiscoveryResultWithPipelineHandler(compilation, sourceCode);
        var options = new GenerationOptions();

        // Act
        var result = generator.Generate(discoveryResult, options);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("namespace Relay.Generated", result);
        Assert.Contains("internal static class PipelineRegistry", result);
        Assert.Contains("PipelineMetadata", result);
        Assert.Contains("GetPipelineBehaviors", result);
        Assert.Contains("GetStreamPipelineBehaviors", result);
        Assert.Contains("// Generator: Pipeline Registry Generator", result);
    }

    [Fact]
    public void GeneratePipelineRegistry_WithEmptyAttributeData_UsesFallbackLogic()
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

        // Create discovery result with handler that has empty AttributeData
        var discoveryResult = new HandlerDiscoveryResult();

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
                HandlerInfo handlerInfo = new()
                {
                    Method = method,
                    MethodSymbol = methodSymbol,
                    Attributes =
                    [
                        new() {
                            Type = RelayAttributeType.Pipeline,
                            AttributeData = CreateMockEmptyAttributeData() // Empty to trigger fallback
                        }
                    ]
                };
                discoveryResult.Handlers.Add(handlerInfo);
            }
        }

        // Act
        var result = generator.GeneratePipelineRegistry(discoveryResult);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("internal static class PipelineRegistry", result);
        // The fallback logic should be triggered, but the exact values depend on the mock
    }

    [Fact]
    public void GeneratePipelineRegistry_WithNullAttributeData_UsesSyntaxParsingFallback()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestPipelineHandler
{
    [Pipeline(Order = 5, Scope = PipelineScope.All)]
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

        // Create discovery result with handler that has null AttributeData to trigger syntax parsing
        var discoveryResult = new HandlerDiscoveryResult();

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
                HandlerInfo handlerInfo = new()
                {
                    Method = method,
                    MethodSymbol = methodSymbol,
                    Attributes =
                    [
                        new() {
                            Type = RelayAttributeType.Pipeline,
                            AttributeData = null // Null to trigger syntax parsing fallback
                        }
                    ]
                };
                discoveryResult.Handlers.Add(handlerInfo);
            }
        }

        // Act
        var result = generator.GeneratePipelineRegistry(discoveryResult);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("internal static class PipelineRegistry", result);
        Assert.Contains("Order = 5", result);
        Assert.Contains("Scope = PipelineScope.All", result);
    }

    [Fact]
    public void GeneratePipelineRegistry_WithInvalidSymbolAttributes_UsesSyntaxParsingFallback()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestPipelineHandler
{
    [Pipeline(Order = 3, Scope = PipelineScope.Streams)]
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

        // Create discovery result with handler that has AttributeData with empty values
        // and mock symbol attributes that don't provide values
        var discoveryResult = new HandlerDiscoveryResult();

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
                HandlerInfo handlerInfo = new()
                {
                    Method = method,
                    MethodSymbol = methodSymbol,
                    Attributes =
                    [
                        new() {
                            Type = RelayAttributeType.Pipeline,
                            AttributeData = CreateMockEmptyAttributeData() // Empty to trigger fallback
                        }
                    ]
                };
                discoveryResult.Handlers.Add(handlerInfo);
            }
        }

        // Act
        var result = generator.GeneratePipelineRegistry(discoveryResult);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("internal static class PipelineRegistry", result);
        Assert.Contains("Order = 3", result);
        Assert.Contains("Scope = PipelineScope.Streams", result);
    }

    private static CSharpCompilation CreateTestCompilation(string sourceCode)
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
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private HandlerDiscoveryResult CreateDiscoveryResultWithPipelineHandler(Compilation compilation, string _)
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
                HandlerInfo handlerInfo = new()
                {
                    Method = method,
                    MethodSymbol = methodSymbol,
                    Attributes =
                    [
                        new() {
                            Type = RelayAttributeType.Pipeline,
                            AttributeData = CreateMockAttributeData()
                        }
                    ]
                };
                result.Handlers.Add(handlerInfo);
            }
        }

        return result;
    }

    private static AttributeData CreateMockAttributeData()
    {
        // Create a mock AttributeData for testing
        // This is a simplified version for testing purposes
        return new MockAttributeData();
    }

    private static AttributeData CreateMockEmptyAttributeData()
    {
        // Create a mock AttributeData with empty named arguments to test fallback logic
        return new MockEmptyAttributeData();
    }

    private class MockAttributeData : AttributeData
    {
        protected override INamedTypeSymbol? CommonAttributeClass => null;
        protected override IMethodSymbol? CommonAttributeConstructor => null;
        protected override SyntaxReference? CommonApplicationSyntaxReference => null;

        protected override System.Collections.Immutable.ImmutableArray<TypedConstant> CommonConstructorArguments =>
            [];

        protected override System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<string, TypedConstant>> CommonNamedArguments =>
            [
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Order", new TypedConstant()),
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Scope", new TypedConstant())
            ,
            ];
    }

    private class MockEmptyAttributeData : AttributeData
    {
        protected override INamedTypeSymbol? CommonAttributeClass => null;
        protected override IMethodSymbol? CommonAttributeConstructor => null;
        protected override SyntaxReference? CommonApplicationSyntaxReference => null;

        protected override System.Collections.Immutable.ImmutableArray<TypedConstant> CommonConstructorArguments =>
            [];

        protected override System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<string, TypedConstant>> CommonNamedArguments =>
            [
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Order", new TypedConstant()),
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Scope", new TypedConstant())
            ]; // Has arguments but with null values to trigger fallback
    }
}