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
    }
}