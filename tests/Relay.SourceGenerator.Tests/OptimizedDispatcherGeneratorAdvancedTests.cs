using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Relay.SourceGenerator;
using Xunit;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

public class OptimizedDispatcherGeneratorAdvancedTests
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
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static HandlerInfo CreateMockHandler(string requestType, string responseType, string handlerType = "TestHandler", string methodName = "HandleAsync", bool isStatic = false, string? handlerName = null, int priority = 0)
    {
        var compilation = CreateCompilation($@"
namespace Test {{
    public class {requestType} : Relay.Core.IRequest<{responseType}> {{ }}
    public class {handlerType} {{
        {(isStatic ? "public static" : "public")} async System.Threading.Tasks.ValueTask<{responseType}> {methodName}({requestType} request, System.Threading.CancellationToken cancellationToken) => default;
    }}
}}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{requestType}");
        var responseTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{responseType}");
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{handlerType}");

        var methodSymbol = handlerTypeSymbol?.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();

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
                    AttributeData = CreateMockAttributeData(handlerName, priority)
                }
            ]
        };

        return handler;
    }

    private static AttributeData CreateMockAttributeData(string? _, int __)
    {
        // For testing purposes, we'll create a mock attribute data
        // In a real scenario, this would be created from actual syntax
        return null!;
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithStreamingHandlers_ShouldGenerateStreamingMethods()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create a mock streaming handler
        var compilation = CreateCompilation(@"
namespace Test {
    public class StreamRequest : Relay.Core.IStreamRequest<string> { }
    public class StreamHandler {
        public async System.Collections.Generic.IAsyncEnumerable<string> HandleAsync(StreamRequest request, System.Threading.CancellationToken cancellationToken) => default;
    }
}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.StreamRequest");
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.StreamHandler");
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

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("DispatchStreamAsync<TRequest, TResponse>", source);
        Assert.Contains("Optimized streaming dispatch method", source);
        Assert.Contains("IAsyncEnumerable<TResponse>", source);
        Assert.Contains("where TRequest : IStreamRequest<TResponse>", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithStaticStreamingHandlers_ShouldGenerateStaticStreamingMethods()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create a mock static streaming handler
        var compilation = CreateCompilation(@"
namespace Test {
    public class StreamRequest : Relay.Core.IStreamRequest<string> { }
    public static class StaticStreamHandler {
        public static async System.Collections.Generic.IAsyncEnumerable<string> HandleAsync(StreamRequest request, System.Threading.CancellationToken cancellationToken) => default;
    }
}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var requestTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.StreamRequest");
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.StaticStreamHandler");
        var methodSymbol = handlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();

        var handler = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle, AttributeData = null }
            ]
        };

        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("DispatchStreamAsync<TRequest, TResponse>", source);
        Assert.Contains("Test.StaticStreamHandler.HandleAsync", source);
        Assert.DoesNotContain("serviceProvider.GetRequiredService", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithNotificationHandlers_ShouldGenerateNotificationMethods()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create a mock notification handler
        var compilation = CreateCompilation(@"
namespace Test {
    public class TestNotification : Relay.Core.INotification { }
    public class NotificationHandler {
        public async System.Threading.Tasks.ValueTask HandleAsync(TestNotification notification, System.Threading.CancellationToken cancellationToken) => default;
    }
}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var notificationTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.TestNotification");
        var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.NotificationHandler");
        var methodSymbol = handlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();

        HandlerInfo handler = new()
        {
            MethodSymbol = methodSymbol,
            HandlerTypeSymbol = handlerTypeSymbol,
            RequestTypeSymbol = notificationTypeSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Notification }
            ]
        };

        discoveryResult.Handlers.Add(handler);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("DispatchNotificationAsync<TNotification>", source);
        Assert.Contains("Optimized notification dispatch method with parallel execution support", source);
        Assert.Contains("where TNotification : INotification", source);
        Assert.Contains("switch (notification)", source);
        Assert.Contains("case Test.TestNotification n:", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMainDispatchMethod_ShouldGenerateTypeSwitching()
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
        Assert.Contains("DispatchAsync<TRequest, TResponse>", source);
        Assert.Contains("Main dispatch method with pattern matching for O(1) type lookup", source);
        Assert.Contains("Pattern matching switch expression for O(1) type lookup", source);
        Assert.Contains("return request switch", source);
        Assert.Contains("Test.TestRequest r => ", source);
        Assert.Contains("await Dispatch_Test_TestRequest(r, serviceProvider, handlerName, cancellationToken)", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMultipleNotificationHandlers_ShouldGenerateParallelExecution()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        // Create multiple notification handlers for the same notification type
        var compilation = CreateCompilation(@"
namespace Test {
    public class TestNotification : Relay.Core.INotification { }
    public class NotificationHandler1 {
        public async System.Threading.Tasks.ValueTask HandleAsync(TestNotification notification, System.Threading.CancellationToken cancellationToken) => default;
    }
    public class NotificationHandler2 {
        public async System.Threading.Tasks.ValueTask HandleAsync(TestNotification notification, System.Threading.CancellationToken cancellationToken) => default;
    }
}");

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        var notificationTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Test.TestNotification");

        foreach (var handlerName in new[] { "NotificationHandler1", "NotificationHandler2" })
        {
            var handlerTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName($"Test.{handlerName}");
            var methodSymbol = handlerTypeSymbol?.GetMembers("HandleAsync").OfType<IMethodSymbol>().FirstOrDefault();

            HandlerInfo handler = new()
            {
                MethodSymbol = methodSymbol,
                HandlerTypeSymbol = handlerTypeSymbol,
                RequestTypeSymbol = notificationTypeSymbol,
                Attributes =
                [
                    new() { Type = RelayAttributeType.Notification }
                ]
            };

            discoveryResult.Handlers.Add(handler);
        }

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("var tasks = new ValueTask[2]", source);
        Assert.Contains("for (int i = 0; i < tasks.Length; i++)", source);
        Assert.Contains("NotificationHandler1", source);
        Assert.Contains("NotificationHandler2", source);
    }

    [Fact]
    public void GenerateOptimizedDispatcher_WithMultipleHandlers_ShouldIncludeErrorHandlingForUnknownHandlerNames()
    {
        // Arrange
        var context = CreateTestContext();
        var generator = new OptimizedDispatcherGenerator(context);
        var discoveryResult = new HandlerDiscoveryResult();

        var handler1 = CreateMockHandler("TestRequest", "string", "TestHandler1", "HandleAsync", false, "default", 1);
        var handler2 = CreateMockHandler("TestRequest", "string", "TestHandler2", "HandleAsync", false, "special", 2);

        discoveryResult.Handlers.Add(handler1);
        discoveryResult.Handlers.Add(handler2);

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("No handler found with name", source);
        Assert.Contains("Optimized handler selection with pattern matching", source);
    }
}