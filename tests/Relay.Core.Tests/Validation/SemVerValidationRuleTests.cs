using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class SemVerValidationRuleTests
{
    private readonly SemVerValidationRule _rule = new();

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.1.3")]
    [InlineData("1.0.0-alpha")]
    [InlineData("1.0.0-alpha.1")]
    [InlineData("1.0.0-0.3.7")]
    [InlineData("1.0.0-x.7.z.92")]
    [InlineData("1.0.0+20130313144700")]
    [InlineData("1.0.0-beta+exp.sha.5114f85")]
    [InlineData("1.0.0+21AF26D3----117B344092BD")]
    public async Task ValidateAsync_ValidSemVer_ReturnsEmptyErrors(string version)
    {
        // Act
        var result = await _rule.ValidateAsync(version);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("1.0.0.0")]
    [InlineData("1.0.0-")]
    [InlineData("1.0.0+")]
    [InlineData("1.0.0-+")]
    [InlineData("01.0.0")]
    [InlineData("1.01.0")]
    [InlineData("1.0.01")]
    [InlineData("1.0.0-alpha..1")]
    [InlineData("1.0.0-alpha+")]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidSemVer_ReturnsError(string version)
    {
        // Act
        var result = await _rule.ValidateAsync(version);

        // Assert
        if (string.IsNullOrWhiteSpace(version))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Invalid Semantic Version format.");
        }
    }
}