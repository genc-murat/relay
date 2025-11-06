using System.Text;
using Xunit;
using Relay.SourceGenerator.Extensions;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for StringBuilderExtensions helper methods.
/// </summary>
public class StringBuilderExtensionsTests
{
    [Fact]
    public void AppendIndentedLine_WithZeroIndent_AppendsLineWithoutIndentation()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendIndentedLine(0, "test line");

        // Assert
        Assert.Equal("test line\r\n", builder.ToString());
    }

    [Fact]
    public void AppendIndentedLine_WithIndentLevel1_AppendsFourSpaces()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendIndentedLine(1, "test line");

        // Assert
        Assert.Equal("    test line\r\n", builder.ToString());
    }

    [Fact]
    public void AppendIndentedLine_WithIndentLevel2_AppendsEightSpaces()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendIndentedLine(2, "test line");

        // Assert
        Assert.Equal("        test line\r\n", builder.ToString());
    }

    [Fact]
    public void AppendIndentedLine_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendIndentedLine(0, "test"));
    }

    [Fact]
    public void AppendIndentedLine_WithNegativeIndentLevel_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AppendIndentedLine(-1, "test"));
    }

    [Fact]
    public void AppendIndentedLines_WithMultipleLines_AppendsAllWithIndentation()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendIndentedLines(1, "line1", "line2", "line3");

        // Assert
        var expected = "    line1\r\n    line2\r\n    line3\r\n";
        Assert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void AppendIndentedLines_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendIndentedLines(0, "line1"));
    }

    [Fact]
    public void AppendIndentedLines_WithNullLines_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AppendIndentedLines(0, null));
    }

    [Fact]
    public void AppendXmlSummary_WithSingleLine_GeneratesCorrectXmlDoc()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendXmlSummary(0, "This is a test summary");

        // Assert
        var result = builder.ToString();
        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// This is a test summary", result);
        Assert.Contains("/// </summary>", result);
    }

    [Fact]
    public void AppendXmlSummary_WithMultiLine_SplitsAndFormatsCorrectly()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendXmlSummary(0, "Line 1\nLine 2");

        // Assert
        var result = builder.ToString();
        Assert.Contains("/// Line 1", result);
        Assert.Contains("/// Line 2", result);
    }

    [Fact]
    public void AppendXmlSummary_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendXmlSummary(0, "summary"));
    }

    [Fact]
    public void AppendXmlParam_GeneratesCorrectParameterDoc()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendXmlParam(0, "paramName", "Parameter description");

        // Assert
        Assert.Equal("/// <param name=\"paramName\">Parameter description</param>\r\n", builder.ToString());
    }

    [Fact]
    public void AppendXmlParam_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendXmlParam(0, "param", "desc"));
    }

    [Fact]
    public void AppendXmlParam_WithEmptyParamName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AppendXmlParam(0, "", "desc"));
    }

    [Fact]
    public void AppendXmlReturns_GeneratesCorrectReturnsDoc()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendXmlReturns(0, "Return value description");

        // Assert
        Assert.Equal("/// <returns>Return value description</returns>\r\n", builder.ToString());
    }

    [Fact]
    public void AppendXmlReturns_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendXmlReturns(0, "desc"));
    }

    [Fact]
    public void AppendFileHeader_GeneratesStandardHeader()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendFileHeader("TestGenerator", includeTimestamp: false);

        // Assert
        var result = builder.ToString();
        Assert.Contains("// <auto-generated />", result);
        Assert.Contains("// Generated by TestGenerator", result);
    }

    [Fact]
    public void AppendFileHeader_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendFileHeader("gen"));
    }

    [Fact]
    public void AppendNamespaceStart_GeneratesNamespaceDeclaration()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendNamespaceStart("Test.Namespace");

        // Assert
        var expected = "namespace Test.Namespace\r\n{\r\n";
        Assert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void AppendNamespaceStart_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendNamespaceStart("ns"));
    }

    [Fact]
    public void AppendNamespaceStart_WithEmptyNamespaceName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AppendNamespaceStart(""));
    }

    [Fact]
    public void AppendMethodEnd_GeneratesClosingBrace()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendMethodEnd(2);

        // Assert
        Assert.Equal("        }\r\n", builder.ToString());
    }

    [Fact]
    public void AppendMethodEnd_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendMethodEnd(0));
    }

    [Fact]
    public void AppendClassEnd_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendClassEnd(0));
    }

    [Fact]
    public void AppendNamespaceEnd_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendNamespaceEnd());
    }

    [Fact]
    public void AppendClassStart_WithNoBaseTypes_GeneratesSimpleClass()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendClassStart(1, "public", "MyClass");

        // Assert
        var expected = "    public class MyClass\r\n    {\r\n";
        Assert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void AppendClassStart_WithBaseTypes_GeneratesClassWithInheritance()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendClassStart(1, "public", "MyClass", null, "BaseClass", "IInterface");

        // Assert
        var result = builder.ToString();
        Assert.Contains("public class MyClass : BaseClass, IInterface", result);
    }

    [Fact]
    public void AppendClassStart_WithModifiers_GeneratesClassWithModifiers()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendClassStart(1, "public", "MyClass", "sealed");

        // Assert
        var result = builder.ToString();
        Assert.Contains("public sealed class MyClass", result);
    }

    [Fact]
    public void AppendClassStart_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendClassStart(0, "public", "Class"));
    }

    [Fact]
    public void AppendClassStart_WithEmptyClassName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AppendClassStart(0, "public", ""));
    }

    [Fact]
    public void AppendMethodStart_GeneratesMethodDeclaration()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendMethodStart(2, "public", "void", "MyMethod", "string param");

        // Assert
        var result = builder.ToString();
        Assert.Contains("public void MyMethod(string param)", result);
        Assert.Contains("{", result);
    }

    [Fact]
    public void AppendMethodStart_WithModifiers_GeneratesMethodWithModifiers()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendMethodStart(2, "public", "Task", "MyMethodAsync", "", "async");

        // Assert
        var result = builder.ToString();
        Assert.Contains("public async Task MyMethodAsync()", result);
    }

    [Fact]
    public void AppendMethodStart_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendMethodStart(0, "public", "void", "Method"));
    }

    [Fact]
    public void AppendMethodStart_WithEmptyMethodName_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AppendMethodStart(0, "public", "void", ""));
    }

    [Fact]
    public void AppendUsings_GeneratesOrderedUsingDirectives()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendUsings("System.Linq", "System", "System.Collections.Generic");

        // Assert
        var result = builder.ToString();
        var lines = result.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("using System;", lines[0]);
        Assert.Equal("using System.Collections.Generic;", lines[1]);
        Assert.Equal("using System.Linq;", lines[2]);
    }

    [Fact]
    public void AppendUsings_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendUsings("System"));
    }

    [Fact]
    public void AppendNullableDirective_WithEnable_GeneratesEnableDirective()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendNullableDirective(true);

        // Assert
        Assert.Contains("#nullable enable", builder.ToString());
    }

    [Fact]
    public void AppendNullableDirective_WithDisable_GeneratesDisableDirective()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendNullableDirective(false);

        // Assert
        Assert.Contains("#nullable disable", builder.ToString());
    }

    [Fact]
    public void AppendNullableDirective_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((StringBuilder)null).AppendNullableDirective(true));
    }

    [Fact]
    public void ChainedCalls_WorkCorrectly()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AppendFileHeader("TestGen", false)
               .AppendUsings("System")
               .AppendNamespaceStart("Test")
               .AppendClassStart(1, "public", "TestClass")
               .AppendMethodStart(2, "public", "void", "Test")
               .AppendIndentedLine(3, "// Implementation")
               .AppendMethodEnd(2)
               .AppendClassEnd(1)
               .AppendNamespaceEnd();

        // Assert
        var result = builder.ToString();
        Assert.Contains("// <auto-generated />", result);
        Assert.Contains("using System;", result);
        Assert.Contains("namespace Test", result);
        Assert.Contains("public class TestClass", result);
        Assert.Contains("public void Test()", result);
        Assert.Contains("// Implementation", result);
    }
}
