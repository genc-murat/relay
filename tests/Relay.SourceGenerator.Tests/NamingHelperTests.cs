using Xunit;
using Relay.SourceGenerator.Helpers;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for NamingHelper utility methods.
/// </summary>
public class NamingHelperTests
{
    [Theory]
    [InlineData("hello world", "HelloWorld")]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("hello-world", "HelloWorld")]
    [InlineData("helloWorld", "HelloWorld")]
    [InlineData("HelloWorld", "HelloWorld")]
    [InlineData("HELLO_WORLD", "HelloWorld")]
    public void ToPascalCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToPascalCase(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("HelloWorld", "helloWorld")]
    [InlineData("hello world", "helloWorld")]
    [InlineData("hello_world", "helloWorld")]
    [InlineData("HELLO", "hello")]
    public void ToCamelCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToCamelCase(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Context", "_context")]
    [InlineData("MyProperty", "_myProperty")]
    [InlineData("handler", "_handler")]
    public void ToPrivateFieldName_AddsUnderscorePrefix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToPrivateFieldName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("IHandlerValidator", "handlerValidator")]
    [InlineData("HandlerValidator", "handlerValidator")]
    [InlineData("IService", "service")]
    [InlineData("MyClass<T>", "myClass")]
    public void ToParameterName_RemovesInterfacePrefixAndConverts(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToParameterName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("DIRegistration", "DIRegistrationGenerator")]
    [InlineData("OptimizedDispatcher", "OptimizedDispatcherGenerator")]
    [InlineData("HandlerGenerator", "HandlerGenerator")]
    public void ToGeneratorName_AppendsGeneratorSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToGeneratorName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Handler", "HandlerValidator")]
    [InlineData("Attribute", "AttributeValidator")]
    [InlineData("HandlerValidator", "HandlerValidator")]
    public void ToValidatorName_AppendsValidatorSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToValidatorName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Type", "TypeHelper")]
    [InlineData("CodeGeneration", "CodeGenerationHelper")]
    [InlineData("NamingHelper", "NamingHelper")]
    public void ToHelperName_AppendsHelperSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToHelperName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("StringBuilder", "StringBuilderExtensions")]
    [InlineData("Symbol", "SymbolExtensions")]
    [InlineData("StringExtensions", "StringExtensions")]
    public void ToExtensionClassName_AppendsExtensionsSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToExtensionClassName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Diagnostic", "DiagnosticService")]
    [InlineData("Validation", "ValidationService")]
    [InlineData("HandlerService", "HandlerService")]
    public void ToServiceName_AppendsServiceSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToServiceName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Compilation", "CompilationContext")]
    [InlineData("CodeGeneration", "CodeGenerationContext")]
    [InlineData("RelayContext", "RelayContext")]
    public void ToContextName_AppendsContextSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToContextName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Validation", "ValidationResult")]
    [InlineData("HandlerDiscovery", "HandlerDiscoveryResult")]
    [InlineData("DiscoveryResult", "DiscoveryResult")]
    public void ToResultName_AppendsResultSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToResultName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Handler", "HandlerInfo")]
    [InlineData("Attribute", "AttributeInfo")]
    [InlineData("HandlerInfo", "HandlerInfo")]
    public void ToInfoName_AppendsInfoSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToInfoName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("CodeGenerator", "ICodeGenerator")]
    [InlineData("IValidator", "IValidator")]
    [InlineData("Service", "IService")]
    public void ToInterfaceName_AddsInterfacePrefix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToInterfaceName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("HandlerValidator", "HandlerValidatorTests")]
    [InlineData("StringBuilderExtensions", "StringBuilderExtensionsTests")]
    [InlineData("MyClassTests", "MyClassTests")]
    public void ToTestClassName_AppendsTestsSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToTestClassName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToTestMethodName_GeneratesCorrectFormat()
    {
        // Act
        var result = NamingHelper.ToTestMethodName(
            "Validate",
            "WithValidHandler",
            "ReturnsSuccess");

        // Assert
        Assert.Equal("Validate_WithValidHandler_ReturnsSuccess", result);
    }

    [Theory]
    [InlineData(1, "RELAY_GEN_001")]
    [InlineData(42, "RELAY_GEN_042")]
    [InlineData(999, "RELAY_GEN_999")]
    public void ToDiagnosticId_GeneratesCorrectFormat(int number, string expected)
    {
        // Act
        var result = NamingHelper.ToDiagnosticId(number);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("RequestDispatcher", "GeneratedRequestDispatcher")]
    [InlineData("GeneratedHandler", "GeneratedHandler")]
    [InlineData("NotificationDispatcher", "GeneratedNotificationDispatcher")]
    public void ToGeneratedClassName_AddsGeneratedPrefix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToGeneratedClassName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToPascalCase_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = NamingHelper.ToPascalCase("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ToCamelCase_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = NamingHelper.ToCamelCase("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ToGeneratorName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToGeneratorName(null!));
    }

    [Fact]
    public void ToValidatorName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToValidatorName(null!));
    }

    [Fact]
    public void ToDiagnosticId_WithNegativeNumber_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => NamingHelper.ToDiagnosticId(-1));
    }
}
