using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class CusipValidationRuleTests
    {
        [Theory]
        [InlineData("037833100")] // Apple Inc.
        [InlineData("931142103")] // Walmart Inc.
        [InlineData("594918104")] // Microsoft Corp.
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_Cusip(string cusip)
        {
            // Arrange
            var rule = new CusipValidationRule();

            // Act
            var errors = await rule.ValidateAsync(cusip);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("037833101")] // Invalid checksum
        [InlineData("123456789")]
        [InlineData("ABCDEFGHI")]
        [InlineData("12345")]
        public async Task ValidateAsync_Should_Return_Error_For_Invalid_Cusip(string cusip)
        {
            // Arrange
            var rule = new CusipValidationRule();

            // Act
            var errors = await rule.ValidateAsync(cusip);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid CUSIP number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new CusipValidationRule("Custom CUSIP error");
            var invalidCusip = "123456789";

            // Act
            var errors = await rule.ValidateAsync(invalidCusip);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom CUSIP error", errors.First());
        }
    }
}