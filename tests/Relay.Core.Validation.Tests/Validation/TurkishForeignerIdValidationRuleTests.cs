using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class TurkishForeignerIdValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Valid_Turkish_Foreigner_Id()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            var request = "99123456740"; // Example valid foreigner TC No

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Does_Not_Start_With_99()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            var request = "89123456789";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish foreigner ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Invalid_Foreigner_Id_Checksum()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            var request = "99123456780"; // Invalid checksum

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish foreigner ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Short()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            var request = "9912345678";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish foreigner ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Long()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            var request = "991234567890";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish foreigner ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Non_Digits()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            var request = "9912345678a";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish foreigner ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Null()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish foreigner ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Empty()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish foreigner ID number.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule("Custom foreigner ID error");
            var request = "99123456780";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom foreigner ID error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new TurkishForeignerIdValidationRule();
            var request = "99123456740";
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
            var rule = new TurkishForeignerIdValidationRule();
            var request = "99123456780";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}