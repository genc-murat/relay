using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Relay.SourceGenerator.Tests;

public class HandlerDiscoveryTests
{
    [Fact]
    public void DiscoverHandlers_Should_Find_Valid_Request_Handler()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public Task<string> HandleTest(string request)
        {
            return Task.FromResult(request);
        }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Single(result.Handlers);
        var handler = result.Handlers.First();
        Assert.Equal("HandleTest", handler.MethodSymbol.Name);
        Assert.Single(handler.Attributes);
        Assert.Equal(RelayAttributeType.Handle, handler.Attributes.First().Type);
    }

    [Fact]
    public void DiscoverHandlers_Should_Find_Valid_Notification_Handler()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Notification]
        public Task HandleNotification(string notification)
        {
            return Task.CompletedTask;
        }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Single(result.Handlers);
        var handler = result.Handlers.First();
        Assert.Equal("HandleNotification", handler.MethodSymbol.Name);
        Assert.Single(handler.Attributes);
        Assert.Equal(RelayAttributeType.Notification, handler.Attributes.First().Type);
    }

    [Fact]
    public void DiscoverHandlers_Should_Find_Valid_Pipeline_Handler()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Pipeline]
        public Task<T> HandlePipeline<T>(T request, Func<Task<T>> next)
        {
            return next();
        }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Single(result.Handlers);
        var handler = result.Handlers.First();
        Assert.Equal("HandlePipeline", handler.MethodSymbol.Name);
        Assert.Single(handler.Attributes);
        Assert.Equal(RelayAttributeType.Pipeline, handler.Attributes.First().Type);
    }

    [Fact]
    public void DiscoverHandlers_Should_Find_Valid_Endpoint_Handler()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [ExposeAsEndpoint]
        public Task<string> HandleEndpoint(string request)
        {
            return Task.FromResult(request);
        }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Single(result.Handlers);
        var handler = result.Handlers.First();
        Assert.Equal("HandleEndpoint", handler.MethodSymbol.Name);
        Assert.Single(handler.Attributes);
        Assert.Equal(RelayAttributeType.ExposeAsEndpoint, handler.Attributes.First().Type);
    }

    [Fact]
    public void DiscoverHandlers_Should_Reject_Request_Handler_With_No_Parameters()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest()
        {
            return ""test"";
        }
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Empty(result.Handlers);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("exactly one parameter"));
    }

    [Fact]
    public void DiscoverHandlers_Should_Reject_Request_Handler_With_Multiple_Parameters()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest(string request, int extra)
        {
            return request;
        }
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Empty(result.Handlers);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("exactly one parameter"));
    }

    [Fact]
    public void DiscoverHandlers_Should_Reject_Notification_Handler_With_Non_Void_Return()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Notification]
        public string HandleNotification(string notification)
        {
            return ""result"";
        }
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Empty(result.Handlers);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("Task, ValueTask, or void"));
    }

    [Fact]
    public void DiscoverHandlers_Should_Accept_Notification_Handler_With_Void_Return()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Notification]
        public void HandleNotification(string notification)
        {
        }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Single(result.Handlers);
    }

    [Fact]
    public void DiscoverHandlers_Should_Accept_ValueTask_Return_Types()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public ValueTask<string> HandleRequest(string request)
        {
            return ValueTask.FromResult(request);
        }

        [Notification]
        public ValueTask HandleNotification(string notification)
        {
            return ValueTask.CompletedTask;
        }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Equal(2, result.Handlers.Count());
    }

    [Fact]
    public void DiscoverHandlers_Should_Accept_Streaming_Handler_With_IAsyncEnumerable()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Collections.Generic;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public IAsyncEnumerable<string> HandleStream(string request)
        {
            return GetStringsAsync();
        }

        private async IAsyncEnumerable<string> GetStringsAsync()
        {
            yield return ""test"";
        }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Single(result.Handlers);
    }

    [Fact]
    public void DiscoverHandlers_Should_Reject_Private_Handler()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        private string HandleTest(string request)
        {
            return request;
        }
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Empty(result.Handlers);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_106");
    }

    [Fact]
    public void DiscoverHandlers_Should_Accept_Internal_Handler()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        internal string HandleTest(string request)
        {
            return request;
        }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Single(result.Handlers);
    }

    [Fact]
    public void DiscoverHandlers_Should_Reject_Pipeline_Handler_With_One_Parameter()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Pipeline]
        public string HandlePipeline(string request)
        {
            return request;
        }
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Empty(result.Handlers);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("at least two parameters"));
    }

    [Fact]
    public void DiscoverHandlers_Should_Find_Multiple_Handlers_In_Same_Class()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleString(string request) => request;

        [Handle]
        public int HandleInt(int request) => request;

        [Notification]
        public void HandleNotification(string notification) { }
    }
}";

        // Act
        var result = RunHandlerDiscovery(source);

        // Assert
        Assert.Equal(3, result.Handlers.Count());
    }

    [Fact]
    public void DiscoverHandlers_Should_Report_Duplicate_Handlers_For_Same_Request_Type()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleString1(string request) => request;

        [Handle]
        public string HandleString2(string request) => request;
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Equal(2, result.Handlers.Count()); // Both handlers are discovered but marked as duplicates
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_005" && d.GetMessage(null).Contains("Multiple handlers"));
    }

    private HandlerDiscoveryResult RunHandlerDiscovery(string source)
    {
        var (result, _) = RunHandlerDiscoveryWithDiagnostics(source);
        return result;
    }

    [Fact]
    public void DiscoverHandlers_Should_Warn_For_Constructor_With_Value_Type_Parameters()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        public TestHandler(int value) { }

        [Handle]
        public string HandleTest(string request)
        {
            return request;
        }
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Single(result.Handlers);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_109");
    }

    [Fact]
    public void HandlerDiscoveryEngine_ClearCaches_ClearsResponseTypeCache()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public Task<string> HandleTest(string request)
        {
            return Task.FromResult(request);
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        var engine = new HandlerDiscoveryEngine(context);

        // Parse and collect candidate methods
        var syntaxTree = compilation.SyntaxTrees.First();
        var receiver = new RelaySyntaxReceiver();
        foreach (var node in syntaxTree.GetRoot().DescendantNodes())
        {
            receiver.OnVisitSyntaxNode(node);
        }

        var mockReporter = new MockDiagnosticReporter(new List<Diagnostic>());

        // Act: Run discovery to populate cache
        var result1 = engine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);

        // Clear caches
        engine.ClearCaches();

        // Run discovery again - should work without issues
        var result2 = engine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);

        // Assert
        Assert.Single(result1.Handlers);
        Assert.Single(result2.Handlers);
        Assert.Equal(result1.Handlers.First().MethodSymbol.Name, result2.Handlers.First().MethodSymbol.Name);
    }

    [Fact]
    public void HandlerDiscoveryEngine_Should_Handle_Named_Handlers_Correctly()
    {
        // Arrange - Test GetHandlerName method with named attributes
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle(Name = ""CustomHandler"")]
        public string HandleTest1(string request) => request;

        [Handle] // Default name
        public string HandleTest2(string request) => request;

        [Handle(Name = """")] // Empty name should use default
        public string HandleTest3(string request) => request;
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Equal(3, result.Handlers.Count);

        // Verify handler names are extracted correctly
        var handler1 = result.Handlers.First(h => h.MethodSymbol.Name == "HandleTest1");
        var handler2 = result.Handlers.First(h => h.MethodSymbol.Name == "HandleTest2");
        var handler3 = result.Handlers.First(h => h.MethodSymbol.Name == "HandleTest3");

        // Note: We can't directly test GetHandlerName since it's private,
        // but we can verify the behavior through duplicate validation
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_005"); // NamedHandlerConflict for duplicate default names
    }

    [Fact]
    public void HandlerDiscoveryEngine_Should_Validate_Multiple_Constructors()
    {
        // Arrange - Test ValidateConstructor method
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        public TestHandler() { }
        public TestHandler(int value) { }

        [Handle]
        public string HandleTest(string request) => request;
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Single(result.Handlers);
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_108"); // MultipleConstructors
    }

    [Fact]
    public void HandlerDiscoveryEngine_Should_Reject_Method_Without_Parameters()
    {
        // Arrange - Test validation of methods without parameters
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest() => ""no params"";
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Empty(result.Handlers); // Handler is rejected due to invalid signature
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_002"); // InvalidHandlerSignature
    }

    [Fact]
    public void HandlerDiscoveryEngine_Should_Handle_Parallel_Processing_Errors()
    {
        // Arrange - Create a scenario that might cause parallel processing errors
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler1
    {
        [Handle]
        public Task<string> HandleTest1(string request) => Task.FromResult(request);
    }

    public class TestHandler2
    {
        [Handle]
        public Task<string> HandleTest2(string request) => Task.FromResult(request);
    }

    public class TestHandler3
    {
        [Handle]
        public Task<string> HandleTest3(string request) => Task.FromResult(request);
    }
}";

        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        var engine = new HandlerDiscoveryEngine(context, 4); // Force parallel processing

        // Parse and collect candidate methods
        var syntaxTree = compilation.SyntaxTrees.First();
        var receiver = new RelaySyntaxReceiver();
        foreach (var node in syntaxTree.GetRoot().DescendantNodes())
        {
            receiver.OnVisitSyntaxNode(node);
        }

        var diagnostics = new List<Diagnostic>();
        var mockReporter = new MockDiagnosticReporter(diagnostics);

        // Act: Run discovery with parallel processing
        var result = engine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);

        // Assert
        Assert.Equal(3, result.Handlers.Count);
        // Should complete without exceptions in parallel processing
    }

    private (HandlerDiscoveryResult result, Diagnostic[] diagnostics) RunHandlerDiscoveryWithDiagnostics(string source)
    {
        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);

        // Parse and collect candidate methods
        var syntaxTree = compilation.SyntaxTrees.First();
        var receiver = new RelaySyntaxReceiver();
        foreach (var node in syntaxTree.GetRoot().DescendantNodes())
        {
            receiver.OnVisitSyntaxNode(node);
        }

        // Create a mock diagnostic reporter to collect diagnostics
        var diagnostics = new List<Diagnostic>();
        var mockReporter = new MockDiagnosticReporter(diagnostics);

        // Run discovery
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var result = discoveryEngine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);

        return (result, diagnostics.ToArray());
    }

    private static CSharpCompilation CreateTestCompilation(string source)
    {
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Relay.Core.IRelay).Assembly.Location),
        };

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private class MockDiagnosticReporter : IDiagnosticReporter
    {
        private readonly List<Diagnostic> _diagnostics;

        public MockDiagnosticReporter(List<Diagnostic> diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }
    }
}