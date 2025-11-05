using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class ExactLengthValidationRuleTests
    {
        [Theory]
        [InlineData("a", 1)]
        [InlineData("ab", 2)]
        [InlineData("hello", 5)]
        [InlineData("test123", 7)]
        [InlineData("", 0)]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Length_Matches_Exactly(string input, int exactLength)
        {
            // Arrange
            var rule = new ExactLengthValidationRule(exactLength);

            // Act
            var errors = await rule.ValidateAsync(input);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("a", 2)]
        [InlineData("ab", 1)]
        [InlineData("hello", 3)]
        [InlineData("test123", 5)]
        [InlineData("", 1)]
        [InlineData("short", 10)]
        public async Task ValidateAsync_Should_Return_Error_When_Length_Does_Not_Match(string input, int exactLength)
        {
            // Arrange
            var rule = new ExactLengthValidationRule(exactLength);

            // Act
            var errors = await rule.ValidateAsync(input);

            // Assert
            Assert.Single(errors);
            Assert.Equal($"Value must be exactly {exactLength} characters long.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Null()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(5);
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task Constructor_Should_Throw_ArgumentOutOfRangeException_When_ExactLength_Is_Negative()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExactLengthValidationRule(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExactLengthValidationRule(-5));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public async Task Constructor_Should_Accept_Non_Negative_ExactLength(int exactLength)
        {
            // Act & Assert
            var rule = new ExactLengthValidationRule(exactLength);
            Assert.NotNull(rule);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Zero_Length()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(0);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(""));
            var errors = await rule.ValidateAsync("a");
            Assert.Single(errors);
            Assert.Equal("Value must be exactly 0 characters long.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Large_Length()
        {
            // Arrange
            var longString = new string('a', 1000);
            var rule = new ExactLengthValidationRule(1000);

            // Act
            var errors = await rule.ValidateAsync(longString);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Length_Longer_Than_Input()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(10);

            // Act
            var errors = await rule.ValidateAsync("short");

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be exactly 10 characters long.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Length_Shorter_Than_Input()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(3);

            // Act
            var errors = await rule.ValidateAsync("verylongstring");

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must be exactly 3 characters long.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Unicode_Characters()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(4);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("café")); // café is 4 characters
            var errors = await rule.ValidateAsync("abc");
            Assert.Single(errors);
            Assert.Equal("Value must be exactly 4 characters long.", errors.First());

            Assert.Empty(await rule.ValidateAsync("test"));
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(5);
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync("hello", cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(5);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync("hello", cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Special_Characters()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(5);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("a@b#c"));
            var errors = await rule.ValidateAsync("a@b");
            Assert.Single(errors);
            Assert.Equal("Value must be exactly 5 characters long.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Whitespace()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(5);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("     "));
            var errors = await rule.ValidateAsync("  ");
            Assert.Single(errors);
            Assert.Equal("Value must be exactly 5 characters long.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Newlines()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(3);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("a\nb"));
            var errors = await rule.ValidateAsync("a\n");
            Assert.Single(errors);
            Assert.Equal("Value must be exactly 3 characters long.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Empty_String_With_Zero_Length()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(0);

            // Act
            var errors = await rule.ValidateAsync("");

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Single_Character()
        {
            // Arrange
            var rule = new ExactLengthValidationRule(1);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("a"));
            var errors = await rule.ValidateAsync("ab");
            Assert.Single(errors);
            Assert.Equal("Value must be exactly 1 characters long.", errors.First());
        }
    }
}