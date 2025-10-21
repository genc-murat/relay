using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Relay.SourceGenerator.Validators;

namespace Relay.SourceGenerator.Tests;

public class ValidationHelperTests
{
    #region ReportDiagnostic Tests

    // Note: ReportDiagnostic methods are simple wrappers that create Diagnostic and call context.ReportDiagnostic.
    // Since SyntaxNodeAnalysisContext and CompilationAnalysisContext are structs and cannot be mocked with Moq,
    // these methods are tested indirectly through analyzer integration tests.

    #endregion

    #region TryGetDeclaredSymbol Tests

    [Fact]
    public void TryGetDeclaredSymbol_WithValidMethodDeclaration_ReturnsMethodSymbol()
    {
        // Arrange
        var code = @"
            public class TestClass
            {
                public void TestMethod() { }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var semanticModel = compilation.GetSemanticModel(tree);
        var methodDeclaration = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        // Act
        var result = ValidationHelper.TryGetDeclaredSymbol(semanticModel, methodDeclaration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestMethod", result.Name);
        Assert.Equal("void", result.ReturnType.ToDisplayString());
    }

    [Fact]
    public void TryGetDeclaredSymbol_WithInvalidSemanticModel_ReturnsNull()
    {
        // Arrange
        var code = @"
            public class TestClass
            {
                public void TestMethod() { }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var methodDeclaration = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        // Create a semantic model from a different compilation that won't match the syntax tree
        var differentCode = "public class Different { }";
        var differentTree = CSharpSyntaxTree.ParseText(differentCode);
        var differentCompilation = CSharpCompilation.Create("Different", new[] { differentTree });
        var semanticModel = differentCompilation.GetSemanticModel(differentTree);

        // Act
        var result = ValidationHelper.TryGetDeclaredSymbol(semanticModel, methodDeclaration);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryGetDeclaredSymbol_WithNullSemanticModel_ReturnsNull()
    {
        // Arrange
        var code = @"
            public class TestClass
            {
                public void TestMethod() { }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var methodDeclaration = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        // Act
        var result = ValidationHelper.TryGetDeclaredSymbol(null!, methodDeclaration);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region TryGetSemanticModel Tests

    [Fact]
    public void TryGetSemanticModel_WithValidCompilationAndTree_ReturnsSemanticModel()
    {
        // Arrange
        var code = "public class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", new[] { tree });

        // Act
        var result = ValidationHelper.TryGetSemanticModel(compilation, tree);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(compilation, result.Compilation);
    }

    [Fact]
    public void TryGetSemanticModel_WithInvalidCompilation_ReturnsNull()
    {
        // Arrange
        var code = "public class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code);

        // Create a compilation that doesn't contain the tree
        var differentCode = "public class Different { }";
        var differentTree = CSharpSyntaxTree.ParseText(differentCode);
        var compilation = CSharpCompilation.Create("Test", new[] { differentTree });

        // Act
        var result = ValidationHelper.TryGetSemanticModel(compilation, tree);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryGetSemanticModel_WithNullCompilation_ReturnsNull()
    {
        // Arrange
        var code = "public class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code);

        // Act
        var result = ValidationHelper.TryGetSemanticModel(null!, tree);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAttribute Tests

    [Fact]
    public void GetAttribute_WithMatchingAttribute_ReturnsAttributeData()
    {
        // Arrange
        var code = @"
            using System;
            public class TestClass
            {
                [Obsolete]
                public void TestMethod() { }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", new[] { tree },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var semanticModel = compilation.GetSemanticModel(tree);
        var methodDeclaration = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);

        // Act
        var result = ValidationHelper.GetAttribute(methodSymbol!, "System.ObsoleteAttribute");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("System.ObsoleteAttribute", result.AttributeClass?.ToDisplayString());
    }

    [Fact]
    public void GetAttribute_WithNonMatchingAttribute_ReturnsNull()
    {
        // Arrange
        var code = @"
            using System;
            public class TestClass
            {
                [Obsolete]
                public void TestMethod() { }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", new[] { tree },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var semanticModel = compilation.GetSemanticModel(tree);
        var methodDeclaration = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);

        // Act
        var result = ValidationHelper.GetAttribute(methodSymbol!, "System.SerializableAttribute");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAttribute_WithNoAttributes_ReturnsNull()
    {
        // Arrange
        var code = @"
            public class TestClass
            {
                public void TestMethod() { }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", new[] { tree });
        var semanticModel = compilation.GetSemanticModel(tree);
        var methodDeclaration = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);

        // Act
        var result = ValidationHelper.GetAttribute(methodSymbol!, "System.ObsoleteAttribute");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAttribute_WithExceptionDuringAttributeAccess_ReturnsNull()
    {
        // Arrange
        var mockMethodSymbol = new Mock<IMethodSymbol>();
        mockMethodSymbol.Setup(ms => ms.GetAttributes()).Throws(new Exception("Test exception"));

        // Act
        var result = ValidationHelper.GetAttribute(mockMethodSymbol.Object, "Test.Attribute");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAttribute_WithNullMethodSymbol_ReturnsNull()
    {
        // Act
        var result = ValidationHelper.GetAttribute(null!, "Test.Attribute");

        // Assert
        Assert.Null(result);
    }

    #endregion
}