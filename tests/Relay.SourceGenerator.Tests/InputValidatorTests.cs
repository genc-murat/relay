using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Relay.SourceGenerator;
using Relay.SourceGenerator.Core;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Discovery;
using Relay.SourceGenerator.Generators;
using Relay.SourceGenerator.Validation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Unit tests for InputValidator methods.
/// Tests all validation methods for input data robustness.
/// </summary>
public class InputValidatorTests
{
    #region ValidateHandlerClass Tests

    [Fact]
    public void ValidateHandlerClass_WithNullHandlerClass_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();

        // Act
        var result = InputValidator.ValidateHandlerClass(null, diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateHandlerClass_WithNullClassSymbol_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var handlerClass = new HandlerClassInfo
        {
            ClassSymbol = null,
            ClassDeclaration = SyntaxFactory.ClassDeclaration("TestClass"),
            ImplementedInterfaces = new List<HandlerInterfaceInfo>()
        };

        // Act
        var result = InputValidator.ValidateHandlerClass(handlerClass, diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.GeneratorError &&
            d.GetMessage().Contains("Invalid handler class symbol"))), Times.Once);
    }

    [Fact]
    public void ValidateHandlerClass_WithNullClassDeclaration_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var compilation = TestCompilationHelper.CreateCompilation("class TestClass {}");
        var classSymbol = compilation.GetTypeByMetadataName("TestClass");
        var handlerClass = new HandlerClassInfo
        {
            ClassSymbol = classSymbol,
            ClassDeclaration = null,
            ImplementedInterfaces = new List<HandlerInterfaceInfo>()
        };

        // Act
        var result = InputValidator.ValidateHandlerClass(handlerClass, diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.GeneratorError &&
            d.GetMessage().Contains("Invalid handler class declaration"))), Times.Once);
    }

    [Fact]
    public void ValidateHandlerClass_WithNullImplementedInterfaces_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var compilation = TestCompilationHelper.CreateCompilation("class TestClass {}");
        var classSymbol = compilation.GetTypeByMetadataName("TestClass");
        var handlerClass = new HandlerClassInfo
        {
            ClassSymbol = classSymbol,
            ClassDeclaration = SyntaxFactory.ClassDeclaration("TestClass"),
            ImplementedInterfaces = null
        };

        // Act
        var result = InputValidator.ValidateHandlerClass(handlerClass, diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateHandlerClass_WithEmptyImplementedInterfaces_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var compilation = TestCompilationHelper.CreateCompilation("class TestClass {}");
        var classSymbol = compilation.GetTypeByMetadataName("TestClass");
        var handlerClass = new HandlerClassInfo
        {
            ClassSymbol = classSymbol,
            ClassDeclaration = SyntaxFactory.ClassDeclaration("TestClass"),
            ImplementedInterfaces = new List<HandlerInterfaceInfo>()
        };

        // Act
        var result = InputValidator.ValidateHandlerClass(handlerClass, diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateHandlerClass_WithValidHandlerClass_ReturnsTrue()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var source = @"
using System;
namespace TestNamespace
{
    public interface IRequestHandler<T> {}
    public class TestRequest {}
    public class TestHandler : IRequestHandler<TestRequest>
    {
        public TestRequest Handle(TestRequest request) { return request; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var classSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestHandler");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestNamespace.IRequestHandler`1");
        var requestType = compilation.GetTypeByMetadataName("TestNamespace.TestRequest");

        var handlerInterface = new HandlerInterfaceInfo
        {
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = interfaceSymbol,
            RequestType = requestType,
            ResponseType = requestType
        };

        var handlerClass = new HandlerClassInfo
        {
            ClassSymbol = classSymbol,
            ClassDeclaration = SyntaxFactory.ClassDeclaration("TestHandler"),
            ImplementedInterfaces = new List<HandlerInterfaceInfo> { handlerInterface }
        };

        // Act
        var result = InputValidator.ValidateHandlerClass(handlerClass, diagnosticReporter.Object);

        // Assert
        Assert.True(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    #endregion

    #region ValidateAndFilterHandlerClasses Tests

    [Fact]
    public void ValidateAndFilterHandlerClasses_WithNullCollection_ReturnsEmptyList()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();

        // Act
        var result = InputValidator.ValidateAndFilterHandlerClasses(null, diagnosticReporter.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ValidateAndFilterHandlerClasses_WithEmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var handlerClasses = new List<HandlerClassInfo>();

        // Act
        var result = InputValidator.ValidateAndFilterHandlerClasses(handlerClasses, diagnosticReporter.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ValidateAndFilterHandlerClasses_WithMixedValidInvalidHandlers_ReturnsOnlyValid()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var compilation = TestCompilationHelper.CreateCompilation("class TestClass {}");
        var validClassSymbol = compilation.GetTypeByMetadataName("TestClass");
        var requestType = compilation.GetTypeByMetadataName("TestClass");

        var validInterface = new HandlerInterfaceInfo
        {
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = compilation.GetTypeByMetadataName("System.Object"), // Mock interface
            RequestType = requestType,
            ResponseType = requestType
        };

        var validHandler = new HandlerClassInfo
        {
            ClassSymbol = validClassSymbol,
            ClassDeclaration = SyntaxFactory.ClassDeclaration("TestClass"),
            ImplementedInterfaces = new List<HandlerInterfaceInfo> { validInterface }
        };

        var invalidHandler = new HandlerClassInfo
        {
            ClassSymbol = null,
            ClassDeclaration = SyntaxFactory.ClassDeclaration("InvalidClass"),
            ImplementedInterfaces = new List<HandlerInterfaceInfo>()
        };

        var handlerClasses = new List<HandlerClassInfo?> { validHandler, null, invalidHandler };

        // Act
        var result = InputValidator.ValidateAndFilterHandlerClasses(handlerClasses, diagnosticReporter.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(validHandler, result);
    }

    #endregion

    #region ValidateGenerationOptions Tests

    [Fact]
    public void ValidateGenerationOptions_WithNullOptions_ReturnsFalse()
    {
        // Act
        var result = InputValidator.ValidateGenerationOptions(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateGenerationOptions_WithValidOptions_ReturnsTrue()
    {
        // Arrange
        var options = new GenerationOptions
        {
            MaxDegreeOfParallelism = 4
        };

        // Act
        var result = InputValidator.ValidateGenerationOptions(options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateGenerationOptions_WithLowMaxDegreeOfParallelism_ClampsToMinimum()
    {
        // Arrange
        var options = new GenerationOptions
        {
            MaxDegreeOfParallelism = 0
        };

        // Act
        var result = InputValidator.ValidateGenerationOptions(options);

        // Assert
        Assert.True(result);
        Assert.Equal(1, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void ValidateGenerationOptions_WithHighMaxDegreeOfParallelism_ClampsToMaximum()
    {
        // Arrange
        var options = new GenerationOptions
        {
            MaxDegreeOfParallelism = 100
        };

        // Act
        var result = InputValidator.ValidateGenerationOptions(options);

        // Assert
        Assert.True(result);
        Assert.Equal(64, options.MaxDegreeOfParallelism);
    }

    #endregion

    #region ValidateCompilationContext Tests

    [Fact]
    public void ValidateCompilationContext_WithNullContext_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();

        // Act
        var result = InputValidator.ValidateCompilationContext(null, diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.GeneratorError &&
            d.GetMessage().Contains("Compilation context is null"))), Times.Once);
    }



    [Fact]
    public void ValidateCompilationContext_WithValidContext_ReturnsTrue()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var context = new RelayCompilationContext(compilation, default);

        // Act
        var result = InputValidator.ValidateCompilationContext(context, diagnosticReporter.Object);

        // Assert
        Assert.True(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    #endregion

    #region ValidateMethodSymbol Tests

    [Fact]
    public void ValidateMethodSymbol_WithNullMethodSymbol_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();

        // Act
        var result = InputValidator.ValidateMethodSymbol(null, "TestMethod", diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.GeneratorError &&
            d.GetMessage().Contains("Invalid method symbol"))), Times.Once);
    }

    [Fact]
    public void ValidateMethodSymbol_WithNullContainingType_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Mock a method symbol with null containing type (this is artificial for testing)
        var mockMethod = new Mock<IMethodSymbol>();
        mockMethod.Setup(m => m.ContainingType).Returns((INamedTypeSymbol)null);

        // Act
        var result = InputValidator.ValidateMethodSymbol(mockMethod.Object, "TestMethod", diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.GeneratorError &&
            d.GetMessage().Contains("has no containing type"))), Times.Once);
    }

    [Fact]
    public void ValidateMethodSymbol_WithValidMethodSymbol_ReturnsTrue()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = InputValidator.ValidateMethodSymbol(methodSymbol, "TestMethod", diagnosticReporter.Object);

        // Assert
        Assert.True(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    #endregion

    #region ValidateTypeSymbol Tests

    [Fact]
    public void ValidateTypeSymbol_WithNullTypeSymbol_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();

        // Act
        var result = InputValidator.ValidateTypeSymbol(null, "TestType", diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.GeneratorError &&
            d.GetMessage().Contains("Invalid type symbol"))), Times.Once);
    }

    [Fact]
    public void ValidateTypeSymbol_WithErrorType_ReturnsFalse()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var mockType = new Mock<ITypeSymbol>();
        mockType.Setup(t => t.TypeKind).Returns(TypeKind.Error);

        // Act
        var result = InputValidator.ValidateTypeSymbol(mockType.Object, "TestType", diagnosticReporter.Object);

        // Assert
        Assert.False(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.GeneratorError &&
            d.GetMessage().Contains("has errors and cannot be analyzed"))), Times.Once);
    }

    [Fact]
    public void ValidateTypeSymbol_WithValidTypeSymbol_ReturnsTrue()
    {
        // Arrange
        var diagnosticReporter = new Mock<IDiagnosticReporter>();
        var compilation = TestCompilationHelper.CreateCompilation("class TestClass {}");
        var typeSymbol = compilation.GetTypeByMetadataName("TestClass");

        // Act
        var result = InputValidator.ValidateTypeSymbol(typeSymbol, "TestClass", diagnosticReporter.Object);

        // Assert
        Assert.True(result);
        diagnosticReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    #endregion

    #region IsNullOrEmpty Tests

    [Fact]
    public void IsNullOrEmpty_WithNullCollection_ReturnsTrue()
    {
        // Act
        var result = InputValidator.IsNullOrEmpty<int>(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_WithEmptyCollection_ReturnsTrue()
    {
        // Arrange
        var collection = new List<int>();

        // Act
        var result = InputValidator.IsNullOrEmpty(collection);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_WithNonEmptyCollection_ReturnsFalse()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };

        // Act
        var result = InputValidator.IsNullOrEmpty(collection);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region SafeCount Tests

    [Fact]
    public void SafeCount_WithNullCollection_ReturnsZero()
    {
        // Act
        var result = InputValidator.SafeCount<int>(null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void SafeCount_WithEmptyCollection_ReturnsZero()
    {
        // Arrange
        var collection = new List<int>();

        // Act
        var result = InputValidator.SafeCount(collection);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void SafeCount_WithNonEmptyCollection_ReturnsCount()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var result = InputValidator.SafeCount(collection);

        // Assert
        Assert.Equal(5, result);
    }

    #endregion

    #region ValidateDiscoveryResult Tests

    [Fact]
    public void ValidateDiscoveryResult_WithNullResult_ReturnsFalse()
    {
        // Act
        var result = InputValidator.ValidateDiscoveryResult(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateDiscoveryResult_WithEmptyResult_ReturnsTrue()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();

        // Act
        var result = InputValidator.ValidateDiscoveryResult(discoveryResult);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateDiscoveryResult_WithPopulatedResult_ReturnsTrue()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        discoveryResult.Handlers.Add(new HandlerInfo());
        discoveryResult.NotificationHandlers.Add(new NotificationHandlerInfo());
        discoveryResult.PipelineBehaviors.Add(new PipelineBehaviorInfo());
        discoveryResult.StreamHandlers.Add(new StreamHandlerInfo());

        // Act
        var result = InputValidator.ValidateDiscoveryResult(discoveryResult);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region ValidateString Tests

    [Fact]
    public void ValidateString_WithNullString_ReturnsFalse()
    {
        // Act
        var result = InputValidator.ValidateString(null, "testParam");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateString_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = InputValidator.ValidateString("", "testParam");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateString_WithWhitespaceString_ReturnsFalse()
    {
        // Act
        var result = InputValidator.ValidateString("   ", "testParam");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateString_WithValidString_ReturnsTrue()
    {
        // Act
        var result = InputValidator.ValidateString("valid string", "testParam");

        // Assert
        Assert.True(result);
    }

    #endregion
}