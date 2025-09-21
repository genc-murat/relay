using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class EndpointMetadataGeneratorTests
    {
        [Fact]
        public void GenerateEndpointMetadata_WithExposeAsEndpointAttribute_GeneratesMetadata()
        {
            // Arrange
            var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(Route = ""/api/test"", HttpMethod = ""GET"")]
    public string HandleTest(TestRequest request)
    {
        return ""test"";
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            var context = new RelayCompilationContext(compilation, default);
            var discoveryEngine = new HandlerDiscoveryEngine(context);
            var diagnosticReporter = new TestDiagnosticReporter();

            // Get candidate methods
            var syntaxTrees = compilation.SyntaxTrees.ToList();
            var candidateMethods = syntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes())
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists.Count > 0)
                .ToList();

            var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);

            var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

            // Act
            var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("EndpointMetadataRegistry.RegisterEndpoint", result);
            Assert.Contains("Route = \"/api/test\"", result);
            Assert.Contains("HttpMethod = \"GET\"", result);
            Assert.Contains("RequestType = typeof(TestRequest)", result);
            Assert.Contains("ResponseType = typeof(string)", result);
        }

        [Fact]
        public void GenerateEndpointMetadata_WithDefaultRoute_GeneratesKebabCaseRoute()
        {
            // Arrange
            var source = @"
using Relay.Core;

public class CreateUserRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleCreateUser(CreateUserRequest request)
    {
        return ""created"";
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            var context = new RelayCompilationContext(compilation, default);
            var discoveryEngine = new HandlerDiscoveryEngine(context);
            var diagnosticReporter = new TestDiagnosticReporter();

            var syntaxTrees = compilation.SyntaxTrees.ToList();
            var candidateMethods = syntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes())
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists.Count > 0)
                .ToList();

            var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
            var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

            // Act
            var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Route = \"/create-user\"", result);
            Assert.Contains("HttpMethod = \"POST\"", result); // Default HTTP method
        }

        [Fact]
        public void GenerateEndpointMetadata_WithVersionedEndpoint_IncludesVersion()
        {
            // Arrange
            var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(Route = ""/api/test"", Version = ""v1"")]
    public string HandleTest(TestRequest request)
    {
        return ""test"";
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            var context = new RelayCompilationContext(compilation, default);
            var discoveryEngine = new HandlerDiscoveryEngine(context);
            var diagnosticReporter = new TestDiagnosticReporter();

            var syntaxTrees = compilation.SyntaxTrees.ToList();
            var candidateMethods = syntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes())
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists.Count > 0)
                .ToList();

            var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
            var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

            // Act
            var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Version = \"v1\"", result);
        }

        [Fact]
        public void GenerateEndpointMetadata_WithAsyncHandler_ExtractsResponseType()
        {
            // Arrange
            var source = @"
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public async Task<string> HandleTestAsync(TestRequest request)
    {
        return await Task.FromResult(""test"");
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            var context = new RelayCompilationContext(compilation, default);
            var discoveryEngine = new HandlerDiscoveryEngine(context);
            var diagnosticReporter = new TestDiagnosticReporter();

            var syntaxTrees = compilation.SyntaxTrees.ToList();
            var candidateMethods = syntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes())
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists.Count > 0)
                .ToList();

            var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
            var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

            // Act
            var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("ResponseType = typeof(string)", result);
        }

        [Fact]
        public void GenerateEndpointMetadata_WithNoEndpointAttributes_ReturnsEmpty()
        {
            // Arrange
            var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public string HandleTest(TestRequest request)
    {
        return ""test"";
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            var context = new RelayCompilationContext(compilation, default);
            var discoveryEngine = new HandlerDiscoveryEngine(context);
            var diagnosticReporter = new TestDiagnosticReporter();

            var syntaxTrees = compilation.SyntaxTrees.ToList();
            var candidateMethods = syntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes())
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists.Count > 0)
                .ToList();

            var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
            var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

            // Act
            var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GenerateEndpointMetadata_WithComplexRequestType_GeneratesJsonSchema()
        {
            // Arrange
            var source = @"
using Relay.Core;

public class CreateUserRequest : IRequest<UserResponse>
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public UserResponse HandleCreateUser(CreateUserRequest request)
    {
        return new UserResponse { Id = 1, Name = request.Name };
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            var context = new RelayCompilationContext(compilation, default);
            var discoveryEngine = new HandlerDiscoveryEngine(context);
            var diagnosticReporter = new TestDiagnosticReporter();

            var syntaxTrees = compilation.SyntaxTrees.ToList();
            var candidateMethods = syntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes())
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists.Count > 0)
                .ToList();

            var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);
            var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

            // Act
            var result = generator.GenerateEndpointMetadata(discoveryResult.Handlers);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("RequestSchema = new JsonSchemaContract", result);
            Assert.Contains("ResponseSchema = new JsonSchemaContract", result);
            Assert.Contains("RequestType = typeof(CreateUserRequest)", result);
            Assert.Contains("ResponseType = typeof(UserResponse)", result);
        }
    }
}