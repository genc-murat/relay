using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class RegexValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Matches_Pattern()
        {
            // Arrange
            var rule = new RegexValidationRule(@"^\d{3}-\d{2}-\d{4}$");
            var request = "123-45-6789";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Null()
        {
            // Arrange
            var rule = new RegexValidationRule(@"^\d+$");
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Empty()
        {
            // Arrange
            var rule = new RegexValidationRule(@"^\d+$");
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Does_Not_Match_Pattern()
        {
            // Arrange
            var rule = new RegexValidationRule(@"^\d{3}-\d{2}-\d{4}$");
            var request = "invalid-ssn";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value does not match the required format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new RegexValidationRule(@"^\d+$", "Must be numeric only");
            var request = "abc123";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Must be numeric only", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Regex_Options()
        {
            // Arrange
            var rule = new RegexValidationRule(@"^hello$", null, RegexOptions.IgnoreCase);
            var request = "HELLO";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new RegexValidationRule(@"^\d+$");
            var request = "123";
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
            var rule = new RegexValidationRule(@"^\d+$");
            var request = "abc";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }
    }
}