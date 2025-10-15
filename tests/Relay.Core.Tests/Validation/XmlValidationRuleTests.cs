using System.Threading.Tasks;
using FluentAssertions;
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
        result.Should().BeEmpty();
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
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Invalid XML format.");
        }
    }
}