using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation
{
    public class DateTimeValidationRuleTests
    {
        [Theory]
        [InlineData("2023-10-27T10:00:00")]
        [InlineData("2023-10-27 10:00:00")]
        [InlineData("10/27/2023 10:00:00")]
        [InlineData("27/10/2023 10:00:00")]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_DateTime_Strings(string validDateTime)
        {
            // Arrange
            var rule = new DateTimeValidationRule();

            // Act
            var errors = await rule.ValidateAsync(validDateTime);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("2023-10-27")]
        [InlineData("10:00:00")]
        [InlineData("not a date time")]
        [InlineData("2023-10-27 25:00:00")]
        public async Task ValidateAsync_Should_Return_Error_For_Invalid_DateTime_Strings(string invalidDateTime)
        {
            // Arrange
            var rule = new DateTimeValidationRule();

            // Act
            var errors = await rule.ValidateAsync(invalidDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid date and time format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Null_Or_Whitespace_String()
        {
            // Arrange
            var rule = new DateTimeValidationRule();

            // Act
            var errorsNull = await rule.ValidateAsync(null);
            var errorsEmpty = await rule.ValidateAsync("");
            var errorsWhitespace = await rule.ValidateAsync("   ");

            // Assert
            Assert.Empty(errorsNull);
            Assert.Empty(errorsEmpty);
            Assert.Empty(errorsWhitespace);
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Formats()
        {
            // Arrange
            var formats = new[] { "yyyy/MM/dd-HH:mm" };
            var rule = new DateTimeValidationRule(formats: formats);
            var validDateTime = "2023/10/27-10:00";
            var invalidDateTime = "2023-10-27 10:00:00";

            // Act
            var errorsValid = await rule.ValidateAsync(validDateTime);
            var errorsInvalid = await rule.ValidateAsync(invalidDateTime);

            // Assert
            Assert.Empty(errorsValid);
            Assert.Single(errorsInvalid);
        }

        [Fact]
        public async Task ValidateAsync_Should_Use_Custom_Error_Message()
        {
            // Arrange
            var rule = new DateTimeValidationRule(errorMessage: "Wrong format!");
            var invalidDateTime = "invalid-date";

            // Act
            var errors = await rule.ValidateAsync(invalidDateTime);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Wrong format!", errors.First());
        }
    }
}