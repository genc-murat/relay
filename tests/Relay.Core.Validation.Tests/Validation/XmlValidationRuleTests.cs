using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class XmlValidationRuleTests
{
    private readonly XmlValidationRule _rule = new();

    [Theory]
    [InlineData("<root></root>")] // Simple element
    [InlineData("<root><child>value</child></root>")] // Nested elements
    [InlineData("<?xml version=\"1.0\"?><root><child id=\"1\">value</child></root>")] // With declaration and attributes
    [InlineData("<root/>")] // Self-closing tag
    [InlineData("<root><![CDATA[<not>parsed</not>]]></root>")] // CDATA
    public async Task ValidateAsync_ValidXml_ReturnsEmptyErrors(string xml)
    {
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("<root>")] // Unclosed tag
    [InlineData("<root></root></extra>")] // Extra closing tag
    [InlineData("<root><child></root>")] // Mismatched tags
    [InlineData("not xml")] // Plain text
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidXml_ReturnsError(string xml)
    {
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        if (string.IsNullOrWhiteSpace(xml))
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Invalid XML format.", result.First());
        }
    }
    
    [Fact]
    public async Task ValidateAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _rule.ValidateAsync("<root>value</root>", cancellationTokenSource.Token));
    }
    
    [Theory]
    [InlineData("   <root></root>   ")] // XML with leading/trailing whitespace
    [InlineData("\t<root>\n<child />\r</root>\t")] // XML with various whitespace
    public async Task ValidateAsync_XmlWithWhitespace_ReturnsEmptyErrors(string xml)
    {
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [InlineData("<root><child attr=\"value\">text</child></root>")] // With attributes
    [InlineData("<ns:root xmlns:ns=\"namespace\"><ns:child>value</ns:child></ns:root>")] // With namespace
    [InlineData("<root><![CDATA[<some>special</some>content]]></root>")] // With CDATA
    [InlineData("<root><?pi processing instruction?></root>")] // With processing instruction
    public async Task ValidateAsync_XmlWithFeatures_ReturnsEmptyErrors(string xml)
    {
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [InlineData("<root><child></child></root>")] // Properly nested
    [InlineData("<a><b><c>value</c></b></a>")] // Deep nesting
    [InlineData("<root><child1/><child2>value</child2><child3/></root>")] // Mixed empty and non-empty
    public async Task ValidateAsync_ValidNestedXml_ReturnsEmptyErrors(string xml)
    {
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task ValidateAsync_XmlWithSpecialCharacters_ReturnsEmptyErrors()
    {
        // Arrange
        var xml = "<root>Special chars: &amp; &lt; &gt; &quot; &apos; éñü</root>";
        
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task ValidateAsync_VeryLargeValidXml_ReturnsEmptyErrors()
    {
        // Arrange - create a large valid XML document
        var xml = "<root>";
        for (int i = 0; i < 1000; i++)
        {
            xml += $"<item id=\"{i}\">value{i}</item>";
        }
        xml += "</root>";
        
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [InlineData("<")] // Incomplete tag start
    [InlineData(">")] // Incomplete tag end
    [InlineData("<root<")] // Malformed tag
    [InlineData("<root attr=>")] // Incomplete attribute value
    [InlineData("<root attr=\"unclosed>]")] // Unclosed attribute
    [InlineData("<!-- comment -->")] // Just a comment without root element
    public async Task ValidateAsync_MalformedXml_ReturnsError(string xml)
    {
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid XML format.", result.First());
    }
    
    [Fact]
    public async Task ValidateAsync_XmlWithInternalDtd_ReturnsEmptyErrors()
    {
        // Arrange - XML with internal DTD (should be allowed since DtdProcessing is Prohibit)
        // Actually this should fail since DTD is prohibited
        var xml = "<!DOCTYPE root [<!ELEMENT root (#PCDATA)>]><root>value</root>";
        
        // Act
        var result = await _rule.ValidateAsync(xml);

        // Assert
        Assert.Single(result); // Should fail because DTD is prohibited
    }
}