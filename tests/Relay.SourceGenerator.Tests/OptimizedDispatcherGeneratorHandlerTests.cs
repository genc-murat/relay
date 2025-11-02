using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Relay.SourceGenerator;
using Xunit;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

public class OptimizedDispatcherGeneratorHandlerTests
{
    private RelayCompilationContext CreateTestContext()
    {
        var code = @"
namespace Test
{
    public class TestRequest : Relay.Core.IRequest<string> { }
    public class TestHandler
    {
        [Relay.Core.Attributes.Handle]
        public async System.Threading.Tasks.ValueTask<string> HandleAsync(TestRequest request, System.Threading.CancellationToken cancellationToken)
        {
            return ""test"";
        }
    }
}";

        var compilation = CreateCompilation(code);
        return new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        // Include the attribute definitions
        var attributeDefinitions = @"
namespace Relay.Core.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var attributeTree = CSharpSyntaxTree.ParseText(attributeDefinitions);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree, attributeTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private HandlerInfo CreateMockHandler(string requestType, string responseType, string handlerType = "TestHandler", string methodName = "HandleAsync", bool isStatic = false, string? handlerName = null, int priority = 0)
    {
        // Build attribute syntax for the handle attribute
        var attributeParts = new List<string>();
        if (handlerName != null)
        {
            attributeParts.Add($"Name = \"{handlerName}\"");
        }
        if (priority != 0)
        {
            attributeParts.Add($"Priority = {priority}");
        }

        var attributeArgs = attributeParts.Count > 0 ? $"({string.Join(", ", attributeParts)})" : "";

        var compilation = CreateCompilation($@"
using Relay.Core.Attributes;

namespace Test {{
    public class {requestType} : Relay.Core.IRequest<{responseType}> {{ }}
    public class {handlerType} {{
        [Handle{attributeArgs}]
        {(isStatic ? "public static" : "public")} async System.Threading.Tasks.ValueTask<{responseType}> {methodName}({requestType} request, System.Threading.CancellationToken cancellationToken) => default;
    }}
}}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{requestType}");
        var responseTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{responseType}");
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{handlerType}");

        var methodSymbol = handlerTypeSymbol?.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();

        // Get the actual AttributeData from the method symbol
        var attributeData = methodSymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "HandleAttribute");

        HandlerInfo handler = new()
        {
            MethodSymbol = methodSymbol,
            HandlerTypeSymbol = handlerTypeSymbol,
            RequestTypeSymbol = requestTypeSymbol,
            ResponseTypeSymbol = responseTypeSymbol,
            Attributes =
            [
                new() {
                    Type = RelayAttributeType.Handle,
                    AttributeData = attributeData
                }
            ]
        };

        return handler;
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithSingleHandler_ShouldGenerateSpecializedMethod()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler = CreateMockHandler("TestRequest", "string", "TestHandler", "HandleAsync", false);
        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("Dispatch_Test_TestRequest", source);
        Assert.Contains("Specialized dispatch method for Test.TestRequest", source);
        Assert.Contains("Direct invocation for single handler - maximum performance", source);
        Assert.Contains("serviceProvider.GetRequiredService<Test.TestHandler>()", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithStaticHandler_ShouldGenerateStaticInvocation()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler = CreateMockHandler("TestRequest", "string", "TestHandler", "HandleAsync", true);
        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.DoesNotContain("serviceProvider.GetRequiredService", source);
        Assert.Contains("Test.TestHandler.HandleAsync(request, cancellationToken)", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithStaticVoidHandler_ShouldGenerateStaticVoidInvocation()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler = CreateMockHandler("TestRequest", "void", "TestHandler", "HandleAsync", true);
        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.DoesNotContain("serviceProvider.GetRequiredService", source);
        Assert.Contains("await Test.TestHandler.HandleAsync(request, cancellationToken).ConfigureAwait(false);", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMultipleHandlers_ShouldGenerateSelectionLogic()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create two handlers with the same request type but different handler types
        var compilation = CreateCompilation(@"
namespace Test {
    public class TestRequest : Relay.Core.IRequest<string> { }
    public class TestHandler1 {
        public async System.Threading.Tasks.ValueTask<string> HandleAsync(TestRequest request, System.Threading.CancellationToken cancellationToken) => default;
    }
    public class TestHandler2 {
        public async System.Threading.Tasks.ValueTask<string> HandleAsync(TestRequest request, System.Threading.CancellationToken cancellationToken) => default;
    }
}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.TestRequest");

        foreach (var handlerName in new[] { "TestHandler1", "TestHandler2" })
        {
            var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{handlerName}");
            var methodSymbol = handlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();

            HandlerInfo handler = new()
            {
                MethodSymbol = methodSymbol,
                HandlerTypeSymbol = handlerTypeSymbol,
                RequestTypeSymbol = requestTypeSymbol,
                Attributes =
                [
                    new() { Type = RelayAttributeType.Handle }
                ]
            };

            discoveryResult.Handlers.Add(handler);
        }

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("Optimized handler selection with pattern matching", source);
        Assert.Contains("Branch prediction optimized: default handler first (most common case)", source);
        Assert.Contains("No handler found with name", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithVoidResponse_ShouldGenerateVoidMethod()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler = CreateMockHandler("TestRequest", "void", "TestHandler", "HandleAsync", false);
        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("public static async ValueTask Dispatch_Test_TestRequest(", source);
        Assert.DoesNotContain("ValueTask<void>", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithTypedResponse_ShouldGenerateTypedMethod()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler = CreateMockHandler("TestRequest", "string", "TestHandler", "HandleAsync", false);
        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("public static async ValueTask<string> Dispatch_Test_TestRequest(", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMultipleRequestTypes_ShouldGenerateMultipleMethods()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler1 = CreateMockHandler("Request1", "string", "Handler1", "HandleAsync", false);
        var handler2 = CreateMockHandler("Request2", "int", "Handler2", "HandleAsync", false);

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("Dispatch_Test_Request1", source);
        Assert.Contains("Dispatch_Test_Request2", source);
        Assert.Contains("ValueTask<string> Dispatch_Test_Request1(", source);
        Assert.Contains("ValueTask<int> Dispatch_Test_Request2(", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMultipleStaticHandlers_ShouldGenerateStaticSelectionLogic()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create multiple static handlers without names (all default)
        var handler1 = CreateMockHandler("TestRequest", "string", "TestHandler1", "HandleAsync", true, null);
        var handler2 = CreateMockHandler("TestRequest", "string", "TestHandler2", "HandleAsync", true, null);
        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.DoesNotContain("serviceProvider.GetRequiredService", source);
        // For multiple handlers with same name (default), it should generate if-else selection logic
        Assert.Contains("handlerName == null || handlerName == \"default\"", source);
        Assert.Contains("Test.TestHandler1.HandleAsync", source);
        Assert.Contains("Test.TestHandler2.HandleAsync", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNoHandlers_ShouldGenerateBasicStructure()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult(); // Empty

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("public static class OptimizedDispatcher", source);
        Assert.DoesNotContain("Dispatch_", source);
        Assert.DoesNotContain("DispatchStreamAsync", source);
        Assert.DoesNotContain("DispatchNotificationAsync", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMixedHandlerTypes_ShouldGenerateAllMethods()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Add regular handler
        var regularHandler = CreateMockHandler("RegularRequest", "string", "RegularHandler", "HandleAsync", false);
        discoveryResult.Handlers.Add(regularHandler);

        // Add streaming handler
        var compilation = CreateCompilation(@"
namespace Test {
    public class StreamRequest : Relay.Core.IStreamRequest<int> { }
    public class StreamHandler {
        public async System.Collections.Generic.IAsyncEnumerable<int> HandleAsync(StreamRequest request, System.Threading.CancellationToken cancellationToken) => default;
    }
}");
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var streamRequestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.StreamRequest");
        var streamHandlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.StreamHandler");
        var streamMethodSymbol = streamHandlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();

        HandlerInfo streamHandler = new()
        {
            MethodSymbol = streamMethodSymbol,
            HandlerTypeSymbol = streamHandlerTypeSymbol,
            RequestTypeSymbol = streamRequestTypeSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(streamHandler);

        // Add notification handler
        var notificationCompilation = CreateCompilation(@"
namespace Test {
    public class TestNotification : Relay.Core.INotification { }
    public class NotificationHandler {
        public async System.Threading.Tasks.ValueTask HandleAsync(TestNotification notification, System.Threading.CancellationToken cancellationToken) => default;
    }
}");
        var notificationSemanticModel = notificationCompilation.GetSemanticModel(notificationCompilation.SyntaxTrees.First());
        var notificationTypeSymbol = notificationSemanticModel.Compilation.GetTypeByMetadataName("Test.TestNotification");
        var notificationHandlerTypeSymbol = notificationSemanticModel.Compilation.GetTypeByMetadataName("Test.NotificationHandler");
        var notificationMethodSymbol = notificationHandlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();

        HandlerInfo notificationHandler = new()
        {
            MethodSymbol = notificationMethodSymbol,
            HandlerTypeSymbol = notificationHandlerTypeSymbol,
            RequestTypeSymbol = notificationTypeSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Notification }
            ]
        };
        discoveryResult.Handlers.Add(notificationHandler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("Dispatch_Test_RegularRequest", source);
        Assert.Contains("DispatchStreamAsync", source);
        Assert.Contains("DispatchNotificationAsync", source);
        Assert.Contains("IAsyncEnumerable<TResponse>", source);
        Assert.Contains("where TRequest : IStreamRequest<TResponse>", source);
        Assert.Contains("where TNotification : INotification", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNamedHandlers_ShouldGeneratePatternMatchingSwitch()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create multiple handlers with different names for the same request type
        var handler1 = CreateMockHandler("TestRequest", "string", "PrimaryHandler", "HandleAsync", false, "primary", 10);
        var handler2 = CreateMockHandler("TestRequest", "string", "SecondaryHandler", "HandleAsync", false, "secondary", 5);
        var handler3 = CreateMockHandler("TestRequest", "string", "DefaultHandler", "HandleAsync", false, "default", 0);

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);
        discoveryResult.Handlers.Add(handler3);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate pattern matching switch expression instead of if-else
        Assert.Contains("return handlerName switch", source);
        Assert.Contains("Pattern matching for O(1) handler selection", source);

        // Should contain all handler name patterns
        Assert.Contains("\"primary\"", source);
        Assert.Contains("\"secondary\"", source);
        Assert.Contains("null or \"default\"", source);

        // Should contain error handling for unknown handler names
        Assert.Contains("No handler found with name", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNamedInstanceHandlers_ShouldGenerateHelperMethods()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create instance handlers with different names
        var handler1 = CreateMockHandler("TestRequest", "string", "Handler1", "HandleAsync", false, "handler1");
        var handler2 = CreateMockHandler("TestRequest", "string", "Handler2", "HandleAsync", false, "handler2");

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate helper methods for instance handlers
        Assert.Contains("InvokeHandler_Test_Handler1_HandleAsync", source);
        Assert.Contains("InvokeHandler_Test_Handler2_HandleAsync", source);

        // Helper methods should have aggressive inlining
        Assert.Contains("[MethodImpl(MethodImplOptions.AggressiveInlining)]", source);

        // Helper methods should resolve handlers from DI
        Assert.Contains("serviceProvider.GetRequiredService<Test.Handler1>()", source);
        Assert.Contains("serviceProvider.GetRequiredService<Test.Handler2>()", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNamedStaticHandlers_ShouldGenerateDirectStaticCalls()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create static handlers with different names
        var handler1 = CreateMockHandler("TestRequest", "string", "StaticHandler1", "HandleAsync", true, "static1");
        var handler2 = CreateMockHandler("TestRequest", "string", "StaticHandler2", "HandleAsync", true, "static2");

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate pattern matching switch
        Assert.Contains("return handlerName switch", source);

        // Should call static methods directly without helper methods
        Assert.Contains("Test.StaticHandler1.HandleAsync(request, cancellationToken)", source);
        Assert.Contains("Test.StaticHandler2.HandleAsync(request, cancellationToken)", source);

        // Should NOT generate helper methods for static handlers
        Assert.DoesNotContain("InvokeHandler_Test_StaticHandler1_HandleAsync", source);
        Assert.DoesNotContain("InvokeHandler_Test_StaticHandler2_HandleAsync", source);

        // Should NOT use DI for static handlers
        var lines = source.Split('\n');
        var switchBlock = string.Join('\n', lines.SkipWhile(l => !l.Contains("return handlerName switch")).TakeWhile(l => !l.Contains("};") || l.Contains("switch")));
        Assert.DoesNotContain("serviceProvider.GetRequiredService", switchBlock);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMixedNamedAndStaticInstanceHandlers_ShouldGenerateMixedPatternMatching()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create mix of static and instance handlers with names
        var staticHandler = CreateMockHandler("TestRequest", "string", "StaticHandler", "HandleAsync", true, "static");
        var instanceHandler = CreateMockHandler("TestRequest", "string", "InstanceHandler", "HandleAsync", false, "instance");
        var defaultHandler = CreateMockHandler("TestRequest", "string", "DefaultHandler", "HandleAsync", false, null);

        discoveryResult.Handlers.Add(staticHandler);
        discoveryResult.Handlers.Add(instanceHandler);
        discoveryResult.Handlers.Add(defaultHandler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate pattern matching switch
        Assert.Contains("return handlerName switch", source);

        // Static handler should be called directly in switch
        Assert.Contains("Test.StaticHandler.HandleAsync(request, cancellationToken)", source);

        // Instance handler should use helper method in switch
        Assert.Contains("InvokeHandler_Test_InstanceHandler_HandleAsync(serviceProvider, request, cancellationToken)", source);

        // Should generate helper method only for instance handler
        Assert.Contains("private static async ValueTask<string> InvokeHandler_Test_InstanceHandler_HandleAsync", source);
        Assert.DoesNotContain("InvokeHandler_Test_StaticHandler_HandleAsync", source);

        // Default handler should also use helper method
        Assert.Contains("InvokeHandler_Test_DefaultHandler_HandleAsync", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNamedVoidHandlers_ShouldGenerateVoidHelperMethods()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create void handlers with names
        var handler1 = CreateMockHandler("TestRequest", "void", "VoidHandler1", "HandleAsync", false, "void1");
        var handler2 = CreateMockHandler("TestRequest", "void", "VoidHandler2", "HandleAsync", false, "void2");

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate void ValueTask helper methods
        Assert.Contains("private static async ValueTask InvokeHandler_Test_VoidHandler1_HandleAsync", source);
        Assert.Contains("private static async ValueTask InvokeHandler_Test_VoidHandler2_HandleAsync", source);

        // Should NOT have ValueTask<void>
        Assert.DoesNotContain("ValueTask<void>", source);

        // Helper methods should await handler calls without return
        Assert.Contains("await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithPrioritizedNamedHandlers_ShouldOrderByPriorityThenName()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create handlers with different priorities
        var lowPriority = CreateMockHandler("TestRequest", "string", "LowHandler", "HandleAsync", false, "low", 1);
        var highPriority = CreateMockHandler("TestRequest", "string", "HighHandler", "HandleAsync", false, "high", 10);
        var mediumPriority = CreateMockHandler("TestRequest", "string", "MediumHandler", "HandleAsync", false, "medium", 5);
        var defaultHandler = CreateMockHandler("TestRequest", "string", "DefaultHandler", "HandleAsync", false, "default", 0);

        discoveryResult.Handlers.Add(lowPriority);
        discoveryResult.Handlers.Add(highPriority);
        discoveryResult.Handlers.Add(mediumPriority);
        discoveryResult.Handlers.Add(defaultHandler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate switch expression
        Assert.Contains("return handlerName switch", source);

        // All handlers should be present
        Assert.Contains("\"high\"", source);
        Assert.Contains("\"medium\"", source);
        Assert.Contains("\"low\"", source);
        Assert.Contains("null or \"default\"", source);

        // Extract switch expression to verify ordering
        var switchLines = source.Split('\n')
            .SkipWhile(l => !l.Contains("return handlerName switch"))
            .Skip(1) // Skip the switch line itself
            .TakeWhile(l => !l.Contains("};"))
            .Where(l => l.Contains("=>"))
            .ToList();

        // High priority should come before medium, medium before low, default handler should be somewhere
        var highIndex = switchLines.FindIndex(l => l.Contains("\"high\""));
        var mediumIndex = switchLines.FindIndex(l => l.Contains("\"medium\""));
        var lowIndex = switchLines.FindIndex(l => l.Contains("\"low\""));
        var defaultIndex = switchLines.FindIndex(l => l.Contains("null or \"default\""));

        Assert.True(highIndex >= 0, "High priority handler should be in switch");
        Assert.True(mediumIndex >= 0, "Medium priority handler should be in switch");
        Assert.True(lowIndex >= 0, "Low priority handler should be in switch");
        Assert.True(defaultIndex >= 0, "Default handler should be in switch");

        // Higher priority should come first (lower index)
        Assert.True(highIndex < mediumIndex, "High priority should come before medium priority");
        Assert.True(mediumIndex < lowIndex, "Medium priority should come before low priority");
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMultipleUnnamedStaticVoidHandlers_ShouldGenerateIfElseWithReturn()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create multiple unnamed (default) static void handlers
        var handler1 = CreateMockHandler("TestRequest", "void", "VoidHandler1", "HandleAsync", true, null);
        var handler2 = CreateMockHandler("TestRequest", "void", "VoidHandler2", "HandleAsync", true, null);

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should use if-else pattern (not switch) because all handlers are unnamed/default
        Assert.Contains("handlerName == null || handlerName == \"default\"", source);
        Assert.DoesNotContain("return handlerName switch", source);

        // Should generate static void invocations with await and return
        Assert.Contains("await Test.VoidHandler1.HandleAsync(request, cancellationToken).ConfigureAwait(false);", source);
        Assert.Contains("return;", source);

        // Should call both handlers
        Assert.Contains("Test.VoidHandler1.HandleAsync", source);
        Assert.Contains("Test.VoidHandler2.HandleAsync", source);

        // Should NOT use DI for static handlers
        Assert.DoesNotContain("serviceProvider.GetRequiredService", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMultipleUnnamedInstanceVoidHandlers_ShouldGenerateIfElseWithDIAndReturn()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create multiple unnamed (default) instance void handlers
        var handler1 = CreateMockHandler("TestRequest", "void", "VoidHandler1", "HandleAsync", false, null);
        var handler2 = CreateMockHandler("TestRequest", "void", "VoidHandler2", "HandleAsync", false, null);

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should use if-else pattern (not switch) because all handlers are unnamed/default
        Assert.Contains("handlerName == null || handlerName == \"default\"", source);
        Assert.DoesNotContain("return handlerName switch", source);

        // Should generate instance void invocations with DI, await and return
        Assert.Contains("var handler = serviceProvider.GetRequiredService<Test.VoidHandler1>();", source);
        Assert.Contains("await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);", source);
        Assert.Contains("return;", source);

        // Should call both handlers
        Assert.Contains("GetRequiredService<Test.VoidHandler1>()", source);
        Assert.Contains("GetRequiredService<Test.VoidHandler2>()", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMixedUnnamedStaticInstanceVoidHandlers_ShouldGenerateCorrectIfElse()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create mix of unnamed static and instance void handlers
        var staticHandler = CreateMockHandler("TestRequest", "void", "StaticVoidHandler", "HandleAsync", true, null);
        var instanceHandler = CreateMockHandler("TestRequest", "void", "InstanceVoidHandler", "HandleAsync", false, null);

        discoveryResult.Handlers.Add(staticHandler);
        discoveryResult.Handlers.Add(instanceHandler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should use if-else pattern
        Assert.Contains("handlerName == null || handlerName == \"default\"", source);

        // Static handler should call directly
        Assert.Contains("await Test.StaticVoidHandler.HandleAsync(request, cancellationToken).ConfigureAwait(false);", source);

        // Instance handler should use DI
        Assert.Contains("var handler = serviceProvider.GetRequiredService<Test.InstanceVoidHandler>();", source);

        // Both should have return statement for void
        var lines = source.Split('\n');
        var voidReturnCount = lines.Count(l => l.Trim() == "return;");
        Assert.True(voidReturnCount >= 2, $"Expected at least 2 'return;' statements for void handlers, found {voidReturnCount}");
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithUnnamedStaticNonVoidHandlers_ShouldGenerateReturnWithoutSeparateReturnStatement()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create multiple unnamed static non-void handlers
        var handler1 = CreateMockHandler("TestRequest", "string", "Handler1", "HandleAsync", true, null);
        var handler2 = CreateMockHandler("TestRequest", "string", "Handler2", "HandleAsync", true, null);

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate return await (not await + return;)
        Assert.Contains("return await Test.Handler1.HandleAsync(request, cancellationToken).ConfigureAwait(false);", source);
        Assert.Contains("return await Test.Handler2.HandleAsync(request, cancellationToken).ConfigureAwait(false);", source);

        // Should NOT have standalone return; statements (only return await)
        var lines = source.Split('\n').Select(l => l.Trim()).ToList();
        var standaloneReturns = lines.Count(l => l == "return;");
        Assert.Equal(0, standaloneReturns);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithUnnamedInstanceNonVoidHandlers_ShouldGenerateReturnAwaitWithDI()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create multiple unnamed instance non-void handlers
        var handler1 = CreateMockHandler("TestRequest", "string", "Handler1", "HandleAsync", false, null);
        var handler2 = CreateMockHandler("TestRequest", "string", "Handler2", "HandleAsync", false, null);

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should use DI
        Assert.Contains("var handler = serviceProvider.GetRequiredService<Test.Handler1>();", source);
        Assert.Contains("var handler = serviceProvider.GetRequiredService<Test.Handler2>();", source);

        // Should generate return await
        Assert.Contains("return await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);", source);

        // Should NOT have standalone return; statements
        var lines = source.Split('\n').Select(l => l.Trim()).ToList();
        var standaloneReturns = lines.Count(l => l == "return;");
        Assert.Equal(0, standaloneReturns);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithHandlerHavingNullAttributeData_ShouldTreatAsDefault()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create handler with null AttributeData
        var compilation = CreateCompilation(@"
namespace Test {
    public class TestRequest : Relay.Core.IRequest<string> { }
    public class TestHandler {
        public async System.Threading.Tasks.ValueTask<string> HandleAsync(TestRequest request, System.Threading.CancellationToken cancellationToken) => default;
    }
}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.TestRequest");
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.TestHandler");
        var methodSymbol = handlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();

        var handler = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            HandlerTypeSymbol = handlerTypeSymbol,
            RequestTypeSymbol = requestTypeSymbol,
            Attributes =
            [
                new() {
                    Type = RelayAttributeType.Handle,
                    AttributeData = null // Explicitly null
                }
            ]
        };

        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should treat handler as default (no named handler logic)
        Assert.DoesNotContain("return handlerName switch", source);
        Assert.Contains("Direct invocation for single handler", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithEmptyNameAttribute_ShouldTreatAsDefault()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create handler with empty name
        var handler1 = CreateMockHandlerWithEmptyName("TestRequest", "string", "Handler1", "HandleAsync", false, "");
        var handler2 = CreateMockHandler("TestRequest", "string", "Handler2", "HandleAsync", false, "named");

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate switch with named handler
        Assert.Contains("return handlerName switch", source);
        Assert.Contains("\"named\"", source);

        // Empty name should be treated as default
        Assert.Contains("null or \"default\"", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithWhitespaceNameAttribute_ShouldTreatAsDefault()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create handler with whitespace name
        var handler1 = CreateMockHandlerWithEmptyName("TestRequest", "string", "Handler1", "HandleAsync", false, "   ");
        var handler2 = CreateMockHandler("TestRequest", "string", "Handler2", "HandleAsync", false, "named");

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate switch with named handler
        Assert.Contains("return handlerName switch", source);
        Assert.Contains("\"named\"", source);

        // Whitespace name should be treated as default
        Assert.Contains("null or \"default\"", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithHandlerMissingNameProperty_ShouldTreatAsDefault()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create handler with Priority but no Name
        var handler1 = CreateMockHandler("TestRequest", "string", "Handler1", "HandleAsync", false, null, 10);
        var handler2 = CreateMockHandler("TestRequest", "string", "Handler2", "HandleAsync", false, "named");

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate switch with named handler
        Assert.Contains("return handlerName switch", source);
        Assert.Contains("\"named\"", source);

        // Handler without Name should be treated as default
        Assert.Contains("null or \"default\"", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithHandlerMissingPriorityProperty_ShouldTreatAsZeroPriority()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create handlers:
        // 1. Handler with only Name (no Priority) - should default to priority 0
        // 2. Handler with explicit Priority = 5
        // 3. Handler with explicit Priority = 10
        var noPriorityHandler = CreateMockHandler("TestRequest", "string", "NoPriorityHandler", "HandleAsync", false, "noprio", 0);
        var mediumPriorityHandler = CreateMockHandler("TestRequest", "string", "MediumHandler", "HandleAsync", false, "medium", 5);
        var highPriorityHandler = CreateMockHandler("TestRequest", "string", "HighHandler", "HandleAsync", false, "high", 10);

        discoveryResult.Handlers.Add(noPriorityHandler);
        discoveryResult.Handlers.Add(mediumPriorityHandler);
        discoveryResult.Handlers.Add(highPriorityHandler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate switch
        Assert.Contains("return handlerName switch", source);

        // All handlers should be present
        Assert.Contains("\"noprio\"", source);
        Assert.Contains("\"medium\"", source);
        Assert.Contains("\"high\"", source);

        // Verify ordering (higher priority should come first)
        var switchLines = source.Split('\n')
            .SkipWhile(l => !l.Contains("return handlerName switch"))
            .Skip(1)
            .TakeWhile(l => !l.Contains("};"))
            .Where(l => l.Contains("=>"))
            .ToList();

        var highIndex = switchLines.FindIndex(l => l.Contains("\"high\""));
        var mediumIndex = switchLines.FindIndex(l => l.Contains("\"medium\""));
        var noprioIndex = switchLines.FindIndex(l => l.Contains("\"noprio\""));

        Assert.True(highIndex >= 0);
        Assert.True(mediumIndex >= 0);
        Assert.True(noprioIndex >= 0);

        // Higher priority should come before lower priority
        Assert.True(highIndex < mediumIndex, "High priority (10) should come before medium priority (5)");
        Assert.True(mediumIndex < noprioIndex, "Medium priority (5) should come before no priority (0)");
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNegativePriorityHandlers_ShouldOrderCorrectly()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create handlers with negative, zero, and positive priorities
        var negativePriorityHandler = CreateMockHandler("TestRequest", "string", "NegativeHandler", "HandleAsync", false, "negative", -10);
        var zeroPriorityHandler = CreateMockHandler("TestRequest", "string", "ZeroHandler", "HandleAsync", false, "zero", 0);
        var positivePriorityHandler = CreateMockHandler("TestRequest", "string", "PositiveHandler", "HandleAsync", false, "positive", 10);

        discoveryResult.Handlers.Add(negativePriorityHandler);
        discoveryResult.Handlers.Add(zeroPriorityHandler);
        discoveryResult.Handlers.Add(positivePriorityHandler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("return handlerName switch", source);

        // All handlers should be present
        Assert.Contains("\"negative\"", source);
        Assert.Contains("\"zero\"", source);
        Assert.Contains("\"positive\"", source);

        // Verify ordering
        var switchLines = source.Split('\n')
            .SkipWhile(l => !l.Contains("return handlerName switch"))
            .Skip(1)
            .TakeWhile(l => !l.Contains("};"))
            .Where(l => l.Contains("=>"))
            .ToList();

        var positiveIndex = switchLines.FindIndex(l => l.Contains("\"positive\""));
        var zeroIndex = switchLines.FindIndex(l => l.Contains("\"zero\""));
        var negativeIndex = switchLines.FindIndex(l => l.Contains("\"negative\""));

        // Higher priority should come first: positive (10) > zero (0) > negative (-10)
        Assert.True(positiveIndex < zeroIndex, "Positive priority (10) should come before zero priority (0)");
        Assert.True(zeroIndex < negativeIndex, "Zero priority (0) should come before negative priority (-10)");
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithSamePriorityHandlers_ShouldOrderByNameWithDefaultFirst()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create multiple handlers with the same priority
        var handler1 = CreateMockHandler("TestRequest", "string", "Handler1", "HandleAsync", false, "alpha", 5);
        var handler2 = CreateMockHandler("TestRequest", "string", "Handler2", "HandleAsync", false, "beta", 5);
        var defaultHandler = CreateMockHandler("TestRequest", "string", "DefaultHandler", "HandleAsync", false, null, 5);

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);
        discoveryResult.Handlers.Add(defaultHandler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        // Should generate switch
        Assert.Contains("return handlerName switch", source);

        // All handlers should be present
        Assert.Contains("\"alpha\"", source);
        Assert.Contains("\"beta\"", source);
        Assert.Contains("null or \"default\"", source);

        // Extract switch expression
        var switchLines = source.Split('\n')
            .SkipWhile(l => !l.Contains("return handlerName switch"))
            .Skip(1)
            .TakeWhile(l => !l.Contains("};"))
            .Where(l => l.Contains("=>"))
            .ToList();

        // Default handler should come before other handlers with same priority
        var defaultIndex = switchLines.FindIndex(l => l.Contains("null or \"default\""));
        var alphaIndex = switchLines.FindIndex(l => l.Contains("\"alpha\""));
        var betaIndex = switchLines.FindIndex(l => l.Contains("\"beta\""));

        Assert.True(defaultIndex >= 0, "Default handler should be in switch");
        Assert.True(alphaIndex >= 0, "Alpha handler should be in switch");
        Assert.True(betaIndex >= 0, "Beta handler should be in switch");

        // Default should come first among handlers with same priority
        Assert.True(defaultIndex < alphaIndex || defaultIndex < betaIndex,
            "Default handler should come before other named handlers with same priority");
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithZeroPriorityHandlers_ShouldTreatSameAsNoPriority()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create handlers:
        // 1. Explicit Priority = 0
        // 2. No Priority property (defaults to 0)
        // 3. Priority = 1 (to verify ordering)
        var explicitZeroHandler = CreateMockHandler("TestRequest", "string", "ExplicitZero", "HandleAsync", false, "zero", 0);
        var noPriorityHandler = CreateMockHandler("TestRequest", "string", "NoPriority", "HandleAsync", false, "noprio", 0);
        var priority1Handler = CreateMockHandler("TestRequest", "string", "Priority1", "HandleAsync", false, "one", 1);

        discoveryResult.Handlers.Add(explicitZeroHandler);
        discoveryResult.Handlers.Add(noPriorityHandler);
        discoveryResult.Handlers.Add(priority1Handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("return handlerName switch", source);

        // All handlers should be present
        Assert.Contains("\"zero\"", source);
        Assert.Contains("\"noprio\"", source);
        Assert.Contains("\"one\"", source);

        // Priority 1 should come before priority 0 handlers
        var switchLines = source.Split('\n')
            .SkipWhile(l => !l.Contains("return handlerName switch"))
            .Skip(1)
            .TakeWhile(l => !l.Contains("};"))
            .Where(l => l.Contains("=>"))
            .ToList();

        var oneIndex = switchLines.FindIndex(l => l.Contains("\"one\""));
        var zeroIndex = switchLines.FindIndex(l => l.Contains("\"zero\""));
        var noprioIndex = switchLines.FindIndex(l => l.Contains("\"noprio\""));

        // Priority 1 should come before priority 0 handlers
        Assert.True(oneIndex < zeroIndex, "Priority 1 should come before explicit zero priority");
        Assert.True(oneIndex < noprioIndex, "Priority 1 should come before no-priority (implicit zero)");
    }

    private HandlerInfo CreateMockHandlerWithEmptyName(string requestType, string responseType, string handlerType, string methodName, bool isStatic, string emptyOrWhitespaceName)
    {
        // Build attribute syntax with empty/whitespace name
        var attributeArgs = $"(Name = \"{emptyOrWhitespaceName}\")";

        var compilation = CreateCompilation($@"
using Relay.Core.Attributes;

namespace Test {{
    public class {requestType} : Relay.Core.IRequest<{responseType}> {{ }}
    public class {handlerType} {{
        [Handle{attributeArgs}]
        {(isStatic ? "public static" : "public")} async System.Threading.Tasks.ValueTask<{responseType}> {methodName}({requestType} request, System.Threading.CancellationToken cancellationToken) => default;
    }}
}}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{requestType}");
        var responseTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{responseType}");
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{handlerType}");

        var methodSymbol = handlerTypeSymbol?.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();

        // Get the actual AttributeData from the method symbol
        var attributeData = methodSymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "HandleAttribute");

        HandlerInfo handler = new()
        {
            MethodSymbol = methodSymbol,
            HandlerTypeSymbol = handlerTypeSymbol,
            RequestTypeSymbol = requestTypeSymbol,
            ResponseTypeSymbol = responseTypeSymbol,
            Attributes =
            [
                new() {
                    Type = RelayAttributeType.Handle,
                    AttributeData = attributeData
                }
            ]
        };

        return handler;
    }
}