using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class SedolValidationRuleTests
    {
        [Theory]
        [InlineData("0263494")] // BAE Systems
        [InlineData("B019KW7")] // GlaxoSmithKline
        [InlineData("B082RF1")] // Unilever
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_Sedol(string sedol)
        {
            // Arrange
            var rule = new SedolValidationRule();

            // Act
            var errors = await rule.ValidateAsync(sedol);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("0263495")] // Invalid checksum
        [InlineData("1234567")]
        [InlineData("ABCDEFG")]
        [InlineData("12345")]
        public async Task ValidateAsync_Should_Return_Error_For_Invalid_Sedol(string sedol)
        {
            // Arrange
            var rule = new SedolValidationRule();

            // Act
            var errors = await rule.ValidateAsync(sedol);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid SEDOL number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new SedolValidationRule("Custom SEDOL error");
            var invalidSedol = "1234567";

            // Act
            var errors = await rule.ValidateAsync(invalidSedol);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom SEDOL error", errors.First());
        }
    }
}