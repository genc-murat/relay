using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.SourceGenerator.Tests;

public class RelayIncrementalGeneratorCoreTests
{
    // Note: IsCandidateHandlerClass is a private method, so it cannot be tested directly
    // It's tested indirectly through the full generator functionality

    // Note: IsCandidateHandlerClass is a private method, so it cannot be tested directly
    // It's tested indirectly through the full generator functionality

    // Note: IsCandidateHandlerClass is a private method, so it cannot be tested directly
    // It's tested indirectly through the full generator functionality

    // Note: IsMethodWithRelayAttribute is a private method, so it cannot be tested directly
    // It's tested indirectly through the full generator functionality

    // Note: IsMethodWithRelayAttribute is a private method, so it cannot be tested directly
    // It's tested indirectly through the full generator functionality

    // Note: IsMethodWithRelayAttribute is a private method, so it cannot be tested directly
    // It's tested indirectly through the full generator functionality

    // Note: IsMethodWithRelayAttribute is a private method, so it cannot be tested directly
    // It's tested indirectly through the full generator functionality

    // Note: IsMethodWithRelayAttribute is a private method, so it cannot be tested directly
    // It's tested indirectly through the full generator functionality

    // Note: GetSemanticHandlerInfo method cannot be tested directly since GeneratorSyntaxContext
    // is provided internally by the source generator framework and cannot be constructed in tests

    [Fact]
    public void GetSemanticHandlerInfo_WithValidNotificationHandler_ReturnsHandlerInfo()
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
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        // Note: Cannot directly test GetSemanticHandlerInfo due to GeneratorSyntaxContext being internal
        // Test covered indirectly through ProcessClassSymbol method
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        Assert.Single(result.ImplementedInterfaces);
        Assert.Equal(HandlerType.Notification, result.ImplementedInterfaces[0].InterfaceType);
        Assert.Equal("TestNotification", result.ImplementedInterfaces[0].RequestType?.Name);
    }

    [Fact]
    public void GetSemanticHandlerInfo_WithValidStreamHandler_ReturnsHandlerInfo()
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
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        // Note: Cannot directly test GetSemanticHandlerInfo due to GeneratorSyntaxContext being internal
        // Test covered indirectly through ProcessClassSymbol method
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        Assert.Single(result.ImplementedInterfaces);
        Assert.Equal(HandlerType.Stream, result.ImplementedInterfaces[0].InterfaceType);
        Assert.Equal("TestRequest", result.ImplementedInterfaces[0].RequestType?.Name);
        Assert.Equal("String", result.ImplementedInterfaces[0].ResponseType?.Name);
    }

    [Fact]
    public void GetSemanticHandlerInfo_WithClassThatDoesNotImplementHandlerInterface_ReturnsNull()
    {
        // Arrange
        var source = @"
using System;

namespace TestProject
{
    public class TestClass : IDisposable
    {
        public void Dispose() { }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestClass");

        // Note: Cannot directly test GetSemanticHandlerInfo due to GeneratorSyntaxContext being internal
        // Test covered indirectly through ProcessClassSymbol method
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSemanticHandlerInfo_WithMultipleHandlerInterfaces_ReturnsHandlerInfo()
    {
        // Arrange
        var source = @"
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
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "MultiHandler");

        // Note: Cannot directly test GetSemanticHandlerInfo due to GeneratorSyntaxContext being internal
        // Test covered indirectly through ProcessClassSymbol method
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MultiHandler", result.ClassSymbol.Name);
        Assert.Equal(2, result.ImplementedInterfaces.Count);
        
        var requestInterface = result.ImplementedInterfaces.First(i => i.InterfaceType == HandlerType.Request);
        Assert.Equal(HandlerType.Request, requestInterface.InterfaceType);
        Assert.Equal("TestRequest", requestInterface.RequestType?.Name);
        Assert.Equal("String", requestInterface.ResponseType?.Name);

        var notificationInterface = result.ImplementedInterfaces.First(i => i.InterfaceType == HandlerType.Notification);
        Assert.Equal(HandlerType.Notification, notificationInterface.InterfaceType);
        Assert.Equal("TestNotification", notificationInterface.RequestType?.Name);
    }

    [Fact]
    public void ProcessClassSymbol_WithRequestHandler_ReturnsCorrectHandlerInfo()
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
        var classSymbol = compilation.GetTypeByMetadataName("TestProject.TestHandler") as Microsoft.CodeAnalysis.INamedTypeSymbol;
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        Assert.Single(result.ImplementedInterfaces);
        Assert.Equal(HandlerType.Request, result.ImplementedInterfaces[0].InterfaceType);
        Assert.Equal("TestRequest", result.ImplementedInterfaces[0].RequestType?.Name);
        Assert.Equal("String", result.ImplementedInterfaces[0].ResponseType?.Name);
    }

    [Fact]
    public void ProcessClassSymbol_WithNotificationHandler_ReturnsCorrectHandlerInfo()
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
        var classSymbol = compilation.GetTypeByMetadataName("TestProject.TestHandler") as Microsoft.CodeAnalysis.INamedTypeSymbol;
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        Assert.Single(result.ImplementedInterfaces);
        Assert.Equal(HandlerType.Notification, result.ImplementedInterfaces[0].InterfaceType);
        Assert.Equal("TestNotification", result.ImplementedInterfaces[0].RequestType?.Name);
    }

    [Fact]
    public void ProcessClassSymbol_WithStreamHandler_ReturnsCorrectHandlerInfo()
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
        var classSymbol = compilation.GetTypeByMetadataName("TestProject.TestHandler") as Microsoft.CodeAnalysis.INamedTypeSymbol;
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestHandler");

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestHandler", result.ClassSymbol.Name);
        Assert.Single(result.ImplementedInterfaces);
        Assert.Equal(HandlerType.Stream, result.ImplementedInterfaces[0].InterfaceType);
        Assert.Equal("TestRequest", result.ImplementedInterfaces[0].RequestType?.Name);
        Assert.Equal("String", result.ImplementedInterfaces[0].ResponseType?.Name);
    }

    [Fact]
    public void ProcessClassSymbol_WithMultipleHandlerInterfaces_ReturnsCorrectHandlerInfo()
    {
        // Arrange
        var source = @"
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
        var classSymbol = compilation.GetTypeByMetadataName("TestProject.MultiHandler") as Microsoft.CodeAnalysis.INamedTypeSymbol;
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "MultiHandler");

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MultiHandler", result.ClassSymbol.Name);
        Assert.Equal(2, result.ImplementedInterfaces.Count);
        
        var requestInterface = result.ImplementedInterfaces.First(i => i.InterfaceType == HandlerType.Request);
        Assert.Equal(HandlerType.Request, requestInterface.InterfaceType);
        Assert.Equal("TestRequest", requestInterface.RequestType?.Name);
        Assert.Equal("String", requestInterface.ResponseType?.Name);

        var notificationInterface = result.ImplementedInterfaces.First(i => i.InterfaceType == HandlerType.Notification);
        Assert.Equal(HandlerType.Notification, notificationInterface.InterfaceType);
        Assert.Equal("TestNotification", notificationInterface.RequestType?.Name);
    }

    [Fact]
    public void ProcessClassSymbol_WithClassThatDoesNotImplementHandlerInterface_ReturnsNull()
    {
        // Arrange
        var source = @"
using System;

namespace TestProject
{
    public class TestClass : IDisposable
    {
        public void Dispose() { }
    }
}";

        var compilation = CreateTestCompilation(source);
        var classSymbol = compilation.GetTypeByMetadataName("TestProject.TestClass") as Microsoft.CodeAnalysis.INamedTypeSymbol;
        var syntaxTree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the stubs
        var classDecl = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestClass");

        // Act
        var result = RelayIncrementalGenerator.ProcessClassSymbol(classSymbol, classDecl);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateBasicAddRelayMethod_CreatesValidSource()
    {
        // This test verifies that when no handlers are found, the basic AddRelay method is generated
        var source = @"
namespace TestProject
{
    public class SomeClass
    {
        public int Value { get; set; }
    }
}";

        var generator = new RelayIncrementalGenerator();
        var compilation = CreateTestCompilation(source);

        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var runResult = driver.GetRunResult();

        // Assert - Should not crash and should generate basic AddRelay method
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        Assert.Contains("RelayRegistration.g.cs", runResult.GeneratedTrees.Select(t => t.FilePath).Select(p => System.IO.Path.GetFileName(p)));
        Assert.Contains("AddRelay", generatedCode);
    }

    // Note: GetStringBuilder and ReturnStringBuilder are internal methods, so they cannot be tested directly
    // They are tested as part of the source generation functionality

    // Note: GetStringBuilder and ReturnStringBuilder are internal methods, so they cannot be tested directly
    // They are tested as part of the source generation functionality

    private static Compilation CreateTestCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
using System.Threading;
using System.Threading.Tasks;

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
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}