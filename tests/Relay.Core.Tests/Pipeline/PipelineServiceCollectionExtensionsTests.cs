using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Requests;
using Relay.Core.Pipeline.Extensions;
using Relay.Core.Pipeline.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

public class PipelineServiceCollectionExtensionsTests
{
    #region Test Models

    public record TestRequest(string Message) : IRequest<TestResponse>;
    public record TestResponse(string Result);

    public class TestPreProcessor : IRequestPreProcessor<TestRequest>
    {
        public ValueTask ProcessAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return default;
        }
    }

    public class TestPostProcessor : IRequestPostProcessor<TestRequest, TestResponse>
    {
        public ValueTask ProcessAsync(TestRequest request, TestResponse response, CancellationToken cancellationToken)
        {
            return default;
        }
    }

    public class TestException : Exception
    {
        public TestException(string message) : base(message) { }
    }

    public class TestExceptionHandler : IRequestExceptionHandler<TestRequest, TestResponse, TestException>
    {
        public ValueTask<ExceptionHandlerResult<TestResponse>> HandleAsync(
            TestRequest request,
            TestException exception,
            CancellationToken cancellationToken)
        {
            return new ValueTask<ExceptionHandlerResult<TestResponse>>(
                ExceptionHandlerResult<TestResponse>.Handle(new TestResponse($"Handled: {request.Message}")));
        }
    }

    public class TestExceptionAction : IRequestExceptionAction<TestRequest, TestException>
    {
        public ValueTask ExecuteAsync(TestRequest request, TestException exception, CancellationToken cancellationToken)
        {
            return default;
        }
    }

    #endregion

    #region AddPreProcessor Tests

    [Fact]
    public void AddPreProcessor_Generic_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPreProcessor<TestRequest, TestPreProcessor>();
        var provider = services.BuildServiceProvider();
        var preProcessor = provider.GetService<IRequestPreProcessor<TestRequest>>();

        // Assert
        Assert.NotNull(preProcessor);
        Assert.IsType<TestPreProcessor>(preProcessor);
    }

    [Fact]
    public void AddPreProcessor_WithFactory_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPreProcessor<TestRequest, TestPreProcessor>(sp => new TestPreProcessor());
        var provider = services.BuildServiceProvider();
        var preProcessor = provider.GetService<TestPreProcessor>(); // Get concrete type as registered

        // Assert
        Assert.NotNull(preProcessor);
    }

    [Fact]
    public void AddPreProcessor_WithTransientLifetime_Should_Create_New_Instance_Each_Time()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPreProcessor<TestRequest, TestPreProcessor>(ServiceLifetime.Transient);

        var provider = services.BuildServiceProvider();

        // Act
        var preProcessor1 = provider.GetService<IRequestPreProcessor<TestRequest>>();
        var preProcessor2 = provider.GetService<IRequestPreProcessor<TestRequest>>();

        // Assert
        Assert.NotNull(preProcessor1);
        Assert.NotNull(preProcessor2);
        Assert.NotSame(preProcessor1, preProcessor2);
    }

    [Fact]
    public void AddPreProcessor_WithSingletonLifetime_Should_Return_Same_Instance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPreProcessor<TestRequest, TestPreProcessor>(ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        // Act
        var preProcessor1 = provider.GetService<IRequestPreProcessor<TestRequest>>();
        var preProcessor2 = provider.GetService<IRequestPreProcessor<TestRequest>>();

        // Assert
        Assert.NotNull(preProcessor1);
        Assert.NotNull(preProcessor2);
        Assert.Same(preProcessor1, preProcessor2);
    }

    #endregion

    #region AddPostProcessor Tests

    [Fact]
    public void AddPostProcessor_Generic_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostProcessor<TestRequest, TestResponse, TestPostProcessor>();
        var provider = services.BuildServiceProvider();
        var postProcessor = provider.GetService<IRequestPostProcessor<TestRequest, TestResponse>>();

        // Assert
        Assert.NotNull(postProcessor);
        Assert.IsType<TestPostProcessor>(postProcessor);
    }

    [Fact]
    public void AddPostProcessor_WithFactory_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostProcessor<TestRequest, TestResponse, TestPostProcessor>(sp => new TestPostProcessor());
        var provider = services.BuildServiceProvider();
        var postProcessor = provider.GetService<TestPostProcessor>(); // Get concrete type as registered

        // Assert
        Assert.NotNull(postProcessor);
    }

    [Fact]
    public void AddPostProcessor_WithTransientLifetime_Should_Create_New_Instance_Each_Time()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostProcessor<TestRequest, TestResponse, TestPostProcessor>(ServiceLifetime.Transient);

        var provider = services.BuildServiceProvider();

        // Act
        var postProcessor1 = provider.GetService<IRequestPostProcessor<TestRequest, TestResponse>>();
        var postProcessor2 = provider.GetService<IRequestPostProcessor<TestRequest, TestResponse>>();

        // Assert
        Assert.NotNull(postProcessor1);
        Assert.NotNull(postProcessor2);
        Assert.NotSame(postProcessor1, postProcessor2);
    }

    [Fact]
    public void AddPostProcessor_WithSingletonLifetime_Should_Return_Same_Instance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostProcessor<TestRequest, TestResponse, TestPostProcessor>(ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        // Act
        var postProcessor1 = provider.GetService<IRequestPostProcessor<TestRequest, TestResponse>>();
        var postProcessor2 = provider.GetService<IRequestPostProcessor<TestRequest, TestResponse>>();

        // Assert
        Assert.NotNull(postProcessor1);
        Assert.NotNull(postProcessor2);
        Assert.Same(postProcessor1, postProcessor2);
    }

    #endregion

    #region AddExceptionHandler Tests

    [Fact]
    public void AddExceptionHandler_Generic_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>();
        var provider = services.BuildServiceProvider();
        var exceptionHandler = provider.GetService<IRequestExceptionHandler<TestRequest, TestResponse, TestException>>();

        // Assert
        Assert.NotNull(exceptionHandler);
        Assert.IsType<TestExceptionHandler>(exceptionHandler);
    }

    [Fact]
    public void AddExceptionHandler_WithFactory_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>(sp => new TestExceptionHandler());
        var provider = services.BuildServiceProvider();
        var exceptionHandler = provider.GetService<TestExceptionHandler>(); // Get concrete type as registered

        // Assert
        Assert.NotNull(exceptionHandler);
    }

    [Fact]
    public void AddExceptionHandler_WithTransientLifetime_Should_Create_New_Instance_Each_Time()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>(ServiceLifetime.Transient);

        var provider = services.BuildServiceProvider();

        // Act
        var handler1 = provider.GetService<IRequestExceptionHandler<TestRequest, TestResponse, TestException>>();
        var handler2 = provider.GetService<IRequestExceptionHandler<TestRequest, TestResponse, TestException>>();

        // Assert
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.NotSame(handler1, handler2);
    }

    [Fact]
    public void AddExceptionHandler_WithSingletonLifetime_Should_Return_Same_Instance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExceptionHandler<TestRequest, TestResponse, TestException, TestExceptionHandler>(ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        // Act
        var handler1 = provider.GetService<IRequestExceptionHandler<TestRequest, TestResponse, TestException>>();
        var handler2 = provider.GetService<IRequestExceptionHandler<TestRequest, TestResponse, TestException>>();

        // Assert
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.Same(handler1, handler2);
    }

    #endregion

    #region AddExceptionAction Tests

    [Fact]
    public void AddExceptionAction_Generic_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>();
        var provider = services.BuildServiceProvider();
        var exceptionAction = provider.GetService<IRequestExceptionAction<TestRequest, TestException>>();

        // Assert
        Assert.NotNull(exceptionAction);
        Assert.IsType<TestExceptionAction>(exceptionAction);
    }

    [Fact]
    public void AddExceptionAction_WithFactory_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>(sp => new TestExceptionAction());
        var provider = services.BuildServiceProvider();
        var exceptionAction = provider.GetService<TestExceptionAction>(); // Get concrete type as registered

        // Assert
        Assert.NotNull(exceptionAction);
    }

    [Fact]
    public void AddExceptionAction_WithTransientLifetime_Should_Create_New_Instance_Each_Time()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>(ServiceLifetime.Transient);

        var provider = services.BuildServiceProvider();

        // Act
        var action1 = provider.GetService<IRequestExceptionAction<TestRequest, TestException>>();
        var action2 = provider.GetService<IRequestExceptionAction<TestRequest, TestException>>();

        // Assert
        Assert.NotNull(action1);
        Assert.NotNull(action2);
        Assert.NotSame(action1, action2);
    }

    [Fact]
    public void AddExceptionAction_WithSingletonLifetime_Should_Return_Same_Instance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExceptionAction<TestRequest, TestException, TestExceptionAction>(ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        // Act
        var action1 = provider.GetService<IRequestExceptionAction<TestRequest, TestException>>();
        var action2 = provider.GetService<IRequestExceptionAction<TestRequest, TestException>>();

        // Assert
        Assert.NotNull(action1);
        Assert.NotNull(action2);
        Assert.Same(action1, action2);
    }

    #endregion

    #region AddRelay Methods Tests

    [Fact]
    public void AddRelayPrePostProcessors_Should_Register_Pre_And_Post_Processor_Behaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayPrePostProcessors();

        // Assert - Check that specific pipeline behaviors are registered
        var descriptors = services.Where(d => d.ServiceType.Name.Contains("PipelineBehavior")).ToList();
        
        // Should have both RequestPreProcessorBehavior and RequestPostProcessorBehavior registered as open generics
        Assert.Contains(descriptors, d => d.ImplementationType?.Name.Contains("RequestPreProcessorBehavior") == true);
        Assert.Contains(descriptors, d => d.ImplementationType?.Name.Contains("RequestPostProcessorBehavior") == true);
    }

    [Fact]
    public void AddRelayExceptionHandlers_Should_Register_Exception_Handling_Behaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayExceptionHandlers();

        // Assert - Check that exception handling pipeline behaviors are registered
        var descriptors = services.Where(d => d.ServiceType.Name.Contains("PipelineBehavior")).ToList();
        
        // Should have both RequestExceptionHandlerBehavior and RequestExceptionActionBehavior
        Assert.Contains(descriptors, d => d.ImplementationType?.Name.Contains("RequestExceptionHandlerBehavior") == true);
        Assert.Contains(descriptors, d => d.ImplementationType?.Name.Contains("RequestExceptionActionBehavior") == true);
    }

    [Fact]
    public void AddRelayTransactions_Should_Register_Transaction_Behavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayTransactions();

        // Assert - Check that transaction behavior is registered
        var descriptors = services.Where(d => d.ServiceType.Name.Contains("PipelineBehavior")).ToList();
        
        Assert.Contains(descriptors, d => d.ImplementationType?.Name.Contains("TransactionBehavior") == true);
    }

    #endregion
}