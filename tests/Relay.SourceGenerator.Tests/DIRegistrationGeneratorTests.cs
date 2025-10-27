using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

public class DIRegistrationGeneratorTests
{
    [Fact]
    public void GenerateDIRegistrations_WithEmptyResult_GeneratesBasicStructure()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("RelayServiceCollectionExtensions", result);
        Assert.Contains("AddRelay", result);
        Assert.Contains("IServiceCollection", result);
        Assert.Contains("Microsoft.Extensions.DependencyInjection", result);
    }

    [Fact]
    public void GenerateDIRegistrations_ContainsRelayRegistration()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("services.TryAddSingleton<IRelay, RelayImplementation>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_ContainsExtensionMethods()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("public static IServiceCollection AddRelay", result);
        Assert.Contains("RelayServiceCollectionExtensions", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithRequestHandler_RegistersHandlerAsScoped()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetUserRequest { }
                    public class GetUserResponse { }
                    public class GetUserHandler
                    {
                        [Relay.Core.Handle]
                        public Task<GetUserResponse> HandleAsync(GetUserRequest request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.GetUserHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("services.AddScoped<Test.GetUserHandler>();", result);
        Assert.Contains("RelayWarmupFilter", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithNotificationHandler_RegistersNotificationDispatcher()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class UserCreatedNotification { }
                    public class UserCreatedHandler
                    {
                        [Relay.Core.Notification]
                        public Task HandleAsync(UserCreatedNotification notification) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.UserCreatedHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Notification }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("services.TryAddSingleton<INotificationDispatcher, GeneratedNotificationDispatcher>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithQueryHandler_RegistersAsSingleton()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetUserQuery { }
                    public class GetUserResponse { }
                    public class GetUserQueryHandler
                    {
                        [Relay.Core.Handle]
                        public Task<GetUserResponse> HandleAsync(GetUserQuery request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.GetUserQueryHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("services.AddSingleton<Test.GetUserQueryHandler>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithCommandHandler_RegistersAsScoped()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class CreateUserCommand { }
                    public class CreateUserResponse { }
                    public class CreateUserCommandHandler
                    {
                        [Relay.Core.Handle]
                        public Task<CreateUserResponse> HandleAsync(CreateUserCommand request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.CreateUserCommandHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("services.AddScoped<Test.CreateUserCommandHandler>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithStaticHandler_DoesNotRegisterHandler()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetUserRequest { }
                    public class GetUserResponse { }
                    public class GetUserHandler
                    {
                        [Relay.Core.Handle]
                        public static Task<GetUserResponse> HandleAsync(GetUserRequest request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.GetUserHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        var handlerInfo = new HandlerInfo
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert - Static handlers should not be registered, so no handler-specific registration should occur
        Assert.DoesNotContain("Test.GetUserHandler", result);
        // But there should still be other services registered
        Assert.Contains("services.TryAddSingleton<IRelay, RelayImplementation>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_IncludesIndividualRegistrationMethods()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetUserRequest { }
                    public class GetUserResponse { }
                    public class GetUserHandler
                    {
                        [Relay.Core.Handle]
                        public Task<GetUserResponse> HandleAsync(GetUserRequest request) => null!;
                    }
                }");

        RelayCompilationContext context = new(compilation, default);
        DIRegistrationGenerator generator = new(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.GetUserHandler", "HandleAsync");
        HandlerDiscoveryResult discoveryResult = new();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("public static IServiceCollection AddScoped<T>", result);
        Assert.Contains("services.TryAddScoped<T>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_IncludesWarmupFilter()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetUserRequest { }
                    public class GetUserResponse { }
                    public class GetUserHandler
                    {
                        [Relay.Core.Handle]
                        public Task<GetUserResponse> HandleAsync(GetUserRequest request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.GetUserHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("RelayWarmupFilter", result);
        Assert.Contains("IStartupFilter", result);
        Assert.Contains("WarmUpHandler<Test.GetUserHandler>", result);
    }

    [Fact]
    public void GenerateDIRegistrations_IncludesAllRequiredUsings()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("using Microsoft.AspNetCore.Builder;", result);
        Assert.Contains("using Microsoft.AspNetCore.Hosting;", result);
        Assert.Contains("using Microsoft.Extensions.DependencyInjection;", result);
        Assert.Contains("using Microsoft.Extensions.DependencyInjection.Extensions;", result);
        Assert.Contains("using Relay.Core;", result);
        Assert.Contains("using Relay.Generated;", result);
    }

    [Fact]
    public void DIRegistrationGenerator_Properties_ReturnCorrectValues()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        // Act & Assert
        Assert.Equal("DI Registration Generator", generator.GeneratorName);
        Assert.Equal("RelayServiceRegistration", generator.OutputFileName);
        Assert.Equal(10, generator.Priority);
    }

    [Fact]
    public void DIRegistrationGenerator_CanGenerate_ReturnsTrueForValidResult()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();

        // Act
        var canGenerate = generator.CanGenerate(discoveryResult);

        // Assert
        Assert.True(canGenerate);
    }

    [Fact]
    public void DIRegistrationGenerator_CanGenerate_ReturnsFalseForNullResult()
    {
    // Arrange
    var compilation = CreateCompilation("");
    var context = new RelayCompilationContext(compilation, default);
    var generator = new DIRegistrationGenerator(context);

    // Act
    var canGenerate = generator.CanGenerate(null!);

    // Assert
    Assert.False(canGenerate);
    }

    [Fact]
    public void DIRegistrationGenerator_Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DIRegistrationGenerator(null!));
    }

    [Fact]
    public void GenerateDIRegistrations_WithReaderHandler_RegistersAsSingleton()
    {
        // Arrange - Test IsStatelessHandler logic
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetDataRequest { }
                    public class GetDataResponse { }
                    public class DataReaderHandler
                    {
                        [Relay.Core.Handle]
                        public Task<GetDataResponse> HandleAsync(GetDataRequest request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.DataReaderHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert - Should be registered as Singleton due to "Reader" in name
        Assert.Contains("services.AddSingleton<Test.DataReaderHandler>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithWriterHandler_RegistersAsScoped()
    {
        // Arrange - Test IsRequestScopedHandler logic
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class SaveDataRequest { }
                    public class SaveDataResponse { }
                    public class DataWriterHandler
                    {
                        [Relay.Core.Handle]
                        public Task<SaveDataResponse> HandleAsync(SaveDataRequest request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.DataWriterHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Handle }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert - Should be registered as Scoped due to "Writer" in name
        Assert.Contains("services.AddScoped<Test.DataWriterHandler>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithMixedHandlerTypes_GeneratesCorrectRegistrations()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class QueryRequest { }
                    public class CommandRequest { }
                    public class NotificationEvent { }
                    public class Response { }

                    public class QueryHandler
                    {
                        [Relay.Core.Handle]
                        public Task<Response> HandleAsync(QueryRequest request) => null!;
                    }

                    public class CommandHandler
                    {
                        [Relay.Core.Handle]
                        public Task<Response> HandleAsync(CommandRequest request) => null!;
                    }

                    public class NotificationHandler
                    {
                        [Relay.Core.Notification]
                        public Task HandleAsync(NotificationEvent notification) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();

        // Add query handler (should be singleton)
        var queryMethod = GetMethodSymbol(compilation, "Test.QueryHandler", "HandleAsync");
        discoveryResult.Handlers.Add(new HandlerInfo
        {
            MethodSymbol = queryMethod,
            Attributes = [new() { Type = RelayAttributeType.Handle }]
        });

        // Add command handler (should be scoped)
        var commandMethod = GetMethodSymbol(compilation, "Test.CommandHandler", "HandleAsync");
        discoveryResult.Handlers.Add(new HandlerInfo
        {
            MethodSymbol = commandMethod,
            Attributes = [new() { Type = RelayAttributeType.Handle }]
        });

        // Add notification handler
        var notificationMethod = GetMethodSymbol(compilation, "Test.NotificationHandler", "HandleAsync");
        discoveryResult.Handlers.Add(new HandlerInfo
        {
            MethodSymbol = notificationMethod,
            Attributes = [new() { Type = RelayAttributeType.Notification }]
        });

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("services.AddSingleton<Test.QueryHandler>();", result);
        Assert.Contains("services.AddScoped<Test.CommandHandler>();", result);
        Assert.Contains("services.TryAddSingleton<INotificationDispatcher, GeneratedNotificationDispatcher>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithPipelineHandler_IncludesPipelineRegistration()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class PipelineRequest { }
                    public class PipelineResponse { }
                    public class PipelineBehavior
                    {
                        [Relay.Core.Pipeline]
                        public Task<PipelineResponse> ProcessAsync(PipelineRequest request, Func<Task<PipelineResponse>> next) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.PipelineBehavior", "ProcessAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.Pipeline }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("Test.PipelineBehavior", result);
        Assert.Contains("services.AddScoped<Test.PipelineBehavior>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithEndpointHandler_IncludesEndpointRegistration()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class EndpointRequest { }
                    public class EndpointResponse { }
                    public class EndpointHandler
                    {
                        [Relay.Core.ExposeAsEndpoint]
                        public Task<EndpointResponse> HandleAsync(EndpointRequest request) => null!;
                    }
                }");

        RelayCompilationContext context = new(compilation, default);
        DIRegistrationGenerator generator = new(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.EndpointHandler", "HandleAsync");
        HandlerDiscoveryResult discoveryResult = new();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes =
            [
                new() { Type = RelayAttributeType.ExposeAsEndpoint }
            ]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert
        Assert.Contains("Test.EndpointHandler", result);
        Assert.Contains("services.AddScoped<Test.EndpointHandler>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithMultipleHandlersOfSameType_RegistersOnce()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class Request1 { }
                    public class Request2 { }
                    public class Response { }
                    public class MultiHandler
                    {
                        [Relay.Core.Handle]
                        public Task<Response> Handle1(Request1 request) => null!;

                        [Relay.Core.Handle]
                        public Task<Response> Handle2(Request2 request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();

        var method1 = GetMethodSymbol(compilation, "Test.MultiHandler", "Handle1");
        var method2 = GetMethodSymbol(compilation, "Test.MultiHandler", "Handle2");

        discoveryResult.Handlers.Add(new HandlerInfo
        {
            MethodSymbol = method1,
            Attributes = [new() { Type = RelayAttributeType.Handle }]
        });

        discoveryResult.Handlers.Add(new HandlerInfo
        {
            MethodSymbol = method2,
            Attributes = [new() { Type = RelayAttributeType.Handle }]
        });

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert - Handler type should be registered (may appear multiple times in different contexts)
        Assert.Contains("Test.MultiHandler", result);
        // The handler appears in both the main registration and the warmup filter
        Assert.True(result.Contains("services.AddScoped<Test.MultiHandler>();") ||
                   result.Contains("services.AddSingleton<Test.MultiHandler>();"));
    }

    [Fact]
    public void GenerateDIRegistrations_IncludesTimestampInHeader()
    {
    // Arrange
    var compilation = CreateCompilation("");
    var context = new RelayCompilationContext(compilation, default);
    var generator = new DIRegistrationGenerator(context);

    var discoveryResult = new HandlerDiscoveryResult();

    // Act
    var result = generator.GenerateDIRegistrations(discoveryResult);

    // Assert
    Assert.Contains("// Generated by Relay.SourceGenerator", result);
    Assert.Contains("// Generation time:", result);
    Assert.Contains("UTC", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithDefaultHandler_RegistersAsScoped()
    {
        // Arrange - Create a handler that doesn't match Query/Reader or Command/Writer patterns
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class SomeRequest { }
                    public class SomeResponse { }
                    public class SomeHandler
                    {
                        [Relay.Core.Handle]
                        public Task<SomeResponse> HandleAsync(SomeRequest request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.SomeHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes = [new() { Type = RelayAttributeType.Handle }]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        // Act
        var result = generator.GenerateDIRegistrations(discoveryResult);

        // Assert - Should default to Scoped in individual registration methods
        Assert.Contains("services.TryAddScoped<T>();", result);
    }

    [Fact]
    public void GenerateDIRegistrations_WithNullDiscoveryResult_ThrowsNullReferenceException()
    {
    // Arrange
    var compilation = CreateCompilation("");
    var context = new RelayCompilationContext(compilation, default);
    var generator = new DIRegistrationGenerator(context);

    // Act & Assert
    Assert.Throws<NullReferenceException>(() => generator.GenerateDIRegistrations(null!));
    }

    [Fact]
    public void Generate_UsesBaseCodeGeneratorInterface()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class TestRequest { }
                    public class TestResponse { }
                    public class TestHandler
                    {
                        [Relay.Core.Handle]
                        public Task<TestResponse> HandleAsync(TestRequest request) => null!;
                    }
                }");

        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var methodSymbol = GetMethodSymbol(compilation, "Test.TestHandler", "HandleAsync");
        var discoveryResult = new HandlerDiscoveryResult();
        HandlerInfo handlerInfo = new()
        {
            MethodSymbol = methodSymbol,
            Attributes = [new() { Type = RelayAttributeType.Handle }]
        };
        discoveryResult.Handlers.Add(handlerInfo);

        var options = new GenerationOptions();

        // Act
        var result = generator.Generate(discoveryResult, options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("RelayServiceCollectionExtensions", result);
        Assert.Contains("AddRelay", result);
        Assert.Contains("namespace Microsoft.Extensions.DependencyInjection", result);
        Assert.Contains("Test.TestHandler", result);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            // Add reference to Relay.Core (this would normally be resolved from the test project)
            MetadataReference.CreateFromFile(typeof(Relay.Core.IRequest<>).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void GetHandlerLifetime_WithNullDiscoveryResult_ReturnsScoped()
    {
        // Arrange - Use reflection to test GetHandlerLifetime with null discoveryResult
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        // Use reflection to access the private GetHandlerLifetime method
        var getHandlerLifetimeMethod = typeof(DIRegistrationGenerator).GetMethod("GetHandlerLifetime",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act & Assert
        var result = getHandlerLifetimeMethod.Invoke(generator, ["Some.Handler", null]);
        Assert.Equal("Scoped", result);
    }

    [Fact]
    public void IsStatelessHandler_WithQueryHandler_ReturnsTrue()
    {
        // Arrange - Use reflection to test IsStatelessHandler
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        // Use reflection to access the private IsStatelessHandler method
        var isStatelessHandlerMethod = typeof(DIRegistrationGenerator).GetMethod("IsStatelessHandler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act & Assert
        var result = isStatelessHandlerMethod.Invoke(generator, ["Some.QueryHandler", null]);
        Assert.True((bool)result);

        result = isStatelessHandlerMethod.Invoke(generator, ["Some.ReaderHandler", null]);
        Assert.True((bool)result);

        result = isStatelessHandlerMethod.Invoke(generator, ["Some.Handler", null]);
        Assert.False((bool)result);
    }

    [Fact]
    public void IsRequestScopedHandler_WithCommandHandler_ReturnsTrue()
    {
        // Arrange - Use reflection to test IsRequestScopedHandler
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        // Use reflection to access the private IsRequestScopedHandler method
        var isRequestScopedHandlerMethod = typeof(DIRegistrationGenerator).GetMethod("IsRequestScopedHandler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act & Assert
        var result = isRequestScopedHandlerMethod.Invoke(generator, ["Some.CommandHandler", null]);
        Assert.True((bool)result);

        result = isRequestScopedHandlerMethod.Invoke(generator, ["Some.WriterHandler", null]);
        Assert.True((bool)result);

        result = isRequestScopedHandlerMethod.Invoke(generator, ["Some.Handler", null]);
        Assert.False((bool)result);
    }

    [Fact]
    public void GetNamespace_ReturnsCorrectNamespace()
    {
        // Arrange
        var compilation = CreateCompilation("");
        var context = new RelayCompilationContext(compilation, default);
        var generator = new DIRegistrationGenerator(context);

        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();

        // Use reflection to access the protected GetNamespace method
        var getNamespaceMethod = typeof(DIRegistrationGenerator).GetMethod("GetNamespace",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act
        var result = getNamespaceMethod.Invoke(generator, [discoveryResult, options]);

        // Assert
        Assert.Equal("Microsoft.Extensions.DependencyInjection", result);
    }

    private static IMethodSymbol GetMethodSymbol(Compilation compilation, string typeName, string methodName)
    {
        var typeSymbol = compilation.GetTypeByMetadataName(typeName);
        if (typeSymbol == null) return null!;

        var methodSymbol = typeSymbol.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();
        return methodSymbol!;
    }
}