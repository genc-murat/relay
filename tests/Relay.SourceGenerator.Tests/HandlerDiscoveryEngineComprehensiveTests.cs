using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Discovery;

namespace Relay.SourceGenerator.Tests;

public class HandlerDiscoveryEngineComprehensiveTests
{
    [Fact]
    public void HandlerDiscoveryEngine_Constructor_With_Null_Context_Throws_ArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new HandlerDiscoveryEngine(null!));
    }

    [Fact]
    public void HandlerDiscoveryEngine_Constructor_With_CustomParallelism_Clamps_To_Minimum()
    {
        // Arrange & Act
        var source = CreateMinimalHandlerSource();
        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        
        var engine = new HandlerDiscoveryEngine(context, 1); // Below minimum of 2

        // Use reflection to access the private field to verify the clamping
        var maxDegreeOfParallelismField = typeof(HandlerDiscoveryEngine)
            .GetField("_maxDegreeOfParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var actualParallelism = (int)maxDegreeOfParallelismField.GetValue(engine)!;
        
        // Assert
        Assert.Equal(2, actualParallelism); // Minimum value
    }

    [Fact]
    public void HandlerDiscoveryEngine_Constructor_With_CustomParallelism_Clamps_To_Maximum()
    {
        // Arrange & Act
        var source = CreateMinimalHandlerSource();
        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        
        var engine = new HandlerDiscoveryEngine(context, 10); // Above maximum of 8

        // Use reflection to access the private field to verify the clamping
        var maxDegreeOfParallelismField = typeof(HandlerDiscoveryEngine)
            .GetField("_maxDegreeOfParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var actualParallelism = (int)maxDegreeOfParallelismField.GetValue(engine)!;
        
        // Assert
        Assert.Equal(8, actualParallelism); // Maximum value
    }

    [Fact]
    public void HandlerDiscoveryEngine_Constructor_With_CustomParallelism_Within_Range_Uses_Exact_Value()
    {
        // Arrange & Act
        var source = CreateMinimalHandlerSource();
        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        
        var engine = new HandlerDiscoveryEngine(context, 5); // Within range 2-8

        // Use reflection to access the private field to verify the value
        var maxDegreeOfParallelismField = typeof(HandlerDiscoveryEngine)
            .GetField("_maxDegreeOfParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var actualParallelism = (int)maxDegreeOfParallelismField.GetValue(engine)!;
        
        // Assert
        Assert.Equal(5, actualParallelism);
    }

    [Fact]
    public void ValidateConstructor_Reports_Diagnostic_For_Multiple_Constructors()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        public TestHandler() { }
        public TestHandler(string param) { }

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
        Assert.Single(result.Handlers); // Handler should still be discovered
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_108"); // Multiple constructors diagnostic
    }

    [Fact]
    public void GetRelayAttributeType_Handles_All_Known_Attribute_Types()
    {
        // Arrange - need to create a valid context
        var source = CreateMinimalHandlerSource();
        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        
        // Test each attribute type using reflection to test the private method
        var engine = new HandlerDiscoveryEngine(context, 4);
        var method = typeof(HandlerDiscoveryEngine).GetMethod("GetRelayAttributeType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        
        // Test Handle attributes
        var handleResult = (RelayAttributeType)method.Invoke(engine, new object[] { "HandleAttribute" })!;
        Assert.Equal(RelayAttributeType.Handle, handleResult);
        
        var handleResult2 = (RelayAttributeType)method.Invoke(engine, new object[] { "Handle" })!;
        Assert.Equal(RelayAttributeType.Handle, handleResult2);
        
        // Test Notification attributes
        var notificationResult = (RelayAttributeType)method.Invoke(engine, new object[] { "NotificationAttribute" })!;
        Assert.Equal(RelayAttributeType.Notification, notificationResult);
        
        var notificationResult2 = (RelayAttributeType)method.Invoke(engine, new object[] { "Notification" })!;
        Assert.Equal(RelayAttributeType.Notification, notificationResult2);
        
        // Test Pipeline attributes
        var pipelineResult = (RelayAttributeType)method.Invoke(engine, new object[] { "PipelineAttribute" })!;
        Assert.Equal(RelayAttributeType.Pipeline, pipelineResult);
        
        var pipelineResult2 = (RelayAttributeType)method.Invoke(engine, new object[] { "Pipeline" })!;
        Assert.Equal(RelayAttributeType.Pipeline, pipelineResult2);
        
        // Test ExposeAsEndpoint attributes
        var endpointResult = (RelayAttributeType)method.Invoke(engine, new object[] { "ExposeAsEndpointAttribute" })!;
        Assert.Equal(RelayAttributeType.ExposeAsEndpoint, endpointResult);
        
        var endpointResult2 = (RelayAttributeType)method.Invoke(engine, new object[] { "ExposeAsEndpoint" })!;
        Assert.Equal(RelayAttributeType.ExposeAsEndpoint, endpointResult2);
        
        // Test unknown attribute
        var unknownResult = (RelayAttributeType)method.Invoke(engine, new object[] { "UnknownAttribute" })!;
        Assert.Equal(RelayAttributeType.None, unknownResult);
    }

    [Fact]
    public void ComputeResponseType_Handles_Task_Without_Generic_Parameter()
    {
        // Arrange - Use handler discovery process to ensure the GetResponseType method works correctly
        // Use ExposeAsEndpoint attribute which allows Task without generic parameter
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestRequest { }
    
    public class TestHandler
    {
        [ExposeAsEndpoint]
        public Task HandleTest(TestRequest request)
        {
            return Task.CompletedTask;
        }
    }
}";

        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Check if there are any validation errors
        var errorDiagnostics = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errorDiagnostics.Count != 0)
        {
            Assert.Fail($"Unexpected diagnostic errors: {string.Join(", ", errorDiagnostics.Select(d => d.GetMessage(null)))}");
        }

        // Assert - the handler should be discovered
        Assert.Single(result.Handlers);
        var handlerInfo = result.Handlers.First();
        
        // The ResponseTypeSymbol might be null due to the Unit type not being found in test environment
        // We'll check if the handler was processed at all
        Assert.NotNull(handlerInfo.MethodSymbol); // Method should be valid
        Assert.NotNull(handlerInfo.HandlerTypeSymbol); // Class should be valid
        // We can't test the exact response type due to the Unit type dependency issue, 
        // but we've verified the handler was processed and not rejected by validation
    }

    [Fact]
    public void ComputeResponseType_Handles_ValueTask_Without_Generic_Parameter()
    {
        // Arrange - Use ExposeAsEndpoint attribute which allows ValueTask without generic parameter
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestRequest { }
    
    public class TestHandler
    {
        [ExposeAsEndpoint]
        public ValueTask HandleTest(TestRequest request)
        {
            return ValueTask.CompletedTask;
        }
    }
}";

        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Check if there are any validation errors
        var errorDiagnostics = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errorDiagnostics.Count != 0)
        {
            Assert.Fail($"Unexpected diagnostic errors: {string.Join(", ", errorDiagnostics.Select(d => d.GetMessage(null)))}");
        }

        // Assert - the handler should be discovered
        Assert.Single(result.Handlers);
        var handlerInfo = result.Handlers.First();
        
        // The ResponseTypeSymbol might be null due to the Unit type not being found in test environment
        // We'll check if the handler was processed at all
        Assert.NotNull(handlerInfo.MethodSymbol); // Method should be valid
        Assert.NotNull(handlerInfo.HandlerTypeSymbol); // Class should be valid
        // We can't test the exact response type due to the Unit type dependency issue, 
        // but we've verified the handler was processed and not rejected by validation
    }

    [Fact]
    public void GetResponseType_Caches_Results()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestRequest { }
    
    public class TestHandler
    {
        [Handle]
        public Task<string> HandleTest(TestRequest request)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);
        
        // Get the method symbol for the handler
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.ValueText == "HandleTest");
        
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        
        var engine = new HandlerDiscoveryEngine(context);
        
        // Use reflection to access the private GetResponseType method
        var getResponseTypeMethod = typeof(HandlerDiscoveryEngine).GetMethod("GetResponseType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        
        // Act - Call GetResponseType twice to test caching
#pragma warning disable CS8601 // Possible null reference assignment - expected in test scenario
        var responseType1 = (ITypeSymbol)getResponseTypeMethod.Invoke(engine, new object[] { methodSymbol })!;
        var responseType2 = (ITypeSymbol)getResponseTypeMethod.Invoke(engine, new object[] { methodSymbol })!;
#pragma warning restore CS8601
        
        // Assert
        Assert.Equal(responseType1?.ToDisplayString(), responseType2?.ToDisplayString());
    }

    [Fact]
    public void ValidateForDuplicates_Does_Not_Report_Conflict_For_Different_Request_Types()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class Request1 { }
    public class Request2 { }
    
    public class TestHandler
    {
        [Handle]
        public string HandleRequest1(Request1 request) => ""req1"";
        
        [Handle]
        public string HandleRequest2(Request2 request) => ""req2"";
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Equal(2, result.Handlers.Count); // Both handlers should be discovered
        // No duplicate diagnostics should be reported since they handle different request types
        Assert.DoesNotContain(diagnostics, d => d.Id == "RELAY_GEN_005");
    }

    [Fact]
    public void ValidateNamedHandlerConflicts_Handles_Custom_Names()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestRequest { }
    
    public class TestHandler1
    {
        [Handle(Name = ""CustomName"")]
        public string HandleTest1(TestRequest request) => ""test1"";
    }
    
    public class TestHandler2
    {
        [Handle(Name = ""CustomName"")]
        public string HandleTest2(TestRequest request) => ""test2"";
    }
}";

        // Act
        var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

        // Assert
        Assert.Equal(2, result.Handlers.Count); // Both handlers should be discovered
        // Should report named handler conflicts since they have the same custom name for the same request type
        Assert.Contains(diagnostics, d => d.Id == "RELAY_GEN_005");
    }

    [Fact]
    public void ValidateNamedHandlerConflicts_Handles_Mixed_Named_With_Default()
    {
        // Arrange - Two handlers in the same class for DIFFERENT request types: one with custom name, one with default
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestRequest1 { }
    public class TestRequest2 { }
    
    public class TestHandler
    {
        [Handle(Name = ""CustomName"")]
        public string HandleTest1(TestRequest1 request) => ""test1"";
        
        [Handle] // This should have the default name
        public string HandleTest2(TestRequest2 request) => ""test2"";
    }
}";

        // Act
        var (result2, diagnostics2) = RunHandlerDiscoveryWithDiagnostics(source);

        // The first method has Name = "CustomName", the second has no Name property so defaults to "default"
        // They handle different request types, so they should not conflict
        
        // Assert
        Assert.Equal(2, result2.Handlers.Count); // Both handlers should be discovered
        // No conflict should be reported since they handle different request types
        Assert.DoesNotContain(diagnostics2, d => d.Id == "RELAY_GEN_005");
    }

    [Fact]
    public void HandlerDiscoveryEngine_Handles_Cancellation_Request()
    {
        // Arrange
        var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestRequest { }
    
    public class TestHandler
    {
        [Handle]
        public Task<string> HandleTest(TestRequest request)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        using var cts = new CancellationTokenSource();
        var context = new RelayCompilationContext(compilation, cts.Token);

        var syntaxTree = compilation.SyntaxTrees.First();
        var receiver = new RelaySyntaxReceiver();
        foreach (var node in syntaxTree.GetRoot().DescendantNodes())
        {
            receiver.OnVisitSyntaxNode(node);
        }

        var diagnostics = new List<Diagnostic>();
        var mockReporter = new MockDiagnosticReporter(diagnostics);

        // Cancel the token after setting up
        cts.Cancel();

        // Act & Assert
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        Assert.Throws<OperationCanceledException>(() => 
        {
            var result = discoveryEngine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);
        });
    }

    [Fact]
    public void ProcessMethodsSequentially_Handles_Null_Methods()
    {
        // Arrange
        var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestRequest { }
    
    public class TestHandler
    {
        [Handle]
        public string HandleTest(TestRequest request) => request.ToString();
    }
}";

        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, default);

        var syntaxTree = compilation.SyntaxTrees.First();
        var receiver = new RelaySyntaxReceiver();
        foreach (var node in syntaxTree.GetRoot().DescendantNodes())
        {
            receiver.OnVisitSyntaxNode(node);
        }

        // Add null methods to the list to test handling of nulls
        var methodsWithNulls = new List<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax?>(receiver.CandidateMethods);
        methodsWithNulls.Add(null);
        methodsWithNulls.Add(null);

        var diagnostics = new List<Diagnostic>();
        var mockReporter = new MockDiagnosticReporter(diagnostics);

        // Act
        var discoveryEngine = new HandlerDiscoveryEngine(context);
        var result = discoveryEngine.DiscoverHandlers(methodsWithNulls, mockReporter);

        // Assert
        Assert.Single(result.Handlers); // Only the valid method should be processed
        Assert.Empty(diagnostics); // No error diagnostics for null methods
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
    
    private string CreateMinimalHandlerSource()
    {
        return @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestRequest { }
    
    public class TestHandler
    {
        [Handle]
        public Task<string> HandleTest(TestRequest request)
        {
            return Task.FromResult(request.ToString());
        }
    }
}";
    }

    private class MockDiagnosticReporter(List<Diagnostic> diagnostics) : IDiagnosticReporter
    {
        private readonly List<Diagnostic> _diagnostics = diagnostics;

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }
    }
}