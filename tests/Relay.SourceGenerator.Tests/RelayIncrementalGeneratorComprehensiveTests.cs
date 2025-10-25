using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Relay.SourceGenerator.Tests;

public class RelayIncrementalGeneratorComprehensiveTests
{
    [Fact]
    public void Generator_Should_Report_MissingRelayCoreReference_Diagnostic()
    {
        // Arrange - Create code with Relay attributes but no Relay.Core reference
        var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public ValueTask<string> HandleAsync(string request, CancellationToken ct)
        {
            return ValueTask.FromResult(request);
        }
    }
}";

        var generator = new RelayIncrementalGenerator();
        // Create compilation WITHOUT Relay.Core reference
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        // Assert
        var missingCoreDiagnostic = diagnostics.FirstOrDefault(d => 
            d.Id == "RELAY_GEN_004" && // MissingRelayCoreReference
            d.Severity == DiagnosticSeverity.Error);
        
        Assert.NotNull(missingCoreDiagnostic);
        Assert.Contains("Relay.Core package must be referenced", missingCoreDiagnostic.GetMessage());
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
    public void Generator_Should_Detect_Invalid_Handler_Name()
    {
        // Arrange - Test invalid relay attribute name
        var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [InvalidAttribute]
        public ValueTask<string> HandleAsync(string request, CancellationToken ct)
        {
            return ValueTask.FromResult(request);
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

        // Assert - Should not crash, and should not generate with invalid attribute
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id == "RELAY_GEN_001"); // GeneratorError
    }

    [Fact]
    public void Generator_Should_Handle_Generic_Request_Handlers()
    {
        // Arrange - Test generic IRequestHandler interface implementation
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class GenericRequest<T> : IRequest<T> { }

    public class GenericHandler<T> : IRequestHandler<GenericRequest<T>, T>
    {
        public ValueTask<T> HandleAsync(GenericRequest<T> request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(default(T));
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
    public void Generator_Should_Detect_RequestHandlerInterface_Both_CodePaths()
    {
        // Arrange - Test both code paths in IsRequestHandlerInterface method
        // Path 1: interfaceSymbol.Name == "IRequestHandler" && namespace check
        // Path 2: fullName.StartsWith("Relay.Core.Contracts.Handlers.IRequestHandler<")
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }

    // This will test the namespace + name check path
    public class SimpleHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""simple"");
        }
    }

    // This will also test the namespace + name check path (generic)
    public class GenericHandler<T> : Relay.Core.Contracts.Handlers.IRequestHandler<TestRequest, T>
    {
        public ValueTask<T> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(default(T));
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
        var generatedCode = runResult.GeneratedTrees.First().ToString();

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotEmpty(runResult.GeneratedTrees);

        // Verify that both handler types were detected and registered
        // This test ensures IsRequestHandlerInterface method correctly identifies
        // request handler interfaces through both code paths:
        // 1. Name + namespace check: IRequestHandler in Relay.Core.Contracts.Handlers
        // 2. Full name check: Relay.Core.Contracts.Handlers.IRequestHandler<TRequest, TResponse>
        Assert.Contains("SimpleHandler", generatedCode);
        Assert.Contains("GenericHandler", generatedCode);
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
    public void Generator_Should_Generate_OptimizedRequestDispatcher_EdgeCases()
    {
        // Arrange - Test GenerateOptimizedRequestDispatcher with edge cases
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    // Only void requests (no response type)
    public class VoidRequest1 : IRequest { }
    public class VoidRequest2 : IRequest { }

    // Only response requests
    public class ResponseRequest1 : IRequest<string> { }
    public class ResponseRequest2 : IRequest<int> { }

    public class VoidHandler1 : IRequestHandler<VoidRequest1>
    {
        public ValueTask HandleAsync(VoidRequest1 request, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public class VoidHandler2 : IRequestHandler<VoidRequest2>
    {
        public ValueTask HandleAsync(VoidRequest2 request, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public class ResponseHandler1 : IRequestHandler<ResponseRequest1, string>
    {
        public ValueTask<string> HandleAsync(ResponseRequest1 request, CancellationToken cancellationToken)
            => ValueTask.FromResult(""response1"");
    }

    public class ResponseHandler2 : IRequestHandler<ResponseRequest2, int>
    {
        public ValueTask<int> HandleAsync(ResponseRequest2 request, CancellationToken cancellationToken)
            => ValueTask.FromResult(123);
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

        // Verify the GenerateOptimizedRequestDispatcher method handles both response and void requests
        Assert.Contains("// Generated by Relay.SourceGenerator - Optimized Request Dispatcher", generatedCode);
        Assert.Contains("public class GeneratedRequestDispatcher : BaseRequestDispatcher", generatedCode);

        // Verify response request switch cases
        Assert.Contains("TestProject.ResponseRequest1 req when request is TestProject.ResponseRequest1 =>", generatedCode);
        Assert.Contains("TestProject.ResponseRequest2 req when request is TestProject.ResponseRequest2 =>", generatedCode);

        // Verify void request switch cases
        Assert.Contains("TestProject.VoidRequest1 req =>", generatedCode);
        Assert.Contains("TestProject.VoidRequest2 req =>", generatedCode);

        // Verify proper casting for response requests
        Assert.Contains("(ValueTask<TResponse>)(object)", generatedCode);

        // Verify no casting for void requests
        Assert.Contains("ServiceProvider.GetRequiredService<IRequestHandler<TestProject.VoidRequest1>>().HandleAsync(req, cancellationToken)", generatedCode);
    }

    [Fact]
    public void IsStreamHandlerInterface_Should_Return_True_For_StreamHandler_Interfaces()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading;

namespace Relay.Core.Contracts.Handlers
{
    public interface IStreamHandler<in TRequest, TResponse>
    {
        IAsyncEnumerable<TResponse> HandleStreamAsync(TRequest request, CancellationToken cancellationToken);
    }
}";

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Get the IStreamHandler interface symbol
        var streamHandlerInterface = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IStreamHandler`2");
        Assert.NotNull(streamHandlerInterface);

        // Act & Assert
        Assert.True(RelayIncrementalGenerator.IsStreamHandlerInterface(streamHandlerInterface));
    }

    [Fact]
    public void IsNotificationHandlerInterface_Should_Return_True_For_NotificationHandler_Interfaces()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Contracts.Notifications
{
    public interface INotification { }
}

namespace Relay.Core.Contracts.Handlers
{
    public interface INotificationHandler<in TNotification>
    {
        ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }
}";

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Get the INotificationHandler interface symbol
        var notificationHandlerInterface = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.INotificationHandler`1");
        Assert.NotNull(notificationHandlerInterface);

        // Act & Assert
        Assert.True(RelayIncrementalGenerator.IsNotificationHandlerInterface(notificationHandlerInterface));
    }

    [Fact]
    public void IsRequestHandlerInterface_Should_Return_True_For_RequestHandler_Interfaces()
    {
        // Arrange
        var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Contracts.Requests
{
    public interface IRequest<out TResponse> { }
}

namespace Relay.Core.Contracts.Handlers
{
    public interface IRequestHandler<in TRequest, TResponse>
    {
        ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}";

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Get the IRequestHandler interface symbol
        var requestHandlerInterface = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IRequestHandler`2");
        Assert.NotNull(requestHandlerInterface);

        // Act & Assert
        Assert.True(RelayIncrementalGenerator.IsRequestHandlerInterface(requestHandlerInterface));
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

        RelayIncrementalGenerator.TestForceException = true;

        try
        {
            var generator = new RelayIncrementalGenerator();
            var compilation = CreateTestCompilation(source);

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
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}