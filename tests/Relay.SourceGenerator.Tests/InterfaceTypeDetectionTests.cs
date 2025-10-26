using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Core;

namespace Relay.SourceGenerator.Tests;

public class InterfaceTypeDetectionTests
{
    [Fact]
    public void IsRequestHandlerInterface_Should_Return_True_For_Generic_Request_Handler_Interface()
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

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IRequestHandler`2");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsRequestHandlerInterface(interfaceSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRequestHandlerInterface_Should_Return_True_For_Non_Generic_Request_Handler_Interface()
    {
        // Arrange - Tests the second branch: (interfaceSymbol.Name == "IRequestHandler" && namespace check)
        var source = @"
namespace Relay.Core.Contracts.Handlers
{
    public interface IRequestHandler<in TRequest>
    {
        ValueTask HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}";

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IRequestHandler`1");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsRequestHandlerInterface(interfaceSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRequestHandlerInterface_Should_Return_False_For_Non_Request_Handler_Interface()
    {
        // Arrange
        var source = @"
namespace Relay.Core.Contracts.Handlers
{
    public interface INonHandlerInterface
    {
        void DoSomething();
    }
}";

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.INonHandlerInterface");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsRequestHandlerInterface(interfaceSymbol);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNotificationHandlerInterface_Should_Return_True_For_Generic_Notification_Handler_Interface()
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

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.INotificationHandler`1");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsNotificationHandlerInterface(interfaceSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNotificationHandlerInterface_Should_Return_False_For_Non_Notification_Handler_Interface()
    {
        // Arrange
        var source = @"
namespace Relay.Core.Contracts.Handlers
{
    public interface INonHandlerInterface
    {
        void DoSomething();
    }
}";

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.INonHandlerInterface");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsNotificationHandlerInterface(interfaceSymbol);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStreamHandlerInterface_Should_Return_True_For_Generic_Stream_Handler_Interface()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Contracts.Requests
{
    public interface IRequest<out TResponse> { }
}

namespace Relay.Core.Contracts.Handlers
{
    public interface IStreamHandler<in TRequest, TResponse>
    {
        IAsyncEnumerable<TResponse> HandleStreamAsync(TRequest request, CancellationToken cancellationToken);
    }
}";

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.IStreamHandler`2");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsStreamHandlerInterface(interfaceSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsStreamHandlerInterface_Should_Return_False_For_Non_Stream_Handler_Interface()
    {
        // Arrange
        var source = @"
namespace Relay.Core.Contracts.Handlers
{
    public interface INonHandlerInterface
    {
        void DoSomething();
    }
}";

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("Relay.Core.Contracts.Handlers.INonHandlerInterface");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsStreamHandlerInterface(interfaceSymbol);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRequestHandlerInterface_Should_Return_False_When_Name_Matches_But_Namespace_Does_Not()
    {
        // Arrange - Tests a scenario where interface name matches but namespace is wrong
        var source = @"
namespace WrongNamespace
{
    public interface IRequestHandler<in TRequest, TResponse>
    {
        void Handle(TRequest request);
    }
}";

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("WrongNamespace.IRequestHandler`2");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsRequestHandlerInterface(interfaceSymbol);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNotificationHandlerInterface_Should_Return_False_When_Name_Matches_But_Namespace_Does_Not()
    {
        // Arrange - Tests a scenario where interface name matches but namespace is wrong
        var source = @"
namespace WrongNamespace
{
    public interface INotificationHandler<in TNotification>
    {
        void Handle(TNotification notification);
    }
}";

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("WrongNamespace.INotificationHandler`1");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsNotificationHandlerInterface(interfaceSymbol);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStreamHandlerInterface_Should_Return_False_When_Name_Matches_But_Namespace_Does_Not()
    {
        // Arrange - Tests a scenario where interface name matches but namespace is wrong
        var source = @"
namespace WrongNamespace
{
    public interface IStreamHandler<in TRequest, TResponse>
    {
        void Handle(TRequest request);
    }
}";

        var compilation = CreateTestCompilation(source);
        var interfaceSymbol = compilation.GetTypeByMetadataName("WrongNamespace.IStreamHandler`2");
        Assert.NotNull(interfaceSymbol);

        // Act
        var result = RelayIncrementalGenerator.IsStreamHandlerInterface(interfaceSymbol);

        // Assert
        Assert.False(result);
    }

    private static Compilation CreateTestCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}