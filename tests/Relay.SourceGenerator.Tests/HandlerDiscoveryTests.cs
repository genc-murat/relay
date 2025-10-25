using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Reflection;

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
    public void DiscoverHandlers_Should_Reject_Endpoint_Handler_With_No_Parameters()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [ExposeAsEndpoint]
        public string HandleEndpoint()
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

        // Verify we have candidates
        Assert.True(receiver.CandidateCount > 0);

        var diagnostics = new List<Diagnostic>();
        var mockReporter = new MockDiagnosticReporter(diagnostics);

        // Run discovery
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var result = discoveryEngine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);

        // Clear the receiver after use
        receiver.Clear();
        Assert.Equal(0, receiver.CandidateCount);
        Assert.Empty(receiver.CandidateMethods);
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

    [Fact]
    public void ProcessMethodsSequentially_Catches_Exception_In_AnalyzeHandlerMethod()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest(string request)
        {
            return request;
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var throwingContext = new ThrowingRelayCompilationContext(compilation, default);

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

        // Run discovery with throwing context
        var discoveryEngine = new HandlerDiscoveryEngine(throwingContext);
        var result = discoveryEngine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);

        // Assert
        Assert.Empty(result.Handlers); // No handlers discovered due to exception
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_001" && d.GetMessage(null).Contains("Error analyzing handler method"));
    }

    [Fact]
    public void ProcessMethodsInParallel_Catches_Exception_In_AnalyzeHandlerMethod()
    {
        // Arrange - Create source with more than 10 methods to trigger parallel processing
        var methods = string.Join("\n", Enumerable.Range(1, 15).Select(i => $@"
        [Handle]
        public string HandleTest{i}(string request{i})
        {{
            return request{i};
        }}"));
        var source = $@"
using Relay.Core;

namespace TestProject
{{
    public class TestHandler
    {{
        {methods}
    }}
}}";

        var compilation = CreateTestCompilation(source);
        var throwingContext = new ThrowingRelayCompilationContext(compilation, default);

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

        // Run discovery with throwing context
        var discoveryEngine = new HandlerDiscoveryEngine(throwingContext);
        var result = discoveryEngine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);

        // Assert
        Assert.Empty(result.Handlers); // No handlers discovered due to exception
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_001" && d.GetMessage(null).Contains("Error analyzing handler method"));
    }

    [Fact]
    public void ProcessMethodsInParallel_Handles_Null_Methods()
    {
        // Arrange - Create source with methods of different types to avoid duplicates
        var types = new[] { "string", "int", "bool", "double", "char", "long", "short", "byte", "float", "decimal", "DateTime", "Guid" };
        var methods = string.Join("\n", types.Select((type, i) => $@"
        [Handle]
        public string HandleTest{i}({type} request{i})
        {{
            return request{i}.ToString();
        }}"));
        var source = $@"
using Relay.Core;
using System;

namespace TestProject
{{
    public class TestHandler
    {{
        {methods}
    }}
}}";

        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);

        // Parse and collect candidate methods
        var syntaxTree = compilation.SyntaxTrees.First();
        var receiver = new RelaySyntaxReceiver();
        foreach (var node in syntaxTree.GetRoot().DescendantNodes())
        {
            receiver.OnVisitSyntaxNode(node);
        }

        // Add some null methods to the list
        var methodsWithNulls = new List<MethodDeclarationSyntax?>(receiver.CandidateMethods);
        methodsWithNulls.Add(null);
        methodsWithNulls.Add(null);

        // Create a mock diagnostic reporter to collect diagnostics
        var diagnostics = new List<Diagnostic>();
        var mockReporter = new MockDiagnosticReporter(diagnostics);

        // Run discovery
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var result = discoveryEngine.DiscoverHandlers(methodsWithNulls, mockReporter);

        // Assert
        Assert.Equal(12, result.Handlers.Count()); // All valid methods discovered, nulls ignored
        Assert.Empty(diagnostics); // No diagnostics for null methods
    }

    [Fact]
    public void AnalyzeHandlerMethod_Returns_Null_For_Method_Without_Relay_Attributes()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        public string HandleTest(string request)
        {
            return request;
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);

        // Parse and find the method without attributes
        var syntaxTree = compilation.SyntaxTrees.First();
        var method = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        var diagnostics = new List<Diagnostic>();
        var mockReporter = new MockDiagnosticReporter(diagnostics);

        // Use reflection to call the private method
        var engine = new HandlerDiscoveryEngine(context);
        var analyzeMethod = typeof(HandlerDiscoveryEngine).GetMethod("AnalyzeHandlerMethod", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (HandlerInfo?)analyzeMethod.Invoke(engine, new object[] { method, mockReporter });

        // Assert
        Assert.Null(result); // No handler info for method without attributes
        Assert.Empty(diagnostics); // No diagnostics reported
    }



    private class ThrowingRelayCompilationContext : RelayCompilationContext
    {
        public ThrowingRelayCompilationContext(Compilation compilation, CancellationToken cancellationToken)
            : base(compilation, cancellationToken)
        {
        }

        public override SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
        {
            throw new InvalidOperationException("Simulated exception in GetSemanticModel");
        }
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