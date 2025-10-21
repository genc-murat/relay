using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class EndsWithValidationRuleTests
    {
        [Theory]
        [InlineData("hello", "lo")]
        [InlineData("test", "st")]
        [InlineData("world", "world")]
        [InlineData("a", "a")]
        [InlineData("programming", "ing")]
        [InlineData("test123", "123")]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Ends_With_Suffix(string input, string suffix)
        {
            // Arrange
            var rule = new EndsWithValidationRule(suffix);

            // Act
            var errors = await rule.ValidateAsync(input);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("hello", "he")]
        [InlineData("test", "te")]
        [InlineData("world", "worl")]
        [InlineData("programming", "program")]
        [InlineData("test123", "test")]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Does_Not_End_With_Suffix(string input, string suffix)
        {
            // Arrange
            var rule = new EndsWithValidationRule(suffix);

            // Act
            var errors = await rule.ValidateAsync(input);

            // Assert
            Assert.Single(errors);
            Assert.Equal($"Value must end with '{suffix}'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Null()
        {
            // Arrange
            var rule = new EndsWithValidationRule("test");
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
            var rule = new EndsWithValidationRule("test");
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Case_Sensitive_Comparison()
        {
            // Arrange
            var rule = new EndsWithValidationRule("Test", StringComparison.Ordinal);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("MyTest"));
            var errors = await rule.ValidateAsync("Mytest");
            Assert.Single(errors);
            Assert.Equal("Value must end with 'Test'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Case_Insensitive_Comparison()
        {
            // Arrange
            var rule = new EndsWithValidationRule("test", StringComparison.OrdinalIgnoreCase);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("MyTest"));
            Assert.Empty(await rule.ValidateAsync("Mytest"));
            Assert.Empty(await rule.ValidateAsync("MYTEST"));
            var errors = await rule.ValidateAsync("MyTes");
            Assert.Single(errors);
            Assert.Equal("Value must end with 'test'.", errors.First());
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public async Task ValidateAsync_Should_Work_With_Different_StringComparison_Types(StringComparison comparisonType)
        {
            // Arrange
            var rule = new EndsWithValidationRule("test", comparisonType);
            var input = "myTest";

            // Act
            var errors = await rule.ValidateAsync(input);

            // Assert
            if (comparisonType == StringComparison.OrdinalIgnoreCase ||
                comparisonType == StringComparison.CurrentCultureIgnoreCase ||
                comparisonType == StringComparison.InvariantCultureIgnoreCase)
            {
                Assert.Empty(errors); // Case-insensitive: "myTest" ends with "test"
            }
            else
            {
                Assert.Single(errors); // Case-sensitive: "myTest" does NOT end with "test"
                Assert.Equal("Value must end with 'test'.", errors.First());
            }
        }

        [Fact]
        public async Task Constructor_Should_Throw_ArgumentNullException_When_Suffix_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EndsWithValidationRule(null!));
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Empty_Suffix()
        {
            // Arrange
            var rule = new EndsWithValidationRule("");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("hello"));
            Assert.Empty(await rule.ValidateAsync(""));
            Assert.Empty(await rule.ValidateAsync(null));
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Suffix_Longer_Than_Input()
        {
            // Arrange
            var rule = new EndsWithValidationRule("verylongsuffix");

            // Act
            var errors = await rule.ValidateAsync("short");

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must end with 'verylongsuffix'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Suffix_Equal_To_Input()
        {
            // Arrange
            var rule = new EndsWithValidationRule("exact");

            // Act
            var errors = await rule.ValidateAsync("exact");

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Unicode_Characters()
        {
            // Arrange
            var rule = new EndsWithValidationRule("café", StringComparison.Ordinal);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("Bonjour café"));
            var errors = await rule.ValidateAsync("Bonjour cafe");
            Assert.Single(errors);
            Assert.Equal("Value must end with 'café'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Unicode_Characters_Case_Insensitive()
        {
            // Arrange
            var rule = new EndsWithValidationRule("CAFÉ", StringComparison.OrdinalIgnoreCase);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("Bonjour café"));
            Assert.Empty(await rule.ValidateAsync("Bonjour CAFÉ"));
            Assert.Empty(await rule.ValidateAsync("Bonjour Café"));
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new EndsWithValidationRule("test");
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync("mytest", cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new EndsWithValidationRule("test");
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync("mytest", cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Long_Strings()
        {
            // Arrange
            var longString = new string('a', 10000) + "test";
            var rule = new EndsWithValidationRule("test");

            // Act
            var errors = await rule.ValidateAsync(longString);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Special_Characters()
        {
            // Arrange
            var rule = new EndsWithValidationRule("@example.com");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("user@example.com"));
            var errors = await rule.ValidateAsync("user@example.org");
            Assert.Single(errors);
            Assert.Equal("Value must end with '@example.com'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Whitespace_In_Suffix()
        {
            // Arrange
            var rule = new EndsWithValidationRule(" test ");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("hello test "));
            var errors = await rule.ValidateAsync("hello test");
            Assert.Single(errors);
            Assert.Equal("Value must end with ' test '.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Newlines_In_Suffix()
        {
            // Arrange
            var rule = new EndsWithValidationRule("\ntest");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("hello\ntest"));
            var errors = await rule.ValidateAsync("hello\ntes");
            Assert.Single(errors);
            Assert.Equal("Value must end with '\ntest'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Default_To_Ordinal_Comparison()
        {
            // Arrange
            var rule = new EndsWithValidationRule("Test");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("MyTest"));
            var errors = await rule.ValidateAsync("Mytest");
            Assert.Single(errors);
            Assert.Equal("Value must end with 'Test'.", errors.First());
        }
    }
}