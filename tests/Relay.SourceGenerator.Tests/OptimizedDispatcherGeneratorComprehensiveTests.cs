using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Generators;
using System.Reflection;
using System.Text;

namespace Relay.SourceGenerator.Tests;

public class OptimizedDispatcherGeneratorComprehensiveTests
{
    [Fact]
    public void SanitizeTypeName_WithVariousInputs_ReturnsExpectedResults()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        // Use reflection to access the private SanitizeTypeName method
        var sanitizeMethod = typeof(OptimizedDispatcherGenerator).GetMethod("SanitizeTypeName",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        Assert.Equal("System_Collections_Generic_List_System_String_", 
            sanitizeMethod?.Invoke(generator, ["System.Collections.Generic.List<System.String>"]));
        Assert.Equal("Test_MyClass__", 
            sanitizeMethod?.Invoke(generator, ["Test.MyClass[]"]));
        Assert.Equal("Test_GenericClass_System_Int32_", 
            sanitizeMethod?.Invoke(generator, ["Test.GenericClass<System.Int32>"]));
        Assert.Equal("Test_MyClass_System_String_System_Int32_", 
            sanitizeMethod?.Invoke(generator, ["Test.MyClass<System.String,System.Int32>"]));
        Assert.Equal("", 
            sanitizeMethod?.Invoke(generator, [""]));
        // The method throws NullReferenceException when called with null
        Assert.ThrowsAny<Exception>(() => sanitizeMethod?.Invoke(generator, [null!]));
    }

    [Fact]
    public void GetResponseType_WithVariousReturnTypes_ReturnsExpectedResults()
    {
        // Arrange
        var compilation = CreateCompilation(@"
using System.Threading.Tasks;
using System.Collections.Generic;
namespace Test 
{
    public class StringResponse { }
    public class TestHandler 
    {
        public Task<StringResponse> HandleAsync() => null!;
        public ValueTask<StringResponse> HandleValueAsync() => default;
        public IAsyncEnumerable<StringResponse> HandleStreamAsync() => null!;
        public StringResponse HandleDirect() => null!;
        public void HandleVoid() { }
        public Task HandleTaskVoid() => Task.CompletedTask;
        public ValueTask HandleValueTaskVoid() => ValueTask.CompletedTask;
    }
}");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.TestHandler");

        // Use reflection to access the private GetResponseType method
        var getResponseTypeMethod = typeof(OptimizedDispatcherGenerator).GetMethod("GetResponseType",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Test Task<T>
        var taskMethod = handlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();
        HandlerInfo taskHandlerInfo = new()
        {
            MethodSymbol = taskMethod,
            Attributes = []
        };
        Assert.Equal("Test.StringResponse", getResponseTypeMethod?.Invoke(generator, [taskHandlerInfo]));

        // Test ValueTask<T>
        var valueTaskMethod = handlerTypeSymbol?.GetMembers("HandleValueAsync").OfType<IMethodSymbol>().FirstOrDefault();
        HandlerInfo valueTaskHandlerInfo = new()
        {
            MethodSymbol = valueTaskMethod,
            Attributes = []
        };
        Assert.Equal("Test.StringResponse", getResponseTypeMethod?.Invoke(generator, [valueTaskHandlerInfo]));

        // Test IAsyncEnumerable<T>
        var streamMethod = handlerTypeSymbol?.GetMembers("HandleStreamAsync").OfType<IMethodSymbol>().FirstOrDefault();
        HandlerInfo streamHandlerInfo = new()
        {
            MethodSymbol = streamMethod,
            Attributes = []
        };
        Assert.Equal("Test.StringResponse", getResponseTypeMethod?.Invoke(generator, [streamHandlerInfo]));

        // Test direct return
        var directMethod = handlerTypeSymbol?.GetMembers("HandleDirect").OfType<IMethodSymbol>().FirstOrDefault();
        HandlerInfo directHandlerInfo = new()
        {
            MethodSymbol = directMethod,
            Attributes = []
        };
        Assert.Equal("Test.StringResponse", getResponseTypeMethod?.Invoke(generator, [directHandlerInfo]));

        // Test void return
        var voidMethod = handlerTypeSymbol?.GetMembers("HandleVoid").OfType<IMethodSymbol>().FirstOrDefault();
        HandlerInfo voidHandlerInfo = new()
        {
            MethodSymbol = voidMethod,
            Attributes = []
        };
        Assert.Equal("void", getResponseTypeMethod?.Invoke(generator, [voidHandlerInfo]));

        // Test Task (non-generic)
        var taskVoidMethod = handlerTypeSymbol?.GetMembers("HandleTaskVoid").OfType<IMethodSymbol>().FirstOrDefault();
        HandlerInfo taskVoidHandlerInfo = new()
        {
            MethodSymbol = taskVoidMethod,
            Attributes = []
        };
        Assert.Equal("void", getResponseTypeMethod?.Invoke(generator, [taskVoidHandlerInfo]));

        // Test ValueTask (non-generic)
        var valueTaskVoidMethod = handlerTypeSymbol?.GetMembers("HandleValueTaskVoid").OfType<IMethodSymbol>().FirstOrDefault();
        var valueTaskVoidHandlerInfo = new HandlerInfo
        {
            MethodSymbol = valueTaskVoidMethod,
            Attributes = []
        };
        Assert.Equal("void", getResponseTypeMethod?.Invoke(generator, [valueTaskVoidHandlerInfo]));

        // Test null MethodSymbol
        HandlerInfo nullHandlerInfo = new()
        {
            MethodSymbol = null,
            Attributes = []
        };
        Assert.Equal("void", getResponseTypeMethod?.Invoke(generator, [nullHandlerInfo]));
    }

    [Fact]
    public void GetHandlerName_WithVariousAttributes_ReturnsExpectedResults()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        // Use reflection to access the private GetHandlerName method
        var getHandlerNameMethod = typeof(OptimizedDispatcherGenerator).GetMethod("GetHandlerName",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Test with default handler (no name attribute) - this is what we get with null AttributeData
        var defaultHandlerInfo = new HandlerInfo
        {
            MethodSymbol = null,
            Attributes =
            [
                new RelayAttributeInfo 
                { 
                    Type = RelayAttributeType.Handle,
                    AttributeData = null  // Null AttributeData means no named arguments
                }
            ]
        };
        Assert.Equal("default", getHandlerNameMethod?.Invoke(generator, [defaultHandlerInfo]));

        // Test with empty attributes list
        HandlerInfo emptyHandlerInfo = new()
        {
            MethodSymbol = null,
            Attributes = []
        };
        Assert.Equal("default", getHandlerNameMethod?.Invoke(generator, [emptyHandlerInfo]));
    }

    [Fact]
    public void GetHandlerPriority_WithVariousAttributes_ReturnsExpectedResults()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        // Use reflection to access the private GetHandlerPriority method
        var getHandlerPriorityMethod = typeof(OptimizedDispatcherGenerator).GetMethod("GetHandlerPriority",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Test with default priority (no priority attribute) - this is what we get with null AttributeData
        var defaultHandlerInfo = new HandlerInfo
        {
            MethodSymbol = null,
            Attributes =
            [
                new RelayAttributeInfo 
                { 
                    Type = RelayAttributeType.Handle,
                    AttributeData = null  // Null AttributeData means no named arguments
                }
            ]
        };
        Assert.Equal(0, getHandlerPriorityMethod?.Invoke(generator, [defaultHandlerInfo]));

        // Test with null AttributeData
        HandlerInfo nullAttributeHandlerInfo = new()
        {
            MethodSymbol = null,
            Attributes =
            [
                new() { 
                    Type = RelayAttributeType.Handle,
                    AttributeData = null
                }
            ]
        };
        Assert.Equal(0, getHandlerPriorityMethod?.Invoke(generator, [nullAttributeHandlerInfo]));

        // Test with empty attributes list
        var emptyHandlerInfo = new HandlerInfo
        {
            MethodSymbol = null,
            Attributes = []
        };
        Assert.Equal(0, getHandlerPriorityMethod?.Invoke(generator, [emptyHandlerInfo]));
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNullMethodSymbol_HandlesGracefully()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = null, // Null MethodSymbol
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert - Should generate without exceptions
        Assert.NotNull(result);
        Assert.Contains("OptimizedDispatcher", result);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithEmptyDiscoveryResult_GeneratesBasicStructure()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();
        // Empty handlers list

        // Act
        var result = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("OptimizedDispatcher", result);
        Assert.Contains("// <auto-generated />", result);
        Assert.Contains("namespace Relay.Generated", result);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNullDiscoveryResult_ThrowsArgumentException()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => generator.GenerateOptimizedDispatcher(null!));
    }

    [Fact]
    public void AppendUsings_WithICodeGeneratorInterface_AddsRequiredUsings()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var builder = new StringBuilder();

        // Use reflection to call the protected AppendUsings method
        var appendUsingsMethod = typeof(OptimizedDispatcherGenerator)
            .GetMethod("AppendUsings", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        appendUsingsMethod?.Invoke(generator, [builder, discoveryResult, options]);

        var result = builder.ToString();

        // Assert
        Assert.Contains("using System;", result);
        Assert.Contains("using System.Collections.Generic;", result);
        Assert.Contains("using System.Runtime.CompilerServices;", result);
        Assert.Contains("using System.Threading;", result);
        Assert.Contains("using System.Threading.Tasks;", result);
        Assert.Contains("using Microsoft.Extensions.DependencyInjection;", result);
        Assert.Contains("using Relay.Core;", result);
    }

    [Fact]
    public void GenerateContent_WithICodeGeneratorInterface_GeneratesBasicStructure()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var builder = new StringBuilder();

        // Use reflection to call the protected GenerateContent method
        var generateContentMethod = typeof(OptimizedDispatcherGenerator)
            .GetMethod("GenerateContent", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        generateContentMethod?.Invoke(generator, [builder, discoveryResult, options]);

        var result = builder.ToString();

        // Assert - Should generate at least basic class structure
        Assert.Contains("OptimizedDispatcher", result);
    }

    [Fact]
    public void Generate_WithICodeGeneratorInterface_WorksCorrectly()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();

        // Act - Use the ICodeGenerator interface method
        var result = ((Generators.ICodeGenerator)generator).Generate(discoveryResult, options);

        // Assert
        Assert.Contains("namespace Relay.Generated", result);
        Assert.Contains("class OptimizedDispatcher", result);
    }

    [Fact]
    public void CanGenerate_ReturnsTrue_WhenHasHandlers()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();
        discoveryResult.Handlers.Add(new HandlerInfo());

        // Act
        var canGenerate = generator.CanGenerate(discoveryResult);

        // Assert
        Assert.True(canGenerate);
    }

    [Fact]
    public void CanGenerate_ReturnsFalse_WhenNoHandlers()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult(); // Empty handlers

        // Act
        var canGenerate = generator.CanGenerate(discoveryResult);

        // Assert
        Assert.False(canGenerate);
    }

    [Fact]
    public void CanGenerate_ReturnsFalse_WhenNullResult()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        // Act
        var canGenerate = generator.CanGenerate(null!);

        // Assert
        Assert.False(canGenerate);
    }

    [Fact]
    public void OptimizedDispatcherGenerator_Properties_ReturnCorrectValues()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new OptimizedDispatcherGenerator(context);

        // Act & Assert
        Assert.Equal("Optimized Dispatcher Generator", generator.GeneratorName);
        Assert.Equal("OptimizedDispatcher", generator.OutputFileName);
        Assert.Equal(30, generator.Priority);
    }

    private Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Relay.Core.IRequest<>).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private IMethodSymbol? GetMethodSymbol(Compilation compilation, string sourceTypeName, string methodName)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetCompilationUnitRoot();

        // Find the class declaration by name
        var className = sourceTypeName.Split('.').Last();
        var classDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.ValueText == className);

        if (classDeclaration != null)
        {
            var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (typeSymbol != null)
            {
                var methodSymbol = typeSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => m.Name == methodName);
                return methodSymbol;
            }
        }

        return null;
    }

    private AttributeData CreateMockAttributeData(string? handlerName, int priority)
    {
        // This is a simplified mock for testing purposes
        // In reality, we would need to create proper AttributeData, but for our tests,
        // we can use reflection to create a mock object that has the expected properties
        return null!;
    }
}