using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class HandlerRegistryGeneratorBasicTests
    {
        [Fact]
        public void GenerateHandlerRegistry_WithRequestHandlers_GeneratesCorrectCode()
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
            Assert.Contains("Request", result); // HandlerKind
        }

        [Fact]
        public void GenerateHandlerRegistry_WithNotificationHandler_GeneratesCorrectCode()
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
            Assert.Contains("Notification", result); // HandlerKind
            Assert.Contains("GetNotificationHandlers", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithStreamHandler_GeneratesCorrectCode()
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
            Assert.Contains("typeof(Test.DataItem)", result); // Response type extracted from IAsyncEnumerable
            Assert.Contains("Stream", result); // HandlerKind for IAsyncEnumerable return
        }

        [Fact]
        public void GenerateHandlerRegistry_WithPipelineHandler_GeneratesCorrectCode()
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
            Assert.Contains("Pipeline", result); // HandlerKind for Pipeline attribute
        }

        private Compilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Relay.Core.IRequest<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation).Assembly.Location)
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private IMethodSymbol GetMethodSymbol(Compilation compilation, string sourceTypeName, string methodName)
        {
            var syntaxTree = compilation.SyntaxTrees.First();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetCompilationUnitRoot();

            // Find the class declaration by name
            var className = sourceTypeName.Split('.').Last();
            var classDeclaration = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.ValueText == className);

            if (classDeclaration != null)
            {
                var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                if (typeSymbol != null)
                {
                    var methodSymbol = typeSymbol.GetMembers()
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(m => m.Name == methodName);
                    return methodSymbol;
                }
            }

            return null;
        }
    }
}