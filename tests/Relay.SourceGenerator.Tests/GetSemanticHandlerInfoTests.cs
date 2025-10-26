using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator.Tests;

public class GetSemanticHandlerInfoTests
{
    [Fact]
    public void ProcessClassSymbol_Should_Identify_Request_Handler_Interface()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(s => s.ToString().Contains("TestHandler"));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDeclaration = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        Assert.NotNull(classSymbol); // Make sure we have a valid symbol

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDeclaration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        Assert.Single(result.ImplementedInterfaces);
        var interfaceInfo = result.ImplementedInterfaces.First();
        Assert.Equal(HandlerType.Request, interfaceInfo.InterfaceType);
        Assert.Contains("IRequestHandler", interfaceInfo.InterfaceSymbol.Name);
    }

    [Fact]
    public void ProcessClassSymbol_Should_Identify_Notification_Handler_Interface()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Notifications;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestNotification : INotification { }
    
    public class TestHandler : INotificationHandler<TestNotification>
    {
        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(s => s.ToString().Contains("TestHandler"));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDeclaration = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        Assert.NotNull(classSymbol); // Make sure we have a valid symbol

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDeclaration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        Assert.Single(result.ImplementedInterfaces);
        var interfaceInfo = result.ImplementedInterfaces.First();
        Assert.Equal(HandlerType.Notification, interfaceInfo.InterfaceType);
        Assert.Contains("INotificationHandler", interfaceInfo.InterfaceSymbol.Name);
    }

    [Fact]
    public void ProcessClassSymbol_Should_Identify_Stream_Handler_Interface()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<IAsyncEnumerable<string>> { }
    
    public class TestHandler : IStreamHandler<TestRequest, string>
    {
        public IAsyncEnumerable<string> HandleStreamAsync(TestRequest request, CancellationToken cancellationToken)
        {
            yield break;
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(s => s.ToString().Contains("TestHandler"));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDeclaration = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        Assert.NotNull(classSymbol); // Make sure we have a valid symbol

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDeclaration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        Assert.Single(result.ImplementedInterfaces);
        var interfaceInfo = result.ImplementedInterfaces.First();
        Assert.Equal(HandlerType.Stream, interfaceInfo.InterfaceType);
        Assert.Contains("IStreamHandler", interfaceInfo.InterfaceSymbol.Name);
    }

    [Fact]
    public void ProcessClassSymbol_Should_Identify_Multiple_Interfaces()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Notifications;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    public class TestNotification : INotification { }
    
    public class MultiHandler : 
        IRequestHandler<TestRequest, string>,
        INotificationHandler<TestNotification>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test"");
        }
        
        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(s => s.ToString().Contains("MultiHandler"));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDeclaration = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "MultiHandler");

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        Assert.NotNull(classSymbol); // Make sure we have a valid symbol

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDeclaration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MultiHandler", result.ClassSymbol.Name);
        Assert.Equal(2, result.ImplementedInterfaces.Count);
        
        var interfaces = result.ImplementedInterfaces.ToList();
        Assert.Contains(interfaces, i => i.InterfaceType == HandlerType.Request);
        Assert.Contains(interfaces, i => i.InterfaceType == HandlerType.Notification);
    }

    [Fact]
    public void ProcessClassSymbol_Should_Return_Null_For_Classes_Without_Handler_Interfaces()
    {
        // Arrange
        var source = @"
namespace TestProject
{
    public class RegularClass
    {
        public void DoSomething() { }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(s => s.ToString().Contains("RegularClass"));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDeclaration = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "RegularClass");

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        Assert.NotNull(classSymbol); // Make sure we have a valid symbol

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDeclaration);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ProcessClassSymbol_Should_Handle_Inheritance_Chain_Interfaces()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class BaseRequest : IRequest<string> { }
    
    public interface IBaseHandler : IRequestHandler<BaseRequest, string> { }
    
    public class TestHandler : IBaseHandler
    {
        public ValueTask<string> HandleAsync(BaseRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First(s => s.ToString().Contains("TestHandler"));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDeclaration = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        Assert.NotNull(classSymbol); // Make sure we have a valid symbol

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDeclaration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        // Should find interfaces in the inheritance chain
        Assert.Contains(result.ImplementedInterfaces, i => i.InterfaceType == HandlerType.Request);
    }

    private static Compilation CreateTestCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Relay.Core.Contracts.Requests
{
    public interface IRequest<out TResponse> { }
    public interface IRequest { }
}

namespace Relay.Core.Contracts.Notifications
{
    public interface INotification { }
}

namespace Relay.Core.Contracts.Handlers
{
    public interface IRequestHandler<in TRequest, TResponse>
    {
        ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
    
    public interface IRequestHandler<in TRequest>
    {
        ValueTask HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
    
    public interface INotificationHandler<in TNotification>
    {
        ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }
    
    public interface IStreamHandler<in TRequest, TResponse>
    {
        IAsyncEnumerable<TResponse> HandleStreamAsync(TRequest request, CancellationToken cancellationToken);
    }
}

namespace Relay.Core
{
    public interface IRequest<out TResponse> { }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandleAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NotificationAttribute : Attribute
    {
        public int Priority { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PipelineAttribute : Attribute
    {
        public int Order { get; set; }
        public string? Scope { get; set; }
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
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}