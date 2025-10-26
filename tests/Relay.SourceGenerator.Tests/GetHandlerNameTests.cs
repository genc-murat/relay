using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

public class GetHandlerNameTests
{
    [Fact]
    public void GetHandlerName_WithValidName_Should_ReturnThatName()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle(Name = ""CustomName"")]
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(st => st.ToString().Contains("TestProject"));
        var root = syntaxTree.GetRoot();
        
        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == "Handle");

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        
        var context = new RelayCompilationContext(compilation, default);
        var generator = new HandlerRegistryGenerator(context);

        // Get the attribute data from the method
        var attributeData = methodSymbol?.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "HandleAttribute");

        var handlerInfo = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle, AttributeData = attributeData }
            ]
        };

        // Use reflection to access the private GetHandlerName method
        var getHandlerNameMethod = typeof(HandlerRegistryGenerator).GetMethod("GetHandlerName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = getHandlerNameMethod?.Invoke(generator, new object[] { handlerInfo });

        // Assert
        Assert.Equal("CustomName", result);
    }

    [Fact]
    public void GetHandlerName_WhitespaceName_Should_ReturnDefault()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle(Name = ""   "")]  // Whitespace name
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(st => st.ToString().Contains("TestProject"));
        var root = syntaxTree.GetRoot();
        
        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == "Handle");

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        
        var context = new RelayCompilationContext(compilation, default);
        var generator = new HandlerRegistryGenerator(context);

        // Get the attribute data from the method
        var attributeData = methodSymbol?.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "HandleAttribute");

        var handlerInfo = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            Attributes = new List<RelayAttributeInfo>
            {
                new() { Type = RelayAttributeType.Handle, AttributeData = attributeData }
            }
        };

        // Use reflection to access the private GetHandlerName method
        var getHandlerNameMethod = typeof(HandlerRegistryGenerator).GetMethod("GetHandlerName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = getHandlerNameMethod?.Invoke(generator, new object[] { handlerInfo });

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void GetHandlerName_EmptyName_Should_ReturnDefault()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle(Name = """")]  // Empty name
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(st => st.ToString().Contains("TestProject"));
        var root = syntaxTree.GetRoot();
        
        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == "Handle");

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        
        var context = new RelayCompilationContext(compilation, default);
        var generator = new HandlerRegistryGenerator(context);

        // Get the attribute data from the method
        var attributeData = methodSymbol?.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "HandleAttribute");

        var handlerInfo = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle, AttributeData = attributeData }
            ]
        };

        // Use reflection to access the private GetHandlerName method
        var getHandlerNameMethod = typeof(HandlerRegistryGenerator).GetMethod("GetHandlerName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = getHandlerNameMethod?.Invoke(generator, new object[] { handlerInfo });

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void GetHandlerName_WithoutNameProperty_Should_ReturnDefault()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]  // No Name property specified
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(st => st.ToString().Contains("TestProject"));
        var root = syntaxTree.GetRoot();
        
        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == "Handle");

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        
        var context = new RelayCompilationContext(compilation, default);
        var generator = new HandlerRegistryGenerator(context);

        // Get the attribute data from the method
        var attributeData = methodSymbol?.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "HandleAttribute");

        var handlerInfo = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            Attributes = new List<RelayAttributeInfo>
            {
                new() { Type = RelayAttributeType.Handle, AttributeData = attributeData }
            }
        };

        // Use reflection to access the private GetHandlerName method
        var getHandlerNameMethod = typeof(HandlerRegistryGenerator).GetMethod("GetHandlerName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = getHandlerNameMethod?.Invoke(generator, [handlerInfo]);

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void GetHandlerName_WithoutHandleAttribute_Should_ReturnDefault()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        // No Handle attribute at all
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(st => st.ToString().Contains("TestProject"));
        var root = syntaxTree.GetRoot();
        
        var methodDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == "Handle");

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        
        var context = new RelayCompilationContext(compilation, default);
        var generator = new HandlerRegistryGenerator(context);

        // No Handle attribute at all
        var handlerInfo = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            Attributes = new List<RelayAttributeInfo>() // Empty list, no Handle attribute
        };

        // Use reflection to access the private GetHandlerName method
        var getHandlerNameMethod = typeof(HandlerRegistryGenerator).GetMethod("GetHandlerName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = getHandlerNameMethod?.Invoke(generator, new object[] { handlerInfo });

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void GetHandlerName_WithNullAttributeData_Should_ReturnDefault()
    {
        // Arrange
        var compilation = CreateTestCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new HandlerRegistryGenerator(context);

        // Create a handler with handle attribute but null AttributeData
        var handlerInfo = new HandlerInfo
        {
            MethodSymbol = null,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle, AttributeData = null }
            ]
        };

        // Use reflection to access the private GetHandlerName method
        var getHandlerNameMethod = typeof(HandlerRegistryGenerator).GetMethod("GetHandlerName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = getHandlerNameMethod?.Invoke(generator, new object[] { handlerInfo });

        // Assert
        Assert.Equal("default", result);
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