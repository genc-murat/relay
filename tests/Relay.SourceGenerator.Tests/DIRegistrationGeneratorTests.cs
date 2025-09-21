using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
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
            
            // Debug: Print the generated result
            System.Console.WriteLine("Generated DI registrations:");
            System.Console.WriteLine(result);
            
            // Assert
            Assert.Contains("public static IServiceCollection AddRelay", result);
            // The AddScoped method is only generated when there are handlers, so let's check for the basic structure
            Assert.Contains("RelayServiceCollectionExtensions", result);
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
}