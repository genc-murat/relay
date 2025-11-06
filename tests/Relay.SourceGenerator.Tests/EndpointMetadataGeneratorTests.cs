using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Discovery;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

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
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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

    [Fact]
    public void EndpointMetadataGenerator_Properties_ReturnCorrectValues()
    {
        // Arrange
        var (compilation, _) = TestHelpers.CreateCompilation("");
        var diagnosticReporter = new TestDiagnosticReporter();
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act & Assert
        Assert.Equal("Endpoint Metadata Generator", generator.GeneratorName);
        Assert.Equal("GeneratedEndpointMetadata", generator.OutputFileName);
        Assert.Equal(60, generator.Priority);
    }

    [Fact]
    public void EndpointMetadataGenerator_CanGenerate_ReturnsTrue_WhenHasEndpointHandlers()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleTest(TestRequest request) => ""test"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        var canGenerate = generator.CanGenerate(discoveryResult);

        // Assert
        Assert.True(canGenerate);
    }

    [Fact]
    public void EndpointMetadataGenerator_CanGenerate_ReturnsFalse_WhenNoEndpointHandlers()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public string HandleTest(TestRequest request) => ""test"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        var canGenerate = generator.CanGenerate(discoveryResult);

        // Assert
        Assert.False(canGenerate);
    }

    [Fact]
    public void EndpointMetadataGenerator_CanGenerate_ReturnsFalse_WhenNullResult()
    {
        // Arrange
        var (compilation, _) = TestHelpers.CreateCompilation("");
        var diagnosticReporter = new TestDiagnosticReporter();
        var generator = new EndpointMetadataGenerator(compilation, diagnosticReporter);

        // Act
        var canGenerate = generator.CanGenerate(null!);

        // Assert
        Assert.False(canGenerate);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithVoidReturnType_ExcludesResponseType()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public void HandleTest(TestRequest request) { }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("RequestType = typeof(TestRequest)", result);
        Assert.DoesNotContain("ResponseType =", result);
        Assert.DoesNotContain("ResponseSchema =", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithQuerySuffix_GeneratesCorrectRoute()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class GetUserQuery : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleGetUser(GetUserQuery request) => ""user"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("Route = \"/get-user\"", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithCommandSuffix_GeneratesCorrectRoute()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class CreateUserCommand : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleCreateUser(CreateUserCommand request) => ""created"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
    }

    [Fact]
    public void GenerateEndpointMetadata_WithDifferentHttpMethods_GeneratesCorrectMetadata()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(Route = ""/api/test"", HttpMethod = ""PUT"")]
    public string HandlePut(TestRequest request) => ""put"";

    [Handle]
    [ExposeAsEndpoint(Route = ""/api/test"", HttpMethod = ""DELETE"")]
    public string HandleDelete(TestRequest request) => ""delete"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("HttpMethod = \"PUT\"", result);
        Assert.Contains("HttpMethod = \"DELETE\"", result);
        Assert.Contains("HandlerMethodName = \"HandlePut\"", result);
        Assert.Contains("HandlerMethodName = \"HandleDelete\"", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithStringEscaping_HandlesSpecialCharacters()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(Route = ""/api/test\\with\\backslashes"", HttpMethod = ""POST"")]
    public string HandleTest(TestRequest request) => ""test"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
            Assert.Contains("Route = \"/api/test\\\\with\\\\backslashes\"", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithValueTask_ReturnsCorrectResponseType()
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
    public ValueTask<string> HandleTestAsync(TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        public void GenerateEndpointMetadata_WithGenericTypes_HandlesComplexTypes()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
using Relay.Core;

public class SearchRequest : IRequest<List<string>>
{
    public string Query { get; set; }
}

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public List<string> HandleSearch(SearchRequest request)
    {
        return new List<string> { ""result1"", ""result2"" };
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
            Assert.Contains("RequestType = typeof(SearchRequest)", result);
            Assert.Contains("ResponseType = typeof(List<string>)", result);
        }

        [Fact]
        public void GenerateEndpointMetadata_WithNestedGenericTypes_HandlesComplexTypes()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
using Relay.Core;

public class NestedRequest : IRequest<Dictionary<string, List<int>>>
{
    public string Key { get; set; }
}

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public Dictionary<string, List<int>> HandleNested(NestedRequest request)
    {
        return new Dictionary<string, List<int>>();
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
            Assert.Contains("RequestType = typeof(NestedRequest)", result);
            Assert.Contains("ResponseType = typeof(Dictionary<string, List<int>>)", result);
        }

        [Fact]
        public void GenerateEndpointMetadata_WithCustomClassResponse_GeneratesJsonSchema()
        {
            // Arrange
            var source = @"
using Relay.Core;

public class CustomRequest : IRequest<CustomResponse> { }

public class CustomResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public CustomResponse HandleCustom(CustomRequest request)
    {
        return new CustomResponse { Id = 1, Name = ""test"", IsActive = true };
    }
}";

            var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
            Assert.Contains("RequestType = typeof(CustomRequest)", result);
            Assert.Contains("ResponseType = typeof(CustomResponse)", result);
            Assert.Contains("RequestSchema =", result);
            Assert.Contains("ResponseSchema =", result);
        }

    [Fact]
    public void GenerateEndpointMetadata_IncludesNamespaceAndUsings()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleTest(TestRequest request) => ""test"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("namespace Relay.Generated", result);
        Assert.Contains("using System;", result);
        Assert.Contains("using System.Collections.Generic;", result);
        Assert.Contains("using Relay.Core;", result);
        Assert.Contains("internal static class GeneratedEndpointMetadata", result);
    }


    [Fact]
    public void GenerateEndpointMetadata_WithNoSuffixInRequestType_GeneratesCorrectRoute()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class User : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public string HandleUser(User request) => ""user"";
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("Route = \"/user\"", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithAsyncVoidReturn_ExcludesResponseType()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint]
    public async Task HandleTestAsync(TestRequest request)
    {
        await Task.CompletedTask;
    }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("RequestType = typeof(TestRequest)", result);
        Assert.DoesNotContain("ResponseType =", result);
        Assert.DoesNotContain("ResponseSchema =", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithConstructorRouteArgument_UsesConstructorValue()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(""/api/constructor-route"")]
    public string HandleTest(TestRequest request)
    {
        return ""test"";
    }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("Route = \"/api/constructor-route\"", result);
        Assert.Contains("HttpMethod = \"POST\"", result); // Default value
    }

    [Fact]
    public void GenerateEndpointMetadata_WithConstructorRouteAndHttpMethodArguments_UsesConstructorValues()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(""/api/constructor-route"", ""PUT"")]
    public string HandleTest(TestRequest request)
    {
        return ""test"";
    }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("Route = \"/api/constructor-route\"", result);
        Assert.Contains("HttpMethod = \"PUT\"", result);
    }

    [Fact]
    public void GenerateEndpointMetadata_WithConstructorArgumentsAndNamedArguments_NamedArgumentsTakePrecedence()
    {
        // Arrange
        var source = @"
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [ExposeAsEndpoint(""/api/constructor-route"", ""PUT"", Version = ""v2"")]
    public string HandleTest(TestRequest request)
    {
        return ""test"";
    }
}";

        var (compilation, diagnostics) = TestHelpers.CreateCompilation(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

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
        Assert.Contains("Route = \"/api/constructor-route\"", result);
        Assert.Contains("HttpMethod = \"PUT\"", result);
        Assert.Contains("Version = \"v2\"", result);
    }
}