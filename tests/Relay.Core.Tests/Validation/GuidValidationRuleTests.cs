using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class GuidValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_GUID_Is_Valid()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = Guid.NewGuid().ToString();

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_GUID_Is_Null()
        {
            // Arrange
            var rule = new GuidValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_GUID_Is_Invalid()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = "not-a-guid";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid GUID format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new GuidValidationRule("Custom GUID error");
            var request = "invalid-guid";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom GUID error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Accept_GUID_Without_Hyphens()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var guid = Guid.NewGuid();
            var request = guid.ToString("N"); // No hyphens

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Accept_GUID_With_Hyphens()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var guid = Guid.NewGuid();
            var request = guid.ToString("D"); // With hyphens

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = Guid.NewGuid().ToString();
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
            var rule = new GuidValidationRule();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Theory]
        [InlineData("{12345678-1234-1234-1234-123456789012}")] // Braces
        [InlineData("(12345678-1234-1234-1234-123456789012)")] // Parentheses
        [InlineData("12345678123412341234123456789012")] // No hyphens
        [InlineData("12345678-1234-1234-1234-123456789012")] // Standard
        [InlineData("12345678-1234-1234-1234-123456789012")] // Lowercase
        [InlineData("12345678-1234-1234-1234-123456789012")] // Uppercase
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_GUID_Formats(string guidString)
        {
            // Arrange
            var rule = new GuidValidationRule();

            // Act
            var errors = await rule.ValidateAsync(guidString);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("")] // Empty
        [InlineData("   ")] // Whitespace
        [InlineData("12345678-1234-1234-1234-12345678901")] // Too short
        [InlineData("12345678-1234-1234-1234-1234567890123")] // Too long
        [InlineData("gggggggg-gggg-gggg-gggg-gggggggggggg")] // Invalid chars
        [InlineData("12345678-1234-1234-1234-12345678901z")] // Invalid char at end
        [InlineData("{12345678-1234-1234-1234-123456789012")] // Missing closing brace
        [InlineData("12345678-1234-1234-1234-123456789012}")] // Missing opening brace
        [InlineData("12345678-1234-1234-1234")] // Incomplete
        public async Task ValidateAsync_Should_Return_Error_For_Invalid_GUID_Formats(string guidString)
        {
            // Arrange
            var rule = new GuidValidationRule();

            // Act
            var errors = await rule.ValidateAsync(guidString);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid GUID format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Accept_All_Zeros_GUID()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = "00000000-0000-0000-0000-000000000000";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Accept_GUID_In_Uppercase()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var guid = Guid.NewGuid();
            var request = guid.ToString().ToUpper();

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_GUID_With_Extra_Hyphens()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = "12345678-1234-1234-1234-123456789-012";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid GUID format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_GUID_With_Invalid_Group_Lengths()
        {
            // Arrange
            var rule = new GuidValidationRule();
            var request = "1234567-1234-1234-1234-123456789012"; // First group too short

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid GUID format.", errors.First());
        }
    }
}