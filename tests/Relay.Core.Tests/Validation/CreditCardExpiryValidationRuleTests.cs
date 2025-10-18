using System;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class CreditCardExpiryValidationRuleTests
    {
        [Theory]
        [InlineData("12/25")]
        [InlineData("01/2026")]
        [InlineData("12/2025")]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_Future_Expiry_Date(string expiryDate)
        {
            // Arrange
            var rule = new CreditCardExpiryValidationRule();

            // Act
            var errors = await rule.ValidateAsync(expiryDate);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("13/25")]
        [InlineData("01/2020")]
        [InlineData("12/20")]
        [InlineData("1/25")]
        [InlineData("12/202")]
        public async Task ValidateAsync_Should_Return_Error_For_Invalid_Or_Past_Expiry_Date(string expiryDate)
        {
            // Arrange
            var rule = new CreditCardExpiryValidationRule();

            // Act
            var errors = await rule.ValidateAsync(expiryDate);

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Expired_Card()
        {
            // Arrange
            var rule = new CreditCardExpiryValidationRule();
            var expiredDate = DateTime.Now.AddMonths(-1).ToString("MM/yy");

            // Act
            var errors = await rule.ValidateAsync(expiredDate);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Credit card has expired.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message_For_Format()
        {
            // Arrange
            var rule = new CreditCardExpiryValidationRule("Wrong format.");
            var invalidFormat = "1-25";

            // Act
            var errors = await rule.ValidateAsync(invalidFormat);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Wrong format.", errors.First());
        }
    }
}