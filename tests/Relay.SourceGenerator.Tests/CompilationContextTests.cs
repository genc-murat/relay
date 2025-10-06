extern alias RelayCore;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class CompilationContextTests
    {
        [Fact]
        public void Constructor_Should_Initialize_Properties()
        {
            // Arrange
            var compilation = CreateTestCompilation("TestAssembly");
            var cancellationToken = new System.Threading.CancellationToken();

            // Act
            var context = new RelayCompilationContext(compilation, cancellationToken);

            // Assert
            context.Compilation.Should().Be(compilation);
            context.CancellationToken.Should().Be(cancellationToken);
            context.AssemblyName.Should().Be("TestAssembly");
        }

        [Fact]
        public void Constructor_Should_Throw_When_Compilation_Is_Null()
        {
            // Act & Assert
            var act = () => new RelayCompilationContext(null!, default);
            act.Should().Throw<System.ArgumentNullException>()
                .WithParameterName("compilation");
        }

        [Fact]
        public void GetSemanticModel_Should_Return_Semantic_Model()
        {
            // Arrange
            var source = "namespace Test { public class TestClass { } }";
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateTestCompilation("TestAssembly", syntaxTree);
            var context = new RelayCompilationContext(compilation, default);

            // Act
            var semanticModel = context.GetSemanticModel(syntaxTree);

            // Assert
            semanticModel.Should().NotBeNull();
            semanticModel.SyntaxTree.Should().Be(syntaxTree);
        }

        [Fact]
        public void FindType_Should_Return_Type_When_Exists()
        {
            // Arrange
            var compilation = CreateTestCompilation("TestAssembly");
            var context = new RelayCompilationContext(compilation, default);

            // Act
            var objectType = context.FindType("System.Object");

            // Assert
            objectType.Should().NotBeNull();
            objectType!.Name.Should().Be("Object");
        }

        [Fact]
        public void FindType_Should_Return_Null_When_Type_Does_Not_Exist()
        {
            // Arrange
            var compilation = CreateTestCompilation("TestAssembly");
            var context = new RelayCompilationContext(compilation, default);

            // Act
            var nonExistentType = context.FindType("NonExistent.Type");

            // Assert
            nonExistentType.Should().BeNull();
        }

        [Fact]
        public void HasRelayCoreReference_Should_Return_True_When_Referenced()
        {
            // Arrange
            var compilation = CreateTestCompilationWithRelayCoreReference("TestAssembly");
            var context = new RelayCompilationContext(compilation, default);

            // Act
            var hasReference = context.HasRelayCoreReference();

            // Assert
            hasReference.Should().BeTrue();
        }

        [Fact]
        public void HasRelayCoreReference_Should_Return_False_When_Not_Referenced()
        {
            // Arrange
            var compilation = CreateTestCompilation("TestAssembly");
            var context = new RelayCompilationContext(compilation, default);

            // Act
            var hasReference = context.HasRelayCoreReference();

            // Assert
            hasReference.Should().BeFalse();
        }

        [Fact]
        public void AssemblyName_Should_Handle_Null_Assembly_Name()
        {
            // Arrange
            var compilation = CSharpCompilation.Create(
                null, // null assembly name
                System.Array.Empty<SyntaxTree>(),
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            // Act
            var context = new RelayCompilationContext(compilation, default);

            // Assert
            context.AssemblyName.Should().Be("Unknown");
        }

        private static CSharpCompilation CreateTestCompilation(string assemblyName, SyntaxTree? syntaxTree = null)
        {
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            };

            var syntaxTrees = syntaxTree != null
                ? new[] { syntaxTree }
                : System.Array.Empty<SyntaxTree>();

            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static CSharpCompilation CreateTestCompilationWithRelayCoreReference(string assemblyName)
        {
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
                // Reference to Relay.Core assembly - use a type that exists in the Relay.Core namespace
                MetadataReference.CreateFromFile(typeof(RelayCore::Relay.Core.Contracts.Core.IRelay).Assembly.Location),
            };

            return CSharpCompilation.Create(
                assemblyName,
                System.Array.Empty<SyntaxTree>(),
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}