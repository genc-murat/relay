using Xunit;
using Relay.SourceGenerator.Helpers;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for XmlDocumentationHelper utility methods.
/// </summary>
public class XmlDocumentationHelperTests
{
    [Fact]
    public void GenerateSummary_GeneratesCorrectXmlDoc()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateSummary("Test summary", 0);

        // Assert
        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// Test summary", result);
        Assert.Contains("/// </summary>", result);
    }

    [Fact]
    public void GenerateSummary_WithIndentation_AppliesIndent()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateSummary("Test", 1);

        // Assert
        Assert.Contains("    /// <summary>", result);
    }

    [Fact]
    public void GenerateMethodDocumentation_WithAllParameters_GeneratesComplete()
    {
        // Arrange
        var parameters = new[]
        {
            ("param1", "First parameter"),
            ("param2", "Second parameter")
        };

        // Act
        var result = XmlDocumentationHelper.GenerateMethodDocumentation(
            "Method summary",
            parameters,
            "Return value");

        // Assert
        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// Method summary", result);
        Assert.Contains("/// <param name=\"param1\">First parameter</param>", result);
        Assert.Contains("/// <param name=\"param2\">Second parameter</param>", result);
        Assert.Contains("/// <returns>Return value</returns>", result);
    }

    [Fact]
    public void GenerateMethodDocumentation_WithoutReturns_OmitsReturnsTag()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateMethodDocumentation("Summary");

        // Assert
        Assert.Contains("/// <summary>", result);
        Assert.DoesNotContain("<returns>", result);
    }

    [Fact]
    public void GeneratePropertyDocumentation_WithValue_IncludesValueTag()
    {
        // Act
        var result = XmlDocumentationHelper.GeneratePropertyDocumentation(
            "Property summary",
            "Property value description");

        // Assert
        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// Property summary", result);
        Assert.Contains("/// <value>Property value description</value>", result);
    }

    [Fact]
    public void GenerateClassDocumentation_WithRemarks_IncludesRemarksTag()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateClassDocumentation(
            "Class summary",
            "Additional remarks");

        // Assert
        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// Class summary", result);
        Assert.Contains("/// <remarks>", result);
        Assert.Contains("/// Additional remarks", result);
        Assert.Contains("/// </remarks>", result);
    }

    [Fact]
    public void GenerateException_GeneratesCorrectFormat()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateException(
            "ArgumentNullException",
            "Thrown when parameter is null");

        // Assert
        Assert.Contains("/// <exception cref=\"ArgumentNullException\">", result);
        Assert.Contains("/// Thrown when parameter is null", result);
        Assert.Contains("/// </exception>", result);
    }

    [Fact]
    public void GenerateExample_WithCodeBlock_GeneratesCorrectFormat()
    {
        // Arrange
        var code = "var x = 1;\nvar y = 2;";

        // Act
        var result = XmlDocumentationHelper.GenerateExample("Example usage", code);

        // Assert
        Assert.Contains("/// <example>", result);
        Assert.Contains("/// Example usage", result);
        Assert.Contains("/// <code>", result);
        Assert.Contains("/// var x = 1;", result);
        Assert.Contains("/// var y = 2;", result);
        Assert.Contains("/// </code>", result);
        Assert.Contains("/// </example>", result);
    }

    [Fact]
    public void GenerateSeeTag_GeneratesCorrectFormat()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateSeeTag("MyClass");

        // Assert
        Assert.Equal("<see cref=\"MyClass\"/>", result);
    }

    [Fact]
    public void GenerateSeeAlsoTag_GeneratesCorrectFormat()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateSeeAlsoTag("MyClass");

        // Assert
        Assert.Contains("/// <seealso cref=\"MyClass\"/>", result);
    }

    [Fact]
    public void GenerateTypeParam_GeneratesCorrectFormat()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateTypeParam("T", "The type parameter");

        // Assert
        Assert.Contains("/// <typeparam name=\"T\">The type parameter</typeparam>", result);
    }

    [Fact]
    public void GenerateInheritDoc_GeneratesCorrectFormat()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateInheritDoc();

        // Assert
        Assert.Contains("/// <inheritdoc/>", result);
    }

    [Fact]
    public void GenerateGeneratedClassDocumentation_IncludesWarning()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateGeneratedClassDocumentation(
            "MyClass",
            "Test purpose");

        // Assert
        Assert.Contains("Generated MyClass class", result);
        Assert.Contains("automatically generated", result);
        Assert.Contains("Test purpose", result);
        Assert.Contains("Do not modify", result);
    }

    [Fact]
    public void GenerateGeneratedMethodDocumentation_GeneratesCorrectFormat()
    {
        // Arrange
        var parameters = new[] { ("param", "Parameter description") };

        // Act
        var result = XmlDocumentationHelper.GenerateGeneratedMethodDocumentation(
            "MyMethod",
            "Method purpose",
            parameters,
            "Return value");

        // Assert
        Assert.Contains("/// Method purpose", result);
        Assert.Contains("/// <param name=\"param\">Parameter description</param>", result);
        Assert.Contains("/// <returns>Return value</returns>", result);
    }

    [Fact]
    public void GenerateAutoGeneratedWarning_GeneratesStandardWarning()
    {
        // Act
        var result = XmlDocumentationHelper.GenerateAutoGeneratedWarning();

        // Assert
        Assert.Contains("/// <remarks>", result);
        Assert.Contains("automatically generated", result);
        Assert.Contains("/// Do not modify", result);
        Assert.Contains("/// </remarks>", result);
    }

    [Fact]
    public void WrapText_WithShortText_ReturnsSingleLine()
    {
        // Act
        var result = XmlDocumentationHelper.WrapText("Short text", 80);

        // Assert
        Assert.Single(result);
        Assert.Equal("Short text", result.First());
    }

    [Fact]
    public void WrapText_WithLongText_WrapsCorrectly()
    {
        // Arrange
        var longText = "This is a very long text that should be wrapped into multiple lines when it exceeds the maximum length";

        // Act
        var result = XmlDocumentationHelper.WrapText(longText, 40).ToList();

        // Assert
        Assert.True(result.Count > 1);
        Assert.All(result, line => Assert.True(line.Length <= 40 || !line.Contains(' ')));
    }

    [Fact]
    public void GenerateException_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            XmlDocumentationHelper.GenerateException(null!, "Description"));
    }

    [Fact]
    public void GenerateException_WithNullDescription_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            XmlDocumentationHelper.GenerateException("Exception", null!));
    }

    [Fact]
    public void GenerateExample_WithNullCode_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            XmlDocumentationHelper.GenerateExample("Description", null!));
    }

    [Fact]
    public void GenerateSeeTag_WithNullReference_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            XmlDocumentationHelper.GenerateSeeTag(null!));
    }

    [Fact]
    public void GenerateTypeParam_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            XmlDocumentationHelper.GenerateTypeParam(null!, "Description"));
    }

    [Fact]
    public void WrapText_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = XmlDocumentationHelper.WrapText("", 80);

        // Assert
        Assert.Empty(result);
    }
}
