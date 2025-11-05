using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class TurkishIdValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Valid_Turkish_Id()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "12345678950"; // Example valid TC No

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Invalid_Turkish_Id()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "12345678900"; // Invalid checksum

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Starts_With_Zero()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "02345678901";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Short()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "123456789";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Long()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "123456789012";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Non_Digits()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "1234567890a";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Null()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Empty()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new TurkishIdValidationRule("Custom Turkish ID error");
            var request = "12345678900";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom Turkish ID error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "12345678950";
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
            var rule = new TurkishIdValidationRule();
            var request = "12345678900";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Another_Valid_Turkish_Id()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "10000000146"; // Another valid TC No

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Checksum_Is_Wrong_By_One()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "12345678951"; // Checksum should be 0, not 1

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_All_Digits_Are_Zero_Except_First()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "10000000006"; // Invalid checksum

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.NotEmpty(errors); // This should be invalid
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Has_Leading_Or_Trailing_Whitespace()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = " 12345678950 ";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Unicode_Characters()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "1234567895\u00A0"; // Contains non-breaking space

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_All_Zeros()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "00000000000";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_All_Same_Digit()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "11111111111";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Very_Long_Invalid_Strings()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = new string('1', 1000) + "a"; // Very long string with non-digit

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Empty_String_After_Trim()
        {
            // Arrange
            var rule = new TurkishIdValidationRule();
            var request = "   ";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Default_Error_Message_When_Custom_Is_Null()
        {
            // Arrange
            var rule = new TurkishIdValidationRule(null);
            var request = "12345678900";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Empty_Custom_Error_Message()
        {
            // Arrange
            var rule = new TurkishIdValidationRule("");
            var request = "12345678900";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("", errors.First());
        }
    }
}