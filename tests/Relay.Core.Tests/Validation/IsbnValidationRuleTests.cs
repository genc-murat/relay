using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class IsbnValidationRuleTests
{
    private readonly IsbnValidationRule _rule = new();

    [Theory]
    [InlineData("9783161484100")] // Valid ISBN-13
    [InlineData("978-3-16-148410-0")] // Valid ISBN-13 with hyphens
    [InlineData("0306406152")] // Valid ISBN-10
    [InlineData("0-306-40615-2")] // Valid ISBN-10 with hyphens
    [InlineData("048665088X")] // Valid ISBN-10 with X check digit
    public async Task ValidateAsync_ValidIsbn_ReturnsEmptyErrors(string isbn)
    {
        // Act
        var result = await _rule.ValidateAsync(isbn);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("978-0-123456-78-8")] // Invalid check digit
    [InlineData("012345678")] // Too short
    [InlineData("01234567890")] // Too long
    [InlineData("978012345678")] // Invalid length
    [InlineData("invalid-isbn")] // Non-numeric
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidIsbn_ReturnsError(string isbn)
    {
        // Act
        var result = await _rule.ValidateAsync(isbn);

        // Assert
        if (string.IsNullOrWhiteSpace(isbn))
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Invalid ISBN format.", result.First());
        }
    }
}