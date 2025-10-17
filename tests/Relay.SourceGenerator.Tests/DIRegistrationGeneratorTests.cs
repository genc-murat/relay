using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
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
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
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
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Notification }
                }
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
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
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
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
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
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
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

            var context = new RelayCompilationContext(compilation, default);
            var generator = new DIRegistrationGenerator(context);

            var methodSymbol = GetMethodSymbol(compilation, "Test.GetUserHandler", "HandleAsync");
            var discoveryResult = new HandlerDiscoveryResult();
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
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
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
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

        private Compilation CreateCompilation(string source)
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
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private IMethodSymbol GetMethodSymbol(Compilation compilation, string typeName, string methodName)
        {
            var typeSymbol = compilation.GetTypeByMetadataName(typeName);
            if (typeSymbol == null) return null!;

            var methodSymbol = typeSymbol.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();
            return methodSymbol!;
        }
    }
}