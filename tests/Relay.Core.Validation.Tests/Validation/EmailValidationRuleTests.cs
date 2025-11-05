using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class EmailValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Email_Is_Valid()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "test@example.com";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Email_Is_Null()
        {
            // Arrange
            var rule = new EmailValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Email_Is_Empty()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Email_Is_Invalid()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "invalid-email";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid email address format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Email_Has_No_At_Symbol()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "testexample.com";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid email address format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Email_Has_No_Domain()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "test@";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid email address format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "test@example.com";
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
            var rule = new EmailValidationRule();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Theory]
        [InlineData("test@sub.example.com")]
        [InlineData("user+tag@example.com")]
        [InlineData("test_user@example.com")]
        [InlineData("123@example.com")]
        [InlineData("test.email@example.com")]
        [InlineData("test-email@example.com")]
        [InlineData("test@example.co.uk")]
        [InlineData("Test.User@Example.Com")] // Case insensitive
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_Email_Formats(string email)
        {
            // Arrange
            var rule = new EmailValidationRule();

            // Act
            var errors = await rule.ValidateAsync(email);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("test@@example.com")] // Multiple @
        [InlineData("test @example.com")] // Space before @
        [InlineData("test@ example.com")] // Space after @
        [InlineData("test@example.com ")] // Trailing space
        [InlineData(" test@example.com")] // Leading space
        [InlineData("test@.com")] // Dot immediately after @
        [InlineData("test..email@example.com")] // Consecutive dots
        [InlineData("@example.com")] // No local part
        [InlineData("test@")] // No domain
        [InlineData("test@exam ple.com")] // Space in domain
        [InlineData("test@exam.ple.com.")] // Trailing dot
        public async Task ValidateAsync_Should_Return_Error_For_Invalid_Email_Formats(string email)
        {
            // Arrange
            var rule = new EmailValidationRule();

            // Act
            var errors = await rule.ValidateAsync(email);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid email address format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Very_Long_Email()
        {
            // Arrange
            var localPart = new string('a', 64); // Max local part length in some systems
            var domain = new string('b', 63) + ".com"; // Max domain label length
            var email = $"{localPart}@{domain}";
            var rule = new EmailValidationRule();

            // Act
            var errors = await rule.ValidateAsync(email);

            // Assert
            Assert.Empty(errors); // Should pass as long as it matches regex
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Email_With_Only_At_Symbol()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "@";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid email address format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Email_With_Only_Dot()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = ".";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid email address format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Whitespace_Only_String()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "   ";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors); // Null or whitespace returns empty errors
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Newline_In_Email()
        {
            // Arrange
            var rule = new EmailValidationRule();
            var request = "test@\nexample.com";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid email address format.", errors.First());
        }
    }
}