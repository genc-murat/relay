using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Relay.SourceGenerator;
using System;
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
            Assert.NotNull(generatorType);
            Assert.IsAssignableFrom<Microsoft.CodeAnalysis.IIncrementalGenerator>(Activator.CreateInstance(generatorType));
        }

        [Fact]
        public void RelaySyntaxReceiver_Should_Be_Defined()
        {
            // Arrange & Act
            var receiverType = typeof(RelaySyntaxReceiver);

            // Assert
            Assert.NotNull(receiverType);
            Assert.IsAssignableFrom<Microsoft.CodeAnalysis.ISyntaxReceiver>(Activator.CreateInstance(receiverType));
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

        [Fact()]
        public async Task Generator_Should_Generate_Marker_File_When_Relay_Core_Referenced()
        {
            // Arrange - Use a handler that implements IRequestHandler interface
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
            Assert.Contains(result.Diagnostics, d => d.Id == "RELAY_GEN_004");
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
            Assert.Equal(2, receiver.CandidateMethods.Count());
            Assert.Contains(receiver.CandidateMethods, m => m.Identifier.ValueText == "HandleTest");
            Assert.Contains(receiver.CandidateMethods, m => m.Identifier.ValueText == "HandleNotification");
        }

        [Fact]
        public void CompilationContext_Should_Provide_Assembly_Information()
        {
            // Arrange
            var compilation = CreateTestCompilation("TestAssembly");
            var context = new RelayCompilationContext(compilation, default);

            // Act & Assert
            Assert.Equal("TestAssembly", context.AssemblyName);
            Assert.Equal(compilation, context.Compilation);
        }

        private static async Task<GeneratorDriverRunResult> RunGeneratorTest(string source, bool expectMarkerFile)
        {
            var result = await RunGeneratorTestCore(source, includeRelayCoreReference: true);

            // Verify no errors - provide detailed error info if any
            var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Any())
            {
                var errorMessages = string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"));
                Assert.Fail($"Expected no errors but found: {errorMessages}");
            }

            // Verify at least one file was generated
            Assert.NotEmpty(result.GeneratedTrees);

            if (expectMarkerFile)
            {
                // When handlers are expected, verify marker file or handler registration was generated
                var hasMarkerFile = result.GeneratedTrees.Any(tree => tree.FilePath.EndsWith("RelayGeneratorMarker.g.cs"));
                var hasHandlerRegistration = result.GeneratedTrees.Any(tree =>
                {
                    var content = tree.ToString();
                    return content.Contains("GeneratedRelayExtensions") || content.Contains("AddRelayGenerated");
                });

                Assert.True(hasMarkerFile || hasHandlerRegistration, 
                    "generator should produce marker file or handler registration when Relay.Core is referenced");

                // Verify the generated code references the test assembly
                var generatedContent = string.Join("\n", result.GeneratedTrees.Select(t => t.ToString()));
                Assert.Contains("GeneratedRelayExtensions", generatedContent);
            }
            else
            {
                // Generator still generates basic AddRelay method even without handlers
                // This is expected behavior for providing basic DI setup
                if (result.GeneratedTrees.Length > 0)
                {
                    Assert.Single(result.GeneratedTrees);
                    var generatedFile = result.GeneratedTrees.First();
                    var content = generatedFile.ToString();
                    Assert.Contains("GeneratedRelayExtensions", content);
                    Assert.Contains("AddRelayGenerated", content);
                }
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

            // Debug: Print referenced assemblies
            var referencedAssemblies = string.Join(", ", compilation.ReferencedAssemblyNames.Select(a => a.Name));
            System.Diagnostics.Debug.WriteLine($"Referenced assemblies: {referencedAssemblies}");

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
                // Create metadata reference with proper assembly identity
                var relayCoreAssembly = typeof(Relay.Core.IRelay).Assembly;
                var relayCoreReference = MetadataReference.CreateFromFile(relayCoreAssembly.Location);
                references.Add(relayCoreReference);
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