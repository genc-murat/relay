using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
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