extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Relay.SourceGenerator.Tests;

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
        Assert.Equal(compilation, context.Compilation);
        Assert.Equal(cancellationToken, context.CancellationToken);
        Assert.Equal("TestAssembly", context.AssemblyName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Compilation_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new RelayCompilationContext(null!, default));
        Assert.Equal("compilation", exception.ParamName);
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
    Assert.NotNull(semanticModel);
    Assert.Equal(syntaxTree, semanticModel.SyntaxTree);
}

[Fact]
public void GetSemanticModel_WithInvalidSyntaxTree_ShouldStillReturnModel()
{
    // Arrange
    var invalidSource = "namespace Test { public class TestClass { invalid syntax } }";
    var syntaxTree = CSharpSyntaxTree.ParseText(invalidSource);
    var compilation = CreateTestCompilation("TestAssembly", syntaxTree);
    var context = new RelayCompilationContext(compilation, default);

    // Act
    var semanticModel = context.GetSemanticModel(syntaxTree);

    // Assert
    Assert.NotNull(semanticModel);
    Assert.Equal(syntaxTree, semanticModel.SyntaxTree);
    // Even with syntax errors, semantic model is created
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
        Assert.NotNull(objectType);
        Assert.Equal("Object", objectType!.Name);
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
        Assert.Null(nonExistentType);
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
        Assert.True(hasReference);
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
        Assert.False(hasReference);
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
        Assert.Equal("Unknown", context.AssemblyName);
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