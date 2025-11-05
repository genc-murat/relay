using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class DateValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Date_Is_Valid()
        {
            // Arrange
            var rule = new DateValidationRule();
            var request = "2023-10-15";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Date_Is_Null()
        {
            // Arrange
            var rule = new DateValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Date_Is_Invalid()
        {
            // Arrange
            var rule = new DateValidationRule();
            var request = "invalid-date";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid date format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Formats()
        {
            // Arrange
            var rule = new DateValidationRule(new[] { "dd-MM-yyyy" });
            var request = "15-10-2023";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new DateValidationRule(errorMessage: "Custom date error");
            var request = "abc";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom date error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Different_Cultures()
        {
            // Arrange
            var rule = new DateValidationRule(
                new[] { "MM/dd/yyyy" },
                CultureInfo.InvariantCulture);
            var request = "10/15/2023";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new DateValidationRule();
            var request = "2023-10-15";
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
            var rule = new DateValidationRule();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}