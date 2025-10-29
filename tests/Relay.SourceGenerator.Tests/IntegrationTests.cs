using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Core;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Integration tests for end-to-end code generation scenarios.
/// Tests multi-handler scenarios and complex type scenarios.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void EndToEnd_SimpleRequestHandler_GeneratesCompleteCode()
    {
        // Arrange - Simple request handler scenario
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestApp
{
    public class GetUserQuery : IRequest<UserDto> 
    {
        public int UserId { get; set; }
    }
    
    public class UserDto 
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
    {
        public ValueTask<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new UserDto { Id = request.UserId, Name = ""Test User"" });
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify DI registration
        Assert.Contains("AddRelay", generatedCode);
        Assert.Contains("IRequestHandler<TestApp.GetUserQuery, TestApp.UserDto>", generatedCode);
        Assert.Contains("TestApp.GetUserHandler", generatedCode);
        
        // Verify dispatcher generation
        Assert.Contains("GeneratedRequestDispatcher", generatedCode);
        Assert.Contains("DispatchAsync", generatedCode);
    }

    [Fact]
    public void EndToEnd_MultipleHandlerTypes_GeneratesAllComponents()
    {
        // Arrange - Multiple handler types: request, notification, stream
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Notifications;

namespace TestApp
{
    // Request handlers
    public class CreateOrderCommand : IRequest<int> { }
    public class GetOrderQuery : IRequest<OrderDto> { }
    
    // Notification
    public class OrderCreatedNotification : INotification { }
    
    // Stream request
    public class GetOrdersStreamQuery : IRequest<IAsyncEnumerable<OrderDto>> { }
    
    public class OrderDto { public int Id { get; set; } }
    
    public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, int>
    {
        public ValueTask<int> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
            => ValueTask.FromResult(1);
    }
    
    public class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto>
    {
        public ValueTask<OrderDto> HandleAsync(GetOrderQuery request, CancellationToken cancellationToken)
            => ValueTask.FromResult(new OrderDto());
    }
    
    public class OrderCreatedHandler : INotificationHandler<OrderCreatedNotification>
    {
        public ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
    
    public class GetOrdersStreamHandler : IStreamHandler<GetOrdersStreamQuery, OrderDto>
    {
        public async IAsyncEnumerable<OrderDto> HandleStreamAsync(GetOrdersStreamQuery request, CancellationToken cancellationToken)
        {
            yield return new OrderDto { Id = 1 };
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify all handler types are registered
        Assert.Contains("IRequestHandler<TestApp.CreateOrderCommand, int>", generatedCode);
        Assert.Contains("IRequestHandler<TestApp.GetOrderQuery, TestApp.OrderDto>", generatedCode);
        Assert.Contains("INotificationHandler<TestApp.OrderCreatedNotification>", generatedCode);
        Assert.Contains("IStreamHandler<TestApp.GetOrdersStreamQuery, TestApp.OrderDto>", generatedCode);
    }

    [Fact]
    public void MultiHandler_TenHandlers_GeneratesEfficientDispatcher()
    {
        // Arrange - Multiple handlers to test scalability
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestApp
{
    public class Request1 : IRequest<string> { }
    public class Request2 : IRequest<int> { }
    public class Request3 : IRequest<bool> { }
    public class Request4 : IRequest<double> { }
    public class Request5 : IRequest<long> { }
    public class Request6 : IRequest<string> { }
    public class Request7 : IRequest<int> { }
    public class Request8 : IRequest<bool> { }
    public class Request9 : IRequest<double> { }
    public class Request10 : IRequest<long> { }
    
    public class Handler1 : IRequestHandler<Request1, string>
    {
        public ValueTask<string> HandleAsync(Request1 request, CancellationToken ct) => ValueTask.FromResult(""1"");
    }
    
    public class Handler2 : IRequestHandler<Request2, int>
    {
        public ValueTask<int> HandleAsync(Request2 request, CancellationToken ct) => ValueTask.FromResult(2);
    }
    
    public class Handler3 : IRequestHandler<Request3, bool>
    {
        public ValueTask<bool> HandleAsync(Request3 request, CancellationToken ct) => ValueTask.FromResult(true);
    }
    
    public class Handler4 : IRequestHandler<Request4, double>
    {
        public ValueTask<double> HandleAsync(Request4 request, CancellationToken ct) => ValueTask.FromResult(4.0);
    }
    
    public class Handler5 : IRequestHandler<Request5, long>
    {
        public ValueTask<long> HandleAsync(Request5 request, CancellationToken ct) => ValueTask.FromResult(5L);
    }
    
    public class Handler6 : IRequestHandler<Request6, string>
    {
        public ValueTask<string> HandleAsync(Request6 request, CancellationToken ct) => ValueTask.FromResult(""6"");
    }
    
    public class Handler7 : IRequestHandler<Request7, int>
    {
        public ValueTask<int> HandleAsync(Request7 request, CancellationToken ct) => ValueTask.FromResult(7);
    }
    
    public class Handler8 : IRequestHandler<Request8, bool>
    {
        public ValueTask<bool> HandleAsync(Request8 request, CancellationToken ct) => ValueTask.FromResult(false);
    }
    
    public class Handler9 : IRequestHandler<Request9, double>
    {
        public ValueTask<double> HandleAsync(Request9 request, CancellationToken ct) => ValueTask.FromResult(9.0);
    }
    
    public class Handler10 : IRequestHandler<Request10, long>
    {
        public ValueTask<long> HandleAsync(Request10 request, CancellationToken ct) => ValueTask.FromResult(10L);
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify all 10 handlers are registered
        for (int i = 1; i <= 10; i++)
        {
            Assert.Contains($"Handler{i}", generatedCode);
            Assert.Contains($"Request{i}", generatedCode);
        }
        
        // Verify dispatcher uses switch expression for efficient dispatch
        Assert.Contains("switch", generatedCode);
    }

    [Fact]
    public void ComplexTypes_GenericHandlers_GeneratesCorrectCode()
    {
        // Arrange - Generic types and complex scenarios
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestApp
{
    public class PagedRequest<T> : IRequest<PagedResult<T>> 
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
    
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
    }
    
    public class User { public int Id { get; set; } }
    public class Product { public int Id { get; set; } }
    
    public class PagedUserHandler : IRequestHandler<PagedRequest<User>, PagedResult<User>>
    {
        public ValueTask<PagedResult<User>> HandleAsync(PagedRequest<User> request, CancellationToken ct)
            => ValueTask.FromResult(new PagedResult<User> { Items = new List<User>(), TotalCount = 0 });
    }
    
    public class PagedProductHandler : IRequestHandler<PagedRequest<Product>, PagedResult<Product>>
    {
        public ValueTask<PagedResult<Product>> HandleAsync(PagedRequest<Product> request, CancellationToken ct)
            => ValueTask.FromResult(new PagedResult<Product> { Items = new List<Product>(), TotalCount = 0 });
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify generic handlers are registered with correct type parameters
        Assert.Contains("PagedRequest<TestApp.User>", generatedCode);
        Assert.Contains("PagedRequest<TestApp.Product>", generatedCode);
        Assert.Contains("PagedResult<TestApp.User>", generatedCode);
        Assert.Contains("PagedResult<TestApp.Product>", generatedCode);
    }

    [Fact]
    public void ComplexTypes_NestedTypes_GeneratesCorrectCode()
    {
        // Arrange - Nested types scenario
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestApp
{
    public class OuterClass
    {
        public class InnerRequest : IRequest<InnerResponse> 
        {
            public string Data { get; set; }
        }
        
        public class InnerResponse
        {
            public string Result { get; set; }
        }
        
        public class InnerHandler : IRequestHandler<InnerRequest, InnerResponse>
        {
            public ValueTask<InnerResponse> HandleAsync(InnerRequest request, CancellationToken ct)
                => ValueTask.FromResult(new InnerResponse { Result = request.Data });
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify nested types are handled correctly
        Assert.Contains("OuterClass.InnerHandler", generatedCode);
        Assert.Contains("OuterClass.InnerRequest", generatedCode);
        Assert.Contains("OuterClass.InnerResponse", generatedCode);
    }

    [Fact]
    public void ComplexTypes_InheritanceHierarchy_GeneratesCorrectCode()
    {
        // Arrange - Inheritance hierarchy with direct interface implementation
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestApp
{
    public class DerivedRequest : IRequest<BaseResponse> { }
    
    public abstract class BaseResponse { }
    public class DerivedResponse : BaseResponse { }
    
    public class ConcreteHandler : IRequestHandler<DerivedRequest, BaseResponse>
    {
        public ValueTask<BaseResponse> HandleAsync(DerivedRequest request, CancellationToken ct)
            => ValueTask.FromResult<BaseResponse>(new DerivedResponse());
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify handler registration code is generated
        Assert.Contains("AddRelay", generatedCode);
        Assert.Contains("IRequestHandler", generatedCode);
    }

    [Fact]
    public void MultiHandler_MixedVoidAndResponseHandlers_GeneratesCorrectDispatchers()
    {
        // Arrange - Mix of void and response-returning handlers
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestApp
{
    public class VoidCommand1 : IRequest { }
    public class VoidCommand2 : IRequest { }
    public class ResponseQuery1 : IRequest<string> { }
    public class ResponseQuery2 : IRequest<int> { }
    
    public class VoidHandler1 : IRequestHandler<VoidCommand1>
    {
        public ValueTask HandleAsync(VoidCommand1 request, CancellationToken ct)
            => ValueTask.CompletedTask;
    }
    
    public class VoidHandler2 : IRequestHandler<VoidCommand2>
    {
        public ValueTask HandleAsync(VoidCommand2 request, CancellationToken ct)
            => ValueTask.CompletedTask;
    }
    
    public class ResponseHandler1 : IRequestHandler<ResponseQuery1, string>
    {
        public ValueTask<string> HandleAsync(ResponseQuery1 request, CancellationToken ct)
            => ValueTask.FromResult(""response1"");
    }
    
    public class ResponseHandler2 : IRequestHandler<ResponseQuery2, int>
    {
        public ValueTask<int> HandleAsync(ResponseQuery2 request, CancellationToken ct)
            => ValueTask.FromResult(42);
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify both void and response dispatchers are generated
        Assert.Contains("DispatchAsync<TResponse>", generatedCode);
        Assert.Contains("DispatchAsync(IRequest request", generatedCode);
        
        // Verify all handlers are registered
        Assert.Contains("VoidHandler1", generatedCode);
        Assert.Contains("VoidHandler2", generatedCode);
        Assert.Contains("ResponseHandler1", generatedCode);
        Assert.Contains("ResponseHandler2", generatedCode);
    }

    [Fact]
    public void EndToEnd_CompleteApplication_GeneratesAllArtifacts()
    {
        // Arrange - Complete application with all features
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Notifications;

namespace TestApp.Features.Users
{
    public class CreateUserCommand : IRequest<int>
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
    
    public class GetUserQuery : IRequest<UserDto>
    {
        public int UserId { get; set; }
    }
    
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
    
    public class UserCreatedNotification : INotification
    {
        public int UserId { get; set; }
    }
    
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, int>
    {
        public ValueTask<int> HandleAsync(CreateUserCommand request, CancellationToken ct)
            => ValueTask.FromResult(1);
    }
    
    public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
    {
        public ValueTask<UserDto> HandleAsync(GetUserQuery request, CancellationToken ct)
            => ValueTask.FromResult(new UserDto { Id = request.UserId });
    }
    
    public class UserCreatedHandler1 : INotificationHandler<UserCreatedNotification>
    {
        public ValueTask HandleAsync(UserCreatedNotification notification, CancellationToken ct)
            => ValueTask.CompletedTask;
    }
    
    public class UserCreatedHandler2 : INotificationHandler<UserCreatedNotification>
    {
        public ValueTask HandleAsync(UserCreatedNotification notification, CancellationToken ct)
            => ValueTask.CompletedTask;
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify DI registration
        Assert.Contains("AddRelay", generatedCode);
        Assert.Contains("services.AddTransient", generatedCode);
        
        // Verify all handlers are registered
        Assert.Contains("CreateUserHandler", generatedCode);
        Assert.Contains("GetUserHandler", generatedCode);
        Assert.Contains("UserCreatedHandler1", generatedCode);
        Assert.Contains("UserCreatedHandler2", generatedCode);
        
        // Verify dispatcher generation
        Assert.Contains("GeneratedRequestDispatcher", generatedCode);
        
        // Verify notification handlers (multiple handlers for same notification)
        Assert.Contains("INotificationHandler<TestApp.Features.Users.UserCreatedNotification>", generatedCode);
    }

    [Fact]
    public void ComplexTypes_CollectionTypes_GeneratesCorrectCode()
    {
        // Arrange - Collection types as request/response
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestApp
{
    public class GetAllUsersQuery : IRequest<List<UserDto>> { }
    public class GetUserIdsQuery : IRequest<int[]> { }
    public class GetUserDictionaryQuery : IRequest<Dictionary<int, UserDto>> { }
    
    public class UserDto { public int Id { get; set; } }
    
    public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, List<UserDto>>
    {
        public ValueTask<List<UserDto>> HandleAsync(GetAllUsersQuery request, CancellationToken ct)
            => ValueTask.FromResult(new List<UserDto>());
    }
    
    public class GetUserIdsHandler : IRequestHandler<GetUserIdsQuery, int[]>
    {
        public ValueTask<int[]> HandleAsync(GetUserIdsQuery request, CancellationToken ct)
            => ValueTask.FromResult(new int[0]);
    }
    
    public class GetUserDictionaryHandler : IRequestHandler<GetUserDictionaryQuery, Dictionary<int, UserDto>>
    {
        public ValueTask<Dictionary<int, UserDto>> HandleAsync(GetUserDictionaryQuery request, CancellationToken ct)
            => ValueTask.FromResult(new Dictionary<int, UserDto>());
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Verify collection types are handled correctly
        Assert.Contains("List<TestApp.UserDto>", generatedCode);
        Assert.Contains("int[]", generatedCode);
        Assert.Contains("Dictionary<int, TestApp.UserDto>", generatedCode);
    }

    [Fact]
    public void MultiHandler_SameRequestDifferentHandlers_GeneratesCorrectRegistration()
    {
        // Arrange - Multiple handlers for the same request type (should register all)
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestApp
{
    public class ProcessDataCommand : IRequest<string> { }
    
    public class ProcessDataHandler1 : IRequestHandler<ProcessDataCommand, string>
    {
        public ValueTask<string> HandleAsync(ProcessDataCommand request, CancellationToken ct)
            => ValueTask.FromResult(""handler1"");
    }
    
    public class ProcessDataHandler2 : IRequestHandler<ProcessDataCommand, string>
    {
        public ValueTask<string> HandleAsync(ProcessDataCommand request, CancellationToken ct)
            => ValueTask.FromResult(""handler2"");
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
        
        var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
        
        // Both handlers should be registered
        Assert.Contains("ProcessDataHandler1", generatedCode);
        Assert.Contains("ProcessDataHandler2", generatedCode);
    }

    private static CSharpCompilation CreateTestCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Contracts.Requests
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
}

namespace Relay.Core.Contracts.Notifications
{
    public interface INotification { }
}

namespace Relay.Core.Contracts.Handlers
{
    public interface IRequestHandler<in TRequest>
        where TRequest : IRequest
    {
        ValueTask HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
    
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
    
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }
    
    public interface IStreamHandler<in TRequest, TResponse>
        where TRequest : IRequest<IAsyncEnumerable<TResponse>>
    {
        IAsyncEnumerable<TResponse> HandleStreamAsync(TRequest request, CancellationToken cancellationToken);
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
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
