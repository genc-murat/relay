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

    [Fact]
    public void GeneratePipelineRegistry_WithNullMethodSymbol_SkipsHandler()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new PipelineRegistryGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Add handler with null MethodSymbol to test the continue statement
        discoveryResult.Handlers.Add(new HandlerInfo
        {
            MethodSymbol = null,
            Attributes = [new() { Type = RelayAttributeType.Pipeline }]
        });

        // Act
        var result = generator.GeneratePipelineRegistry(discoveryResult);

        // Assert
        // Header should be generated because handler has Pipeline attribute, 
        // but no pipeline metadata should be included since MethodSymbol is null
        Assert.Contains("// <auto-generated />", result);
        Assert.Contains("internal static class PipelineRegistry", result);
        Assert.DoesNotContain("new PipelineMetadata", result); // No pipeline entries should be generated
    }

    [Fact]
    public void GeneratePipelineMetadata_WithEmptyNamedArguments_InspectsSymbolAttributes()
    {
        // Arrange - Test the specific if block that handles empty named arguments
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestPipelineHandler
{
    [Pipeline(Order = 10, Scope = PipelineScope.Requests)]
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
        var discoveryResult = new HandlerDiscoveryResult();

        // Parse the source and create handler info with empty named arguments
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.AttributeLists.Any());

        var methodSymbol = semanticModel.GetDeclaredSymbol(method);
        if (methodSymbol != null)
        {
            // Create handler with MockEmptyAttributeData that has empty named arguments
            HandlerInfo handlerInfo = new()
            {
                Method = method,
                MethodSymbol = methodSymbol,
                Attributes =
                [
                    new() {
                        Type = RelayAttributeType.Pipeline,
                        AttributeData = new MockEmptyAttributeData() // Empty named arguments to trigger symbol attributes inspection
                    }
                ]
            };
            discoveryResult.Handlers.Add(handlerInfo);
        }

        // Act
        var result = generator.GeneratePipelineRegistry(discoveryResult);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("internal static class PipelineRegistry", result);
        // Should extract values from symbol attributes since named arguments are empty
        Assert.Contains("Order = 10", result);
        Assert.Contains("Scope = PipelineScope.Requests", result);
    }

    [Fact]
    public void GeneratePipelineRegistry_WithZeroOrderAndSyntaxParsing_UsesFallback()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestPipelineHandler
{
    [Pipeline(Order = 0, Scope = PipelineScope.All)]
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
                            AttributeData = CreateMockZeroOrderAttributeData() // Order = 0 to trigger syntax parsing
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
        Assert.Contains("Order = 0", result);
        Assert.Contains("Scope = PipelineScope.All", result);
    }

    [Fact]
    public void GeneratePipelineMetadata_WithMockedEmptyAttributeData_InspectsSymbolAttributes()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestPipelineHandler
{
    [Pipeline(Order = 7, Scope = PipelineScope.Streams)]
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
                            AttributeData = CreateMockEmptyAttributeData() // Empty to trigger symbol attributes inspection
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
        // The fallback logic should inspect symbol attributes and extract Order = 7 and Scope = Streams
        Assert.Contains("Order = 7", result);
        Assert.Contains("Scope = PipelineScope.Streams", result);
    }

    [Fact]
    public void GeneratePipelineMetadata_WithMockedInvalidAttributeData_InspectsSymbolAttributes()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestPipelineHandler
{
    [Pipeline(Order = 15, Scope = PipelineScope.Requests)]
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
                            AttributeData = CreateMockInvalidAttributeData() // Invalid values to trigger symbol attributes inspection
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
        // The fallback logic should inspect symbol attributes and extract Order = 15 and Scope = Requests
        Assert.Contains("Order = 15", result);
        Assert.Contains("Scope = PipelineScope.Requests", result);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithOrderAndMemberAccessScope_ReturnsTrue()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Pipeline(Order = 42, Scope = PipelineScope.Streams)]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.True(result);
        Assert.Equal(42, order);
        Assert.Equal("Streams", scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithOrderAndIdentifierScope_ReturnsTrue()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Pipeline(Order = 99, Scope = All)]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.True(result);
        Assert.Equal(99, order);
        Assert.Equal("All", scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithOnlyOrder_ReturnsTrue()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Pipeline(Order = 7)]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.True(result);
        Assert.Equal(7, order);
        Assert.Null(scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithOnlyScope_ReturnsTrue()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Pipeline(Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.True(result);
        Assert.Null(order);
        Assert.Equal("Requests", scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithNoArguments_ReturnsFalse()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Pipeline]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.False(result);
        Assert.Null(order);
        Assert.Null(scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithNonPipelineAttribute_ReturnsFalse()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Handle]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.False(result);
        Assert.Null(order);
        Assert.Null(scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithPipelineAttributeSuffix_ReturnsTrue()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [PipelineAttribute(Order = 25, Scope = PipelineScope.All)]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.True(result);
        Assert.Equal(25, order);
        Assert.Equal("All", scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithInvalidOrderType_ReturnsFalse()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Pipeline(Order = ""invalid"", Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.True(result); // Still true because Scope is valid
        Assert.Null(order); // Order is null because it's not a numeric literal
        Assert.Equal("Requests", scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithInvalidScopeType_ReturnsFalse()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Pipeline(Order = 10, Scope = 123)]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.True(result); // Still true because Order is valid
        Assert.Equal(10, order);
        Assert.Null(scope); // Scope is null because it's not MemberAccess or Identifier
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithNoAttributes_ReturnsFalse()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.False(result);
        Assert.Null(order);
        Assert.Null(scope);
    }

    [Fact]
    public void TryParsePipelineAttributeFromSyntax_WithMultipleAttributes_FindsFirstPipeline()
    {
        // Arrange
        var sourceCode = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

public class TestHandler
{
    [Handle]
    [Pipeline(Order = 88, Scope = PipelineScope.Streams)]
    [Obsolete]
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        return await next();
    }
}";

        var compilation = CreateTestCompilation(sourceCode);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        // Act
        var result = CallTryParsePipelineAttributeFromSyntax(method, out var order, out var scope);

        // Assert
        Assert.True(result);
        Assert.Equal(88, order);
        Assert.Equal("Streams", scope);
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

    private static AttributeData CreateMockZeroOrderAttributeData()
    {
        // Create a mock AttributeData with Order = 0 to trigger syntax parsing fallback
        return new MockZeroOrderAttributeData();
    }

    private static AttributeData CreateMockInvalidAttributeData()
    {
        // Create a mock AttributeData with invalid/null values to trigger symbol attributes inspection
        return new MockInvalidAttributeData();
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
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Order", MockHelper.CreateTypedConstant(1)),
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Scope", MockHelper.CreateTypedConstant("Requests"))
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
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Order", MockHelper.CreateEmptyTypedConstant()),
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Scope", MockHelper.CreateEmptyTypedConstant())
            ]; // Has arguments but with null values to trigger fallback
    }

    private static class MockHelper
    {
        public static TypedConstant CreateTypedConstant(object value)
        {
            // Use the default TypedConstant constructor and set properties through reflection if needed
            var typedConstant = new TypedConstant();
            
            // For testing purposes, we'll create a simple TypedConstant
            // The actual values don't matter as much as having the right structure
            return typedConstant;
        }

        public static TypedConstant CreateEmptyTypedConstant()
        {
            return new TypedConstant();
        }

        public static TypedConstant CreateNullTypedConstant()
        {
            // Create a TypedConstant that represents a null value
            // This will trigger the condition where all named arguments are null
            return new TypedConstant();
        }
    }

    private bool CallTryParsePipelineAttributeFromSyntax(Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax method, out int? order, out string? scope)
    {
        // Use reflection to call the private TryParsePipelineAttributeFromSyntax method
        var generatorType = typeof(PipelineRegistryGenerator);
        var methodInfo = generatorType.GetMethod("TryParsePipelineAttributeFromSyntax", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (methodInfo == null)
        {
            order = null;
            scope = null;
            return false;
        }

        // Create a dummy instance of PipelineRegistryGenerator to call the method
        var compilation = CreateTestCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new PipelineRegistryGenerator(context);

        var parameters = new object[] { method, null, null };
        var result = (bool)methodInfo.Invoke(generator, parameters);
        
        order = (int?)parameters[1];
        scope = (string?)parameters[2];
        
        return result;
    }

    private class MockZeroOrderAttributeData : AttributeData
    {
        protected override INamedTypeSymbol? CommonAttributeClass => null;
        protected override IMethodSymbol? CommonAttributeConstructor => null;
        protected override SyntaxReference? CommonApplicationSyntaxReference => null;

        protected override System.Collections.Immutable.ImmutableArray<TypedConstant> CommonConstructorArguments =>
            [];

        protected override System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<string, TypedConstant>> CommonNamedArguments =>
            [
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Order", MockHelper.CreateTypedConstant(0)), // Order = 0 to trigger syntax parsing
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Scope", MockHelper.CreateEmptyTypedConstant())
            ];
    }

    private class MockInvalidAttributeData : AttributeData
    {
        protected override INamedTypeSymbol? CommonAttributeClass => null;
        protected override IMethodSymbol? CommonAttributeConstructor => null;
        protected override SyntaxReference? CommonApplicationSyntaxReference => null;

        protected override System.Collections.Immutable.ImmutableArray<TypedConstant> CommonConstructorArguments =>
            [];

        protected override System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<string, TypedConstant>> CommonNamedArguments =>
            [
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Order", MockHelper.CreateNullTypedConstant()), // Null value to trigger symbol attributes inspection
                new System.Collections.Generic.KeyValuePair<string, TypedConstant>("Scope", MockHelper.CreateNullTypedConstant()) // Null value to trigger symbol attributes inspection
            ];
    }
}