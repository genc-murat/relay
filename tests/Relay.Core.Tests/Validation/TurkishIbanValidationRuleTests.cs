using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class TurkishIbanValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Valid_Turkish_Iban()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "TR330006100519786457841326";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Has_Spaces()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "TR33 0006 1005 1978 6457 8413 26";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Lowercase()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "tr330006100519786457841326";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Does_Not_Start_With_TR()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "US330006100519786457841326";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish IBAN.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Short()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "TR33000610051978645784132";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish IBAN.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Too_Long()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "TR3300061005197864578413261";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish IBAN.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Contains_Non_Alphanumeric()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "TR33@006100519786457841326";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish IBAN.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Null()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish IBAN.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Is_Empty()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid Turkish IBAN.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule("Custom IBAN error");
            var request = "US330006100519786457841326";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom IBAN error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new TurkishIbanValidationRule();
            var request = "TR330006100519786457841326";
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
            var rule = new TurkishIbanValidationRule();
            var request = "US330006100519786457841326";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}