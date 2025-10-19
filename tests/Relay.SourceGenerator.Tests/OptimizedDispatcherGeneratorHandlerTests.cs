using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Relay.SourceGenerator;
using Xunit;

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

    private Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private HandlerInfo CreateMockHandler(string requestType, string responseType, string handlerType = "TestHandler", string methodName = "HandleAsync", bool isStatic = false, string? handlerName = null, int priority = 0)
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

        var handler = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            HandlerTypeSymbol = handlerTypeSymbol,
            RequestTypeSymbol = requestTypeSymbol,
            ResponseTypeSymbol = responseTypeSymbol,
            Attributes = new List<RelayAttributeInfo>
            {
                new RelayAttributeInfo
                {
                    Type = RelayAttributeType.Handle,
                    AttributeData = CreateMockAttributeData(handlerName, priority)
                }
            }
        };

        return handler;
    }

    private AttributeData CreateMockAttributeData(string? handlerName, int priority)
    {
        // For testing purposes, we'll create a mock attribute data
        // In a real scenario, this would be created from actual syntax
        return null!;
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

            var handler = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                HandlerTypeSymbol = handlerTypeSymbol,
                RequestTypeSymbol = requestTypeSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
            };

            discoveryResult.Handlers.Add(handler);
        }

        // Act
        var source = generator.GenerateOptimizedDispatcher(discoveryResult);

        // Assert
        Assert.Contains("Optimized handler selection with branch prediction", source);
        Assert.Contains("Most common handlers first for better branch prediction", source);
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

        var streamHandler = new HandlerInfo
        {
            MethodSymbol = streamMethodSymbol,
            HandlerTypeSymbol = streamHandlerTypeSymbol,
            RequestTypeSymbol = streamRequestTypeSymbol,
            Attributes = new List<RelayAttributeInfo>
            {
                new RelayAttributeInfo { Type = RelayAttributeType.Handle }
            }
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

        var notificationHandler = new HandlerInfo
        {
            MethodSymbol = notificationMethodSymbol,
            HandlerTypeSymbol = notificationHandlerTypeSymbol,
            RequestTypeSymbol = notificationTypeSymbol,
            Attributes = new List<RelayAttributeInfo>
            {
                new RelayAttributeInfo { Type = RelayAttributeType.Notification }
            }
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
}