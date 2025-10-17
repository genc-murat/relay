using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class HandlerRegistryGeneratorTests
    {
        [Fact]
        public void GenerateHandlerRegistry_WithEmptyResult_GeneratesCorrectStructure()
        {
            // Arrange
            var compilation = CreateCompilation("");
            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var discoveryResult = new HandlerDiscoveryResult();

            // Act
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("HandlerMetadata", result);
            Assert.Contains("HandlerRegistry", result);
            Assert.Contains("GetHandlersForRequest", result);
            Assert.Contains("GetNamedHandler", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithEmptyResult_GeneratesBasicStructure()
        {
            // Arrange
            var compilation = CreateCompilation("");
            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var discoveryResult = new HandlerDiscoveryResult();

            // Act
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("HandlerMetadata", result);
            Assert.Contains("HandlerRegistry", result);
            Assert.Contains("GetHandlersForRequest", result);
            Assert.Contains("GetNamedHandler", result);
            Assert.Contains("GetNotificationHandlers", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_ContainsExpectedEnums()
        {
            // Arrange
            var compilation = CreateCompilation("");
            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var discoveryResult = new HandlerDiscoveryResult();

            // Act
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("Request,", result);
            Assert.Contains("Notification,", result);
            Assert.Contains("Stream,", result);
            Assert.Contains("Pipeline", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithRequestHandler_GeneratesHandlerMetadata()
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
            var generator = new HandlerRegistryGenerator(context);

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
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("typeof(Test.GetUserRequest)", result);
            Assert.Contains("typeof(Test.GetUserResponse)", result);
            Assert.Contains("typeof(Test.GetUserHandler)", result);
            Assert.Contains("GetMethod(\"HandleAsync\")", result);
            Assert.Contains("HandlerKind.Request", result);
            Assert.Contains("IsStatic = false", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithNotificationHandler_GeneratesHandlerMetadata()
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
            var generator = new HandlerRegistryGenerator(context);

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
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("typeof(Test.UserCreatedNotification)", result);
            Assert.Contains("HandlerKind.Notification", result);
            Assert.Contains("GetNotificationHandlers", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithStreamHandler_GeneratesHandlerMetadata()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                using System.Collections.Generic;
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetStreamRequest { }
                    public class DataItem { }
                    public class StreamHandler
                    {
                        [Relay.Core.Handle]
                        public IAsyncEnumerable<DataItem> HandleAsync(GetStreamRequest request) => null!;
                    }
                }");

            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var methodSymbol = GetMethodSymbol(compilation, "Test.StreamHandler", "HandleAsync");
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
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("typeof(Test.DataItem)", result);
            Assert.Contains("HandlerKind.Stream", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithPipelineHandler_GeneratesHandlerMetadata()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetUserRequest { }
                    public class GetUserResponse { }
                    public class LoggingPipeline
                    {
                        [Relay.Core.Pipeline]
                        public async Task<GetUserResponse> HandleAsync(GetUserRequest request, HandlerDelegate<GetUserResponse> next) => null!;
                    }
                }");

            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var methodSymbol = GetMethodSymbol(compilation, "Test.LoggingPipeline", "HandleAsync");
            var discoveryResult = new HandlerDiscoveryResult();
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Pipeline }
                }
            };
            discoveryResult.Handlers.Add(handlerInfo);

            // Act
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("HandlerKind.Pipeline", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithMultipleHandlers_IncludesAllHandlers()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class GetUserRequest { }
                    public class GetUserResponse { }
                    public class CreateUserRequest { }
                    public class CreateUserResponse { }
                    public class GetUserHandler
                    {
                        [Relay.Core.Handle]
                        public Task<GetUserResponse> HandleAsync(GetUserRequest request) => null!;
                    }
                    public class CreateUserHandler
                    {
                        [Relay.Core.Handle]
                        public Task<CreateUserResponse> HandleAsync(CreateUserRequest request) => null!;
                    }
                }");

            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var methodSymbol1 = GetMethodSymbol(compilation, "Test.GetUserHandler", "HandleAsync");
            var methodSymbol2 = GetMethodSymbol(compilation, "Test.CreateUserHandler", "HandleAsync");

            var discoveryResult = new HandlerDiscoveryResult();
            discoveryResult.Handlers.Add(new HandlerInfo
            {
                MethodSymbol = methodSymbol1,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
            });
            discoveryResult.Handlers.Add(new HandlerInfo
            {
                MethodSymbol = methodSymbol2,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo { Type = RelayAttributeType.Handle }
                }
            });

            // Act
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("typeof(Test.GetUserRequest)", result);
            Assert.Contains("typeof(Test.GetUserResponse)", result);
            Assert.Contains("typeof(Test.CreateUserRequest)", result);
            Assert.Contains("typeof(Test.CreateUserResponse)", result);
            Assert.Contains("AllHandlers = new()", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_IncludesLookupMethods()
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
            var generator = new HandlerRegistryGenerator(context);

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
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("GetHandlersForRequest", result);
            Assert.Contains("GetNamedHandler", result);
            Assert.Contains("GetNotificationHandlers", result);
            Assert.Contains("OrderByDescending(h => h.Priority)", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithStaticHandler_SetsIsStaticToTrue()
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
            var generator = new HandlerRegistryGenerator(context);

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
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("IsStatic = true", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithVoidReturnType_SetsResponseTypeToNull()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                using System.Threading.Tasks;
                namespace Test
                {
                    public class ProcessRequest { }
                    public class ProcessHandler
                    {
                        [Relay.Core.Handle]
                        public Task HandleAsync(ProcessRequest request) => null!;
                    }
                }");

            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var methodSymbol = GetMethodSymbol(compilation, "Test.ProcessHandler", "HandleAsync");
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
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.DoesNotContain("ResponseType =", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithValueTaskReturnType_ExtractsResponseType()
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
                        public ValueTask<GetUserResponse> HandleAsync(GetUserRequest request) => default;
                    }
                }");

            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

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
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert
            Assert.Contains("typeof(Test.GetUserResponse)", result);
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

    public class TestDiagnosticReporter : IDiagnosticReporter
    {
        public List<Diagnostic> Diagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }


}