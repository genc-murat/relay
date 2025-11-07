using Xunit;
using Microsoft.CodeAnalysis;
using Moq;
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
    [InlineData("DIRegistration", "DIRegistration")]
    [InlineData("XMLParser", "XmlParser")]
    [InlineData("DI", "DI")]
    [InlineData("a", "A")]
    [InlineData("A", "A")]
    [InlineData("ab", "Ab")]
    [InlineData("AB", "AB")]
    [InlineData("aB", "AB")]
    [InlineData("Ab", "Ab")]
    [InlineData("test_case", "TestCase")]
    [InlineData("test-case", "TestCase")]
    [InlineData("test case", "TestCase")]
    [InlineData("test123", "Test123")]
    [InlineData("123test", "123test")]
    [InlineData("_test", "Test")]
    [InlineData("-test", "Test")]
    [InlineData("test_", "Test")]
    [InlineData("test-", "Test")]
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
    [InlineData("DIRegistration", "dIRegistration")]
    [InlineData("XMLParser", "xmlParser")]
    [InlineData("DI", "dI")]
    [InlineData("A", "a")]
    [InlineData("AB", "aB")]
    [InlineData("a", "a")]
    [InlineData("test_case", "testCase")]
    [InlineData("test-case", "testCase")]
    [InlineData("test case", "testCase")]
    [InlineData("Test123", "test123")]
    [InlineData("123Test", "123Test")]
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
    [InlineData("DIRegistration", "_dIRegistration")]
    [InlineData("XMLParser", "_xmlParser")]
    [InlineData("A", "_a")]
    [InlineData("test_case", "_testCase")]
    [InlineData("test-case", "_testCase")]
    [InlineData("test case", "_testCase")]
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
    [InlineData("I", "i")]
    [InlineData("IClass", "class")]
    [InlineData("IInterface", "interface")]
    [InlineData("MyClass<T,K>", "myClass")]
    [InlineData("MyClass< T >", "myClass")]
    [InlineData("DIRegistration", "dIRegistration")]
    [InlineData("XMLParser", "xmlParser")]
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
    [InlineData("IClass", "IClass")]
    [InlineData("I", "I")]
    [InlineData("Interface", "Interface")]
    [InlineData("diRegistration", "IDiRegistration")]
    [InlineData("xmlParser", "IXmlParser")]
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
    [InlineData("DIRegistration", "DIRegistrationTests")]
    [InlineData("XMLParser", "XmlParserTests")]
    [InlineData("test_case", "TestCaseTests")]
    [InlineData("test-case", "TestCaseTests")]
    [InlineData("test case", "TestCaseTests")]
    public void ToTestClassName_AppendsTestsSuffix(string input, string expected)
    {
        // Act
        var result = NamingHelper.ToTestClassName(input);

        // Assert
        Assert.Equal(expected, result);
    }

[Theory]
    [InlineData("Validate", "WithValidHandler", "ReturnsSuccess", "Validate_WithValidHandler_ReturnsSuccess")]
    [InlineData("validate", "with_valid_handler", "returns_success", "Validate_WithValidHandler_ReturnsSuccess")]
    [InlineData("validate", "with-valid-handler", "returns-success", "Validate_WithValidHandler_ReturnsSuccess")]
    [InlineData("validate", "with valid handler", "returns success", "Validate_WithValidHandler_ReturnsSuccess")]
    [InlineData("DIRegistration", "WithValidHandler", "ReturnsSuccess", "DIRegistration_WithValidHandler_ReturnsSuccess")]
    [InlineData("XMLParser", "WithValidInput", "ReturnsParsed", "XmlParser_WithValidInput_ReturnsParsed")]
    public void ToTestMethodName_GeneratesCorrectFormat(string method, string scenario, string result, string expected)
    {
        // Act
        var actual = NamingHelper.ToTestMethodName(method, scenario, result);

        // Assert
        Assert.Equal(expected, actual);
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
    [InlineData("DIRegistration", "GeneratedDIRegistration")]
    [InlineData("XMLParser", "GeneratedXmlParser")]
    [InlineData("test_case", "GeneratedTestCase")]
    [InlineData("test-case", "GeneratedTestCase")]
    [InlineData("test case", "GeneratedTestCase")]
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
    public void ToPascalCase_WithNull_ReturnsNull()
    {
        // Act
        var result = NamingHelper.ToPascalCase(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToPascalCase_WithWhiteSpace_ReturnsWhiteSpace()
    {
        // Act
        var result = NamingHelper.ToPascalCase("   ");

        // Assert
        Assert.Equal("   ", result);
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
    public void ToCamelCase_WithNull_ReturnsNull()
    {
        // Act
        var result = NamingHelper.ToCamelCase(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToCamelCase_WithWhiteSpace_ReturnsWhiteSpace()
    {
        // Act
        var result = NamingHelper.ToCamelCase("   ");

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void ToPrivateFieldName_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = NamingHelper.ToPrivateFieldName("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ToPrivateFieldName_WithNull_ReturnsNull()
    {
        // Act
        var result = NamingHelper.ToPrivateFieldName(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToPrivateFieldName_WithWhiteSpace_ReturnsWhiteSpace()
    {
        // Act
        var result = NamingHelper.ToPrivateFieldName("   ");

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void ToParameterName_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = NamingHelper.ToParameterName("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ToParameterName_WithNull_ReturnsNull()
    {
        // Act
        var result = NamingHelper.ToParameterName(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToParameterName_WithWhiteSpace_ReturnsWhiteSpace()
    {
        // Act
        var result = NamingHelper.ToParameterName("   ");

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void ToGeneratorName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToGeneratorName(null!));
    }

    [Fact]
    public void ToGeneratorName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToGeneratorName(""));
    }

    [Fact]
    public void ToGeneratorName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToGeneratorName("   "));
    }

    [Fact]
    public void ToValidatorName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToValidatorName(null!));
    }

    [Fact]
    public void ToValidatorName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToValidatorName(""));
    }

    [Fact]
    public void ToValidatorName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToValidatorName("   "));
    }

    [Fact]
    public void ToHelperName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToHelperName(null!));
    }

    [Fact]
    public void ToHelperName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToHelperName(""));
    }

    [Fact]
    public void ToHelperName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToHelperName("   "));
    }

    [Fact]
    public void ToExtensionClassName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToExtensionClassName(null!));
    }

    [Fact]
    public void ToExtensionClassName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToExtensionClassName(""));
    }

    [Fact]
    public void ToExtensionClassName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToExtensionClassName("   "));
    }

    [Fact]
    public void ToServiceName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToServiceName(null!));
    }

    [Fact]
    public void ToServiceName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToServiceName(""));
    }

    [Fact]
    public void ToServiceName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToServiceName("   "));
    }

    [Fact]
    public void ToContextName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToContextName(null!));
    }

    [Fact]
    public void ToContextName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToContextName(""));
    }

    [Fact]
    public void ToContextName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToContextName("   "));
    }

    [Fact]
    public void ToResultName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToResultName(null!));
    }

    [Fact]
    public void ToResultName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToResultName(""));
    }

    [Fact]
    public void ToResultName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToResultName("   "));
    }

    [Fact]
    public void ToInfoName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToInfoName(null!));
    }

    [Fact]
    public void ToInfoName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToInfoName(""));
    }

    [Fact]
    public void ToInfoName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToInfoName("   "));
    }

    [Fact]
    public void ToInterfaceName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToInterfaceName(null!));
    }

    [Fact]
    public void ToInterfaceName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToInterfaceName(""));
    }

    [Fact]
    public void ToInterfaceName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToInterfaceName("   "));
    }

    [Fact]
    public void ToTestClassName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestClassName(null!));
    }

    [Fact]
    public void ToTestClassName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestClassName(""));
    }

    [Fact]
    public void ToTestClassName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestClassName("   "));
    }

    [Fact]
    public void ToTestMethodName_WithNullMethod_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName(null!, "scenario", "result"));
    }

    [Fact]
    public void ToTestMethodName_WithNullScenario_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName("method", null!, "result"));
    }

    [Fact]
    public void ToTestMethodName_WithNullResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName("method", "scenario", null!));
    }

    [Fact]
    public void ToTestMethodName_WithEmptyMethod_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName("", "scenario", "result"));
    }

    [Fact]
    public void ToTestMethodName_WithEmptyScenario_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName("method", "", "result"));
    }

    [Fact]
    public void ToTestMethodName_WithEmptyResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName("method", "scenario", ""));
    }

    [Fact]
    public void ToTestMethodName_WithWhiteSpaceMethod_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName("   ", "scenario", "result"));
    }

    [Fact]
    public void ToTestMethodName_WithWhiteSpaceScenario_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName("method", "   ", "result"));
    }

    [Fact]
    public void ToTestMethodName_WithWhiteSpaceResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToTestMethodName("method", "scenario", "   "));
    }

    [Fact]
    public void ToDiagnosticId_WithNegativeNumber_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => NamingHelper.ToDiagnosticId(-1));
    }

    [Fact]
    public void ToDiagnosticId_WithZero_ReturnsCorrectFormat()
    {
        // Act
        var result = NamingHelper.ToDiagnosticId(0);

        // Assert
        Assert.Equal("RELAY_GEN_000", result);
    }

    [Fact]
    public void ToGeneratedClassName_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToGeneratedClassName(null!));
    }

    [Fact]
    public void ToGeneratedClassName_WithEmptyString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToGeneratedClassName(""));
    }

    [Fact]
    public void ToGeneratedClassName_WithWhiteSpace_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.ToGeneratedClassName("   "));
    }

    [Fact]
    public void GetSafeIdentifier_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NamingHelper.GetSafeIdentifier(null!));
    }

    [Fact]
    public void GetSafeIdentifier_ReplacesSpecialCharacters()
    {
        // Arrange
        var symbol = Mock.Of<ISymbol>(s => s.Name == "MyClass<T,K>");

        // Act
        var result = NamingHelper.GetSafeIdentifier(symbol);

        // Assert
        Assert.Equal("MyClass_T_K_", result);
    }

    [Fact]
    public void GetSafeIdentifier_PrefixesWithUnderscoreWhenStartsWithDigit()
    {
        // Arrange
        var symbol = Mock.Of<ISymbol>(s => s.Name == "123Test");

        // Act
        var result = NamingHelper.GetSafeIdentifier(symbol);

        // Assert
        Assert.Equal("_123Test", result);
    }

    [Fact]
    public void GetSafeIdentifier_HandlesUnderscorePrefix()
    {
        // Arrange
        var symbol = Mock.Of<ISymbol>(s => s.Name == "_test");

        // Act
        var result = NamingHelper.GetSafeIdentifier(symbol);

        // Assert
        Assert.Equal("_test", result);
    }

    [Fact]
    public void GetSafeIdentifier_HandlesLetterStart()
    {
        // Arrange
        var symbol = Mock.Of<ISymbol>(s => s.Name == "Test");

        // Act
        var result = NamingHelper.GetSafeIdentifier(symbol);

        // Assert
        Assert.Equal("Test", result);
    }

    [Fact]
    public void GetSafeIdentifier_HandlesComplexGenericName()
    {
        // Arrange
        var symbol = Mock.Of<ISymbol>(s => s.Name == "Dictionary<string, List<int>>");

        // Act
        var result = NamingHelper.GetSafeIdentifier(symbol);

        // Assert
        Assert.Equal("Dictionary_string_ List_int__", result);
    }
}
