using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.SourceGenerator.Tests;

public class RelayIncrementalGeneratorAllTests
{
    [Fact]
    public void IsRelayAttributeName_WithNullInput_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithWhitespaceOnly_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithHandleAttribute_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("HandleAttribute");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithNotificationAttribute_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("NotificationAttribute");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithPipelineAttribute_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("PipelineAttribute");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithExposeAsEndpointAttribute_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("ExposeAsEndpointAttribute");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithHandle_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Handle");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithNotification_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Notification");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithPipeline_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Pipeline");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithExposeAsEndpoint_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("ExposeAsEndpoint");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithUnknownAttribute_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("UnknownAttribute");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithUnknownAttributeWithoutSuffix_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Unknown");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRelayAttributeName_WithJustAttributeSuffix_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsRelayAttributeName("Attribute");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHandlerInterface_WithRequestHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IRequestHandler");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithNotificationHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("INotificationHandler");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithStreamHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IStreamHandler");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithGenericRequestHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IRequestHandler<string>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithGenericNotificationHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("INotificationHandler<MyEvent>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithGenericStreamHandler_ReturnsTrue()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IStreamHandler<Request, Response>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandlerInterface_WithNonHandlerInterface_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface("IDisposable");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHandlerInterface_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = RelayIncrementalGenerator.IsHandlerInterface(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRequestHandlerInterface_WithValidInterface_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace Relay.Core.Contracts.Handlers
{
    public interface IRequestHandler<in TRequest, TResponse>
    {
        System.Threading.Tasks.ValueTask<TResponse> HandleAsync(TRequest request, System.Threading.CancellationToken cancellationToken);
    }
}
");
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IRequestHandler`2");

        // Act
        var result = RelayIncrementalGenerator.IsRequestHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNotificationHandlerInterface_WithValidInterface_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace Relay.Core.Contracts.Handlers
{
    public interface INotificationHandler<in TNotification>
    {
        System.Threading.Tasks.ValueTask HandleAsync(TNotification notification, System.Threading.CancellationToken cancellationToken);
    }
}
");
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.INotificationHandler`1");

        // Act
        var result = RelayIncrementalGenerator.IsNotificationHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsStreamHandlerInterface_WithValidInterface_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace Relay.Core.Contracts.Handlers
{
    public interface IStreamHandler<in TRequest, TResponse>
    {
        System.Collections.Generic.IAsyncEnumerable<TResponse> HandleStreamAsync(TRequest request, System.Threading.CancellationToken cancellationToken);
    }
}
");
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IStreamHandler`2");

        // Act
        var result = RelayIncrementalGenerator.IsStreamHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRequestHandlerInterface_WithNonHandlerInterface_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
using System;

public interface ITestInterface { }
");
        var interfaceSymbol = compilation.GetTypeByMetadataName("ITestInterface");

        // Act
        var result = RelayIncrementalGenerator.IsRequestHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNotificationHandlerInterface_WithNonHandlerInterface_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
using System;

public interface ITestInterface { }
");
        var interfaceSymbol = compilation.GetTypeByMetadataName("ITestInterface");

        // Act
        var result = RelayIncrementalGenerator.IsNotificationHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStreamHandlerInterface_WithNonHandlerInterface_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
using System;

public interface ITestInterface { }
");
        var interfaceSymbol = compilation.GetTypeByMetadataName("ITestInterface");

        // Act
        var result = RelayIncrementalGenerator.IsStreamHandlerInterface(interfaceSymbol!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ParseConfiguration_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => RelayIncrementalGenerator.ParseConfiguration(null!));
    }

    [Fact]
    public void ParseConfiguration_WithEmptyOptions_ReturnsDefaultConfiguration()
    {
        // Arrange
        var options = new TestAnalyzerConfigOptionsProvider();

        // Act
        var result = RelayIncrementalGenerator.ParseConfiguration(options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ParseConfiguration_WithMSBuildProperties_SetsOptionsCorrectly()
    {
        // Arrange
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.RelayEnableDIGeneration", "false");
        options.Options.Add("build_property.RelayEnableOptimizedDispatcher", "true");
        options.Options.Add("build_property.RelayIncludeDocumentation", "false");

        // Act
        var result = RelayIncrementalGenerator.ParseConfiguration(options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.False(result.Options.EnableDIGeneration);
        Assert.True(result.Options.EnableOptimizedDispatcher);
        Assert.False(result.Options.IncludeDocumentation);
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
    public void Generator_Should_Handle_RequestHandler_Interface_Implementations()
    {
        // Arrange - Test IRequestHandler interface implementation
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

        var generator = new RelayIncrementalGenerator();
        var compilation = CreateTestCompilation(source);

        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var runResult = driver.GetRunResult();

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotEmpty(runResult.GeneratedTrees);
    }

    [Fact]
    public void Generator_Should_Handle_NotificationHandler_Interface_Implementations()
    {
        // Arrange - Test INotificationHandler interface implementation
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

        var generator = new RelayIncrementalGenerator();
        var compilation = CreateTestCompilation(source);

        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var runResult = driver.GetRunResult();

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotEmpty(runResult.GeneratedTrees);
    }

    [Fact]
    public void Generator_Should_Handle_StreamHandler_Interface_Implementations()
    {
        // Arrange - Test IStreamHandler interface implementation
        var source = @"
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

        var generator = new RelayIncrementalGenerator();
        var compilation = CreateTestCompilation(source);

        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var runResult = driver.GetRunResult();

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotEmpty(runResult.GeneratedTrees);
    }

    [Fact]
    public void Generator_Should_Generate_DI_Registrations_For_Multiple_Handlers()
    {
        // Arrange - Multiple different handler types
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Notifications;

namespace TestProject
{
    public class TestRequest1 : IRequest<string> { }
    public class TestRequest2 : IRequest<int> { }
    public class TestNotification : INotification { }

    public class TestHandler1 : IRequestHandler<TestRequest1, string>
    {
        public ValueTask<string> HandleAsync(TestRequest1 request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test1"");
        }
    }

    public class TestHandler2 : IRequestHandler<TestRequest2, int>
    {
        public ValueTask<int> HandleAsync(TestRequest2 request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(42);
        }
    }

    public class TestHandler3 : INotificationHandler<TestNotification>
    {
        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
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

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotEmpty(runResult.Results);

        // Should generate both DI registration and optimized dispatcher
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        Assert.Contains("AddRelay", generatedCode);
        Assert.Contains("IRequestHandler", generatedCode);
        Assert.Contains("INotificationHandler", generatedCode);
    }

    [Fact]
    public void Generator_Should_Generate_OptimizedRequestDispatcher_With_RequestHandlers()
    {
        // Arrange - Test that GenerateOptimizedRequestDispatcher is called and produces correct output
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }

    public class StringRequest : IRequest<string> { }

    public class IntRequest : IRequest<int> { }

    public class VoidRequest : IRequest { }

    public class TestHandler1 : IRequestHandler<TestRequest, string>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test1"");
        }
    }

    public class TestHandler2 : IRequestHandler<StringRequest, string>
    {
        public ValueTask<string> HandleAsync(StringRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test2"");
        }
    }

    public class TestHandler3 : IRequestHandler<IntRequest, int>
    {
        public ValueTask<int> HandleAsync(IntRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(42);
        }
    }

    public class VoidHandler : IRequestHandler<VoidRequest>
    {
        public ValueTask HandleAsync(VoidRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
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

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotEmpty(runResult.GeneratedTrees);

        // Check that OptimizedRequestDispatcher.g.cs was generated
        var optimizedDispatcherFile = runResult.GeneratedTrees.FirstOrDefault(tree =>
            tree.FilePath.Contains("OptimizedRequestDispatcher.g.cs"));
        Assert.NotNull(optimizedDispatcherFile);

        var generatedCode = optimizedDispatcherFile.ToString();

        // Verify the GenerateOptimizedRequestDispatcher method produced correct output
        Assert.Contains("// Generated by Relay.SourceGenerator - Optimized Request Dispatcher", generatedCode);
        Assert.Contains("public class GeneratedRequestDispatcher : BaseRequestDispatcher", generatedCode);
        Assert.Contains("public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)", generatedCode);
        Assert.Contains("public override ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)", generatedCode);

        // Verify switch cases for response requests
        Assert.Contains("TestProject.TestRequest req when request is TestProject.TestRequest =>", generatedCode);
        Assert.Contains("TestProject.StringRequest req when request is TestProject.StringRequest =>", generatedCode);
        Assert.Contains("TestProject.IntRequest req when request is TestProject.IntRequest =>", generatedCode);

        // Verify switch cases for void requests
        Assert.Contains("TestProject.VoidRequest req =>", generatedCode);

        // Verify error handling
        Assert.Contains("HandlerNotFoundException", generatedCode);

        // Verify AggressiveInlining attribute
        Assert.Contains("MethodImpl(MethodImplOptions.AggressiveInlining)", generatedCode);
    }

    [Fact]
    public void Execute_Should_Catch_Exceptions_And_Report_Diagnostic()
    {
        // Arrange - Use test hook to force exception in generation
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

        try
        {
            var generator = new RelayIncrementalGenerator();
            var compilation = CreateTestCompilation(source);

            // Force the exception during DI registration source generation
            RelayIncrementalGenerator.TestForceException = true;

            // Act
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            // Assert
            var errorDiagnostic = diagnostics.FirstOrDefault(d => d.Id == "RELAY_GEN_001"); // GeneratorError
            Assert.NotNull(errorDiagnostic);
            Assert.Contains("Source generator error", errorDiagnostic.GetMessage());
        }
        finally
        {
            RelayIncrementalGenerator.TestForceException = false;
        }
    }

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
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public Dictionary<string, string> Options { get; } = new();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return new TestAnalyzerConfigOptions(Options);
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new TestAnalyzerConfigOptions(Options);
        }

        public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(this.Options);
    }

    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public TestAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value!);
        }
    }
}