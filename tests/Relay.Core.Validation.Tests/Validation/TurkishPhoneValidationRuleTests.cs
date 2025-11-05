using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class TurkishPhoneValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Valid_Turkish_Mobile()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "5551234567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Valid_With_Country_Code()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "+905551234567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Valid_With_90_Prefix()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "905551234567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Does_Not_Start_With_5()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "4551234567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Short()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "555123456";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Long()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "55512345678";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Null()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Empty()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule("Custom Turkish phone error");
            var request = "4551234567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom Turkish phone error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "5551234567";
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(request, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "4551234567";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Short_After_Country_Code_Removal()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "+9055512345"; // 9 digits after +90

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Long_After_Country_Code_Removal()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "+9055512345678"; // 11 digits after +90

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Has_Invalid_Country_Code()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "+915551234567"; // Wrong country code

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Starts_With_90_But_Wrong_Length()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "90555123456"; // 11 digits total, should be 12

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Letters()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "555abc4567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Special_Characters()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "555-123-4567";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Starts_With_Wrong_Digit()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = "4551234567"; // Starts with 4, not 5

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Different_Valid_Mobile_Numbers()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var validNumbers = new[] { "5011234567", "5051234567", "5061234567", "5071234567", "5511234567" };

            // Act & Assert
            foreach (var number in validNumbers)
            {
                var errors = await rule.ValidateAsync(number);
                Assert.Empty(errors);
            }
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Very_Long_Invalid_Phone_String()
        {
            // Arrange
            var rule = new TurkishPhoneValidationRule();
            var request = new string('5', 100) + "a";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish phone number.", errors.First());
        }
    }
}