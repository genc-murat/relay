using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

public class NotificationDispatcherGeneratorAsyncTests
{
    [Fact]
    public void GenerateNotificationDispatcher_WithMultipleAsyncNotificationHandlers_ShouldGenerateTaskRunCode()
    {
        // Arrange: Create a scenario with multiple async notification handlers in parallel mode
        // Using the same approach as the existing NotificationDispatcherGenerator tests
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestApp
{
    public class TestNotification : INotification { }

    public class NotificationHandler1
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public async Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
        }
    }
    
    public class NotificationHandler2
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public async Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var generator = new NotificationDispatcherGenerator(context);
        
        // Create multiple async notification handlers similar to existing tests
        var discoveryResult = new HandlerDiscoveryResult();
        
        // Create method symbols for the async handlers
        var handlerType1 = compilation.GetTypeByMetadataName("TestApp.NotificationHandler1");
        var handlerType2 = compilation.GetTypeByMetadataName("TestApp.NotificationHandler2");
        
        var method1 = handlerType1?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => m.Name == "HandleAsync");
        var method2 = handlerType2?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => m.Name == "HandleAsync");
        
        // Create handler info with proper attribute data
        if (method1 != null)
        {
            var attribute1 = method1.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "NotificationAttribute");
            var handlerInfo1 = new HandlerInfo
            {
                MethodSymbol = method1,
                Attributes =
                [
                    new() { 
                        Type = RelayAttributeType.Notification,
                        AttributeData = attribute1
                    }
                ]
            };
            discoveryResult.Handlers.Add(handlerInfo1);
        }
        
        if (method2 != null)
        {
            var attribute2 = method2.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "NotificationAttribute");
            var handlerInfo2 = new HandlerInfo
            {
                MethodSymbol = method2,
                Attributes = new List<RelayAttributeInfo>
                {
                    new() {
                        Type = RelayAttributeType.Notification,
                        AttributeData = attribute2
                    }
                }
            };
            discoveryResult.Handlers.Add(handlerInfo2);
        }

        // Act
        var result = generator.GenerateNotificationDispatcher(discoveryResult);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("Task.Run", result); // This indicates GenerateHandlerExecutionAsTask was called
        Assert.Contains("await", result); // This indicates the async branch was triggered
        Assert.Contains("GeneratedNotificationDispatcher", result);
    }

    [Fact]
    public void GenerateNotificationDispatcher_WithMultipleAsyncStaticNotificationHandlers_ShouldGenerateTaskRunCodeWithAwait()
    {
        // Arrange: Create a scenario with multiple async static notification handlers in parallel mode
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestApp
{
    public class TestNotification : INotification { }

    public class StaticNotificationHandler1
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public static async Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
        }
    }
    
    public class StaticNotificationHandler2
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public static async Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetCompilationUnitRoot();
        
        // Get method symbols for both handlers
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDeclarations = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .Where(c => c.Identifier.ValueText.Contains("StaticNotificationHandler"))
            .ToList();

        var notificationClass = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestNotification");
            
        var notificationType = semanticModel.GetDeclaredSymbol(notificationClass);
        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var generator = new NotificationDispatcherGenerator(context);
        
        // Simulate the discovery process to create handlers with async methods
        var discoveryResult = new HandlerDiscoveryResult();
        
        // Create handler info for each handler
        foreach (var classDecl in classDeclarations)
        {
            var methodDecl = classDecl.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First(m => m.Identifier.ValueText == "HandleAsync");
                
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
            var attributeData = methodSymbol?.GetAttributes().FirstOrDefault();
            
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes =
                [
                    new() {
                        Type = RelayAttributeType.Notification, 
                        AttributeData = attributeData 
                    }
                ]
            };
            
            discoveryResult.Handlers.Add(handlerInfo);
        }

        // Act
        var result = generator.GenerateNotificationDispatcher(discoveryResult);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("Task.Run", result); // This indicates GenerateHandlerExecutionAsTask was called
        Assert.Contains("await", result); // This indicates the async branch was triggered
        Assert.Contains("StaticNotificationHandler1", result);
        Assert.Contains("StaticNotificationHandler2", result);
    }

    [Fact]
    public void GenerateNotificationDispatcher_WithMultipleAsyncNotificationHandlers_WhenUsingICodeGenerator_ShouldGenerateTaskRunWithAwait()
    {
        // Arrange: Test using the ICodeGenerator interface
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestApp
{
    public class TestNotification : INotification { }

    public class AsyncNotificationHandler1
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public async Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
        }
    }
    
    public class AsyncNotificationHandler2
    {
        [Notification(DispatchMode = NotificationDispatchMode.Parallel)]
        public async Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First();
        var root = syntaxTree.GetCompilationUnitRoot();
        
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classDeclarations = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .Where(c => c.Identifier.ValueText.Contains("AsyncNotificationHandler"))
            .ToList();

        var context = new RelayCompilationContext(compilation, CancellationToken.None);
        var generator = new NotificationDispatcherGenerator(context);
        
        var discoveryResult = new HandlerDiscoveryResult();
        
        // Create handler info for each handler
        foreach (var classDecl in classDeclarations)
        {
            var methodDecl = classDecl.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First(m => m.Identifier.ValueText == "HandleAsync");
                
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
            var attributeData = methodSymbol?.GetAttributes().FirstOrDefault();
            
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes =
                [
                    new() {
                        Type = RelayAttributeType.Notification, 
                        AttributeData = attributeData 
                    }
                ]
            };
            
            discoveryResult.Handlers.Add(handlerInfo);
        }

        // Act - Use ICodeGenerator interface
        var options = new GenerationOptions();
        var result = ((ICodeGenerator)generator).Generate(discoveryResult, options);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("Task.Run", result); // Indicates GenerateHandlerExecutionAsTask was called
        Assert.Contains("await", result); // Indicates the async branch was triggered
        Assert.Contains("async () =>", result); // Part of the Task.Run(async () => ...) pattern
    }

    private static Compilation CreateTestCompilation(string source = "")
    {
        if (string.IsNullOrEmpty(source))
        {
            source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestApp
{
    public class TestNotification : INotification { }

    public class TestHandler
    {
        [Notification]
        public void Handle(TestNotification notification, CancellationToken cancellationToken)
        {
        }
    }
}";
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Relay.Core.INotification).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}