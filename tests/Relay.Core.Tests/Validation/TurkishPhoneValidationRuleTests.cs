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
    }
}