using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for CodeGenerationContext class to ensure full coverage and proper functionality
/// </summary>
public class CodeGenerationContextTests
{
    #region Constructor Tests

    [Fact]
    public void CodeGenerationContext_Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();

        // Act
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Assert
        Assert.NotNull(context);
        Assert.Same(discoveryResult, context.DiscoveryResult);
        Assert.Same(options, context.Options);
        Assert.Same(compilationContext, context.CompilationContext);
    }

    [Fact]
    public void CodeGenerationContext_Constructor_WithNullDiscoveryResult_ThrowsArgumentNullException()
    {
        // Arrange
        GenerationOptions options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new CodeGenerationContext(null!, options, compilationContext));
        Assert.Equal("discoveryResult", exception.ParamName);
    }

    [Fact]
    public void CodeGenerationContext_Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var compilationContext = CreateCompilationContext();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new CodeGenerationContext(discoveryResult, null!, compilationContext));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void CodeGenerationContext_Constructor_WithNullCompilationContext_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new CodeGenerationContext(discoveryResult, options, null!));
        Assert.Equal("compilationContext", exception.ParamName);
    }

    #endregion

    #region GetData Tests

    [Fact]
    public void GetData_WithValidKeyAndExistingData_ReturnsCorrectValue()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);
        var testValue = "Test Value";
        context.SetData("testKey", testValue);

        // Act
        var result = context.GetData<string>("testKey");

        // Assert
        Assert.Equal(testValue, result);
    }

    [Fact]
    public void GetData_WithValidKeyAndDifferentType_ReturnsDefault()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);
        context.SetData("intKey", 42);

        // Act
        var result = context.GetData<string>("intKey");

        // Assert
        Assert.Null(result); // Default value for string is null
    }

    [Fact]
    public void GetData_WithNonExistingKey_ReturnsDefault()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act
        var result = context.GetData<string>("nonExistingKey");

        // Assert
        Assert.Null(result); // Default value for string is null
    }

    [Fact]
    public void GetData_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => context.GetData<string>(null!));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void GetData_WithEmptyKey_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => context.GetData<string>(""));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void GetData_WithWhitespaceKey_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => context.GetData<string>("   "));
        Assert.Equal("key", exception.ParamName);
    }

    #endregion

    #region SetData Tests

    [Fact]
    public void SetData_WithValidKeyAndValue_AddsToContext()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);
        var testValue = 42;

        // Act
        context.SetData("testKey", testValue);

        // Assert
        var retrieved = context.GetData<int>("testKey");
        Assert.Equal(testValue, retrieved);
    }

    [Fact]
    public void SetData_WithSameKey_OverwritesValue()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);
        context.SetData("testKey", 42);

        // Act
        context.SetData("testKey", 99);

        // Assert
        var retrieved = context.GetData<int>("testKey");
        Assert.Equal(99, retrieved);
    }

    [Fact]
    public void SetData_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => context.SetData<string>(null!, "someValue"));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void SetData_WithEmptyKey_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => context.SetData<string>("", "someValue"));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void SetData_WithWhitespaceKey_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => context.SetData<string>("   ", "someValue"));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void SetData_WithValueNull_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => context.SetData<string>("testKey", null!));
        Assert.Equal("value", exception.ParamName);
    }

    #endregion

    #region Data Storage Type Tests

    [Fact]
    public void SetData_And_GetData_WithVariousTypes_WorksCorrectly()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act & Assert - Test with int
        context.SetData("intValue", 42);
        Assert.Equal(42, context.GetData<int>("intValue"));

        // Test with string
        context.SetData("stringValue", "Hello");
        Assert.Equal("Hello", context.GetData<string>("stringValue"));

        // Test with boolean
        context.SetData("boolValue", true);
        Assert.Equal(true, context.GetData<bool>("boolValue"));

        // Test with custom object
        var handlerInfo = new HandlerInfo { HandlerName = "TestHandler" };
        context.SetData("handlerInfo", handlerInfo);
        Assert.Same(handlerInfo, context.GetData<HandlerInfo>("handlerInfo"));
    }

    [Fact]
    public void SetData_And_GetData_WithGenericTypes_WorksCorrectly()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions();
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);
        var complexData = new HandlerDiscoveryResult
        {
            // Add some handlers to verify complex data storage
        };

        // Act
        context.SetData("complexData", complexData);
        var retrieved = context.GetData<HandlerDiscoveryResult>("complexData");

        // Assert
        Assert.Same(complexData, retrieved);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CodeGenerationContext_FullRoundTrip_WithMultipleDataTypes()
    {
        // Arrange
        var discoveryResult = new HandlerDiscoveryResult();
        var options = new GenerationOptions { EnableHandlerRegistry = true };
        var compilationContext = CreateCompilationContext();
        var context = new CodeGenerationContext(discoveryResult, options, compilationContext);

        // Act - Store various types of data
        context.SetData("stringData", "test string");
        context.SetData("intData", 123);
        context.SetData("boolData", false);
        context.SetData("handlerData", new HandlerInfo { HandlerName = "IntegrationTestHandler" });

        // Retrieve all stored data
        var stringResult = context.GetData<string>("stringData");
        var intResult = context.GetData<int>("intData");
        var boolResult = context.GetData<bool>("boolData");
        var handlerResult = context.GetData<HandlerInfo>("handlerData");

        // Assert
        Assert.Equal("test string", stringResult);
        Assert.Equal(123, intResult);
        Assert.False(boolResult);
        Assert.NotNull(handlerResult);
        Assert.Equal("IntegrationTestHandler", handlerResult.HandlerName);
        Assert.Same(discoveryResult, context.DiscoveryResult);
        Assert.Same(options, context.Options);
        Assert.Same(compilationContext, context.CompilationContext);
    }

    #endregion

    private static RelayCompilationContext CreateCompilationContext()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("class Test {}");
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree);
        return new RelayCompilationContext(compilation, System.Threading.CancellationToken.None);
    }
}