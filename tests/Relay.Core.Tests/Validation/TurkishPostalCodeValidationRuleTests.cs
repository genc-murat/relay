using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class TurkishPostalCodeValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Valid_Postal_Code()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule();
            var request = "34000";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Has_Spaces()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule();
            var request = "34 000";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Short()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule();
            var request = "3400";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish postal code.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Long()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule();
            var request = "340000";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish postal code.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Non_Digits()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule();
            var request = "34a00";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish postal code.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Null()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish postal code.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Empty()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish postal code.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule("Custom postal code error");
            var request = "3400";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom postal code error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new TurkishPostalCodeValidationRule();
            var request = "34000";
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
            var rule = new TurkishPostalCodeValidationRule();
            var request = "3400";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}