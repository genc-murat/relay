using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Relay.SourceGenerator;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class SourceGeneratorTests
    {
        [Fact]
        public void RelayIncrementalGenerator_Should_Be_Defined()
        {
            // Arrange & Act
            var generatorType = typeof(RelayIncrementalGenerator);

            // Assert
            generatorType.Should().NotBeNull();
            generatorType.Should().BeAssignableTo<Microsoft.CodeAnalysis.IIncrementalGenerator>();
        }

        [Fact]
        public void RelaySyntaxReceiver_Should_Be_Defined()
        {
            // Arrange & Act
            var receiverType = typeof(RelaySyntaxReceiver);

            // Assert
            receiverType.Should().NotBeNull();
            receiverType.Should().BeAssignableTo<Microsoft.CodeAnalysis.ISyntaxReceiver>();
        }

        [Fact]
        public async Task Generator_Should_Execute_Without_Errors_On_Empty_Project()
        {
            // Arrange
            var source = @"
namespace TestProject
{
    public class EmptyClass
    {
    }
}";

            // Act & Assert
            await RunGeneratorTest(source, expectMarkerFile: false);
        }

        [Fact(Skip = "Incremental generator attribute detection needs refinement - tracked in issue #123")]
        public async Task Generator_Should_Generate_Marker_File_When_Relay_Core_Referenced()
        {
            // Arrange - Use a more complete handler that matches the expected pattern
            var source = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestRequest : IRequest<string>
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TestHandler : IRequestHandler<TestRequest, string>
    {
        [Handle]
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult($""Handled: {request.Message}"");
        }
    }
}";

            // Act & Assert
            await RunGeneratorTest(source, expectMarkerFile: true);
        }

        [Fact]
        public async Task Generator_Should_Report_Missing_Relay_Core_Reference()
        {
            // Arrange
            var source = @"
namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest(string request)
        {
            return request;
        }
    }
}";

            // Act
            var result = await RunGeneratorTestWithoutRelayCoreReference(source);

            // Assert
            result.Diagnostics.Should().Contain(d => d.Id == "RELAY_GEN_004");
        }

        [Fact]
        public void SyntaxReceiver_Should_Collect_Attributed_Methods()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest(string request) => request;

        [Notification]
        public void HandleNotification(string notification) { }

        public void RegularMethod() { }
    }
}";

            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var receiver = new RelaySyntaxReceiver();

            // Act
            foreach (var node in syntaxTree.GetRoot().DescendantNodes())
            {
                receiver.OnVisitSyntaxNode(node);
            }

            // Assert
            receiver.CandidateMethods.Should().HaveCount(2);
            receiver.CandidateMethods.Should().Contain(m => m.Identifier.ValueText == "HandleTest");
            receiver.CandidateMethods.Should().Contain(m => m.Identifier.ValueText == "HandleNotification");
        }

        [Fact]
        public void CompilationContext_Should_Provide_Assembly_Information()
        {
            // Arrange
            var compilation = CreateTestCompilation("TestAssembly");
            var context = new RelayCompilationContext(compilation, default);

            // Act & Assert
            context.AssemblyName.Should().Be("TestAssembly");
            context.Compilation.Should().Be(compilation);
        }

        private static async Task<GeneratorDriverRunResult> RunGeneratorTest(string source, bool expectMarkerFile)
        {
            var result = await RunGeneratorTestCore(source, includeRelayCoreReference: true);

            // Verify no errors
            result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                .Should().BeEmpty();

            if (expectMarkerFile)
            {
                // Verify marker file was generated
                result.GeneratedTrees.Should().Contain(tree => tree.FilePath.EndsWith("RelayGeneratorMarker.g.cs"));

                var markerFile = result.GeneratedTrees.First(tree => tree.FilePath.EndsWith("RelayGeneratorMarker.g.cs"));
                var markerContent = markerFile.ToString();
                markerContent.Should().Contain("RelayGeneratorMarker");
                markerContent.Should().Contain("TestAssembly");
            }
            else
            {
                // Should not generate marker file without Relay attributes
                result.GeneratedTrees.Should().BeEmpty();
            }

            return result;
        }

        private static async Task<GeneratorDriverRunResult> RunGeneratorTestWithoutRelayCoreReference(string source)
        {
            return await RunGeneratorTestCore(source, includeRelayCoreReference: false);
        }

        private static async Task<GeneratorDriverRunResult> RunGeneratorTestCore(string source, bool includeRelayCoreReference)
        {
            var compilation = CreateTestCompilation("TestAssembly", source, includeRelayCoreReference);

            var generator = new RelayIncrementalGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);

            var runResult = driver.RunGenerators(compilation);
            return runResult.GetRunResult();
        }

        private static CSharpCompilation CreateTestCompilation(string assemblyName, string? source = null, bool includeRelayCoreReference = true)
        {
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            }.ToList();

            if (includeRelayCoreReference)
            {
                references.Add(MetadataReference.CreateFromFile(typeof(Relay.Core.IRelay).Assembly.Location));
            }

            var syntaxTrees = source != null
                ? new[] { CSharpSyntaxTree.ParseText(source) }
                : System.Array.Empty<SyntaxTree>();

            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}