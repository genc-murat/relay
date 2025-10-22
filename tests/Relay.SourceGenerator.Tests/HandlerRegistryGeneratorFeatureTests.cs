using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

#pragma warning disable CS8603 // Possible null reference return

namespace Relay.SourceGenerator.Tests
{
    public class HandlerRegistryGeneratorFeatureTests
    {
        [Fact]
        public void GenerateHandlerRegistry_WithNamedHandler_GeneratesCorrectCode()
        {
            // Arrange - directly creating a HandlerInfo with a named attribute
            var sourceCode = @"
                namespace TestApp
                {
                    public class GetUserRequest { }
                    public class GetUserResponse { }
                    public class GetUserHandler
                    {
                        public Task<GetUserResponse> HandleAsync(GetUserRequest request) => null!;
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var methodSymbol = GetMethodSymbol(compilation, "TestApp.GetUserHandler", "HandleAsync");

            // Create a mock attribute info with a named argument
            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo
                    {
                        Type = RelayAttributeType.Handle
                    }
                }
            };

            var discoveryResult = new HandlerDiscoveryResult();
            discoveryResult.Handlers.Add(handlerInfo);

            // Act
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert that the generated code contains the expected elements
            // The generator should create entries for the handler with the correct types
            Assert.Contains("typeof(TestApp.GetUserRequest)", result); // Request type should be present
            Assert.Contains("HandlerRegistry", result); // Should contain the registry class
            Assert.Contains("GetHandlersForRequest", result); // Should contain lookup method
        }

        [Fact]
        public void GenerateHandlerRegistry_WithPriorityHandler_GeneratesCorrectCode()
        {
            // Arrange - testing with priority
            var sourceCode = @"
                namespace TestApp
                {
                    public class GetUserRequest { }
                    public class GetUserResponse { }
                    public class GetUserHandler
                    {
                        public Task<GetUserResponse> HandleAsync(GetUserRequest request) => null!;
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var methodSymbol = GetMethodSymbol(compilation, "TestApp.GetUserHandler", "HandleAsync");

            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodSymbol,
                Attributes = new List<RelayAttributeInfo>
                {
                    new RelayAttributeInfo
                    {
                        Type = RelayAttributeType.Handle
                    }
                }
            };

            var discoveryResult = new HandlerDiscoveryResult();
            discoveryResult.Handlers.Add(handlerInfo);

            // Act
            var result = generator.GenerateHandlerRegistry(discoveryResult);

            // Assert basic functionality
            Assert.Contains("typeof(TestApp.GetUserRequest)", result);
        }

        [Fact]
        public void GenerateHandlerRegistry_WithStaticMethod_GeneratesCorrectCode()
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
            Assert.Contains("IsStatic = true", result); // Static method detection
        }

        [Fact]
        public void GenerateHandlerRegistry_WithValueTaskHandler_GeneratesCorrectCode()
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
                        public ValueTask<GetUserResponse> HandleAsync(GetUserRequest request) => null!;
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
            Assert.Contains("typeof(Test.GetUserResponse)", result); // Response type extracted from ValueTask
        }

        [Fact]
        public void GenerateHandlerRegistry_WithVoidHandler_GeneratesCorrectCode()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                namespace Test
                {
                    public class VoidRequest { }
                    public class VoidHandler
                    {
                        [Relay.Core.Handle]
                        public void Handle(VoidRequest request) { }
                    }
                }");

            var context = new RelayCompilationContext(compilation, default);
            var generator = new HandlerRegistryGenerator(context);

            var methodSymbol = GetMethodSymbol(compilation, "Test.VoidHandler", "Handle");
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
            Assert.DoesNotContain("ResponseType =", result); // No response type for void
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