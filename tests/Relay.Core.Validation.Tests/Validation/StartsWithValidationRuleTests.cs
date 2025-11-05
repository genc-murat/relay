using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class StartsWithValidationRuleTests
    {
        [Theory]
        [InlineData("hello", "he")]
        [InlineData("test", "te")]
        [InlineData("world", "wor")]
        [InlineData("a", "a")]
        [InlineData("programming", "pro")]
        [InlineData("test123", "test")]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Starts_With_Prefix(string input, string prefix)
        {
            // Arrange
            var rule = new StartsWithValidationRule(prefix);

            // Act
            var errors = await rule.ValidateAsync(input);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("hello", "lo")]
        [InlineData("test", "st")]
        [InlineData("world", "ld")]
        [InlineData("programming", "ing")]
        [InlineData("test123", "123")]
        public async Task ValidateAsync_Should_Return_Error_When_Value_Does_Not_Start_With_Prefix(string input, string prefix)
        {
            // Arrange
            var rule = new StartsWithValidationRule(prefix);

            // Act
            var errors = await rule.ValidateAsync(input);

            // Assert
            Assert.Single(errors);
            Assert.Equal($"Value must start with '{prefix}'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Value_Is_Null()
        {
            // Arrange
            var rule = new StartsWithValidationRule("test");
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
            var rule = new StartsWithValidationRule("test");
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
            var rule = new StartsWithValidationRule("Test", StringComparison.Ordinal);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("TestHello"));
            var errors = await rule.ValidateAsync("testHello");
            Assert.Single(errors);
            Assert.Equal("Value must start with 'Test'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Case_Insensitive_Comparison()
        {
            // Arrange
            var rule = new StartsWithValidationRule("test", StringComparison.OrdinalIgnoreCase);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("TestHello"));
            Assert.Empty(await rule.ValidateAsync("testHello"));
            Assert.Empty(await rule.ValidateAsync("TESTHELLO"));
            var errors = await rule.ValidateAsync("HelloTest");
            Assert.Single(errors);
            Assert.Equal("Value must start with 'test'.", errors.First());
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
            var rule = new StartsWithValidationRule("test", comparisonType);
            var input = "TestHello";

            // Act
            var errors = await rule.ValidateAsync(input);

            // Assert
            if (comparisonType == StringComparison.OrdinalIgnoreCase ||
                comparisonType == StringComparison.CurrentCultureIgnoreCase ||
                comparisonType == StringComparison.InvariantCultureIgnoreCase)
            {
                Assert.Empty(errors); // Case-insensitive: "TestHello" starts with "test"
            }
            else
            {
                Assert.Single(errors); // Case-sensitive: "TestHello" does NOT start with "test"
                Assert.Equal("Value must start with 'test'.", errors.First());
            }
        }

        [Fact]
        public async Task Constructor_Should_Throw_ArgumentNullException_When_Prefix_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StartsWithValidationRule(null!));
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Empty_Prefix()
        {
            // Arrange
            var rule = new StartsWithValidationRule("");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("hello"));
            Assert.Empty(await rule.ValidateAsync(""));
            Assert.Empty(await rule.ValidateAsync(null));
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Prefix_Longer_Than_Input()
        {
            // Arrange
            var rule = new StartsWithValidationRule("verylongprefix");

            // Act
            var errors = await rule.ValidateAsync("short");

            // Assert
            Assert.Single(errors);
            Assert.Equal("Value must start with 'verylongprefix'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Prefix_Equal_To_Input()
        {
            // Arrange
            var rule = new StartsWithValidationRule("exact");

            // Act
            var errors = await rule.ValidateAsync("exact");

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Unicode_Characters()
        {
            // Arrange
            var rule = new StartsWithValidationRule("café", StringComparison.Ordinal);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("café Bonjour"));
            var errors = await rule.ValidateAsync("cafe Bonjour");
            Assert.Single(errors);
            Assert.Equal("Value must start with 'café'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Unicode_Characters_Case_Insensitive()
        {
            // Arrange
            var rule = new StartsWithValidationRule("CAFÉ", StringComparison.OrdinalIgnoreCase);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("café Bonjour"));
            Assert.Empty(await rule.ValidateAsync("CAFÉ Bonjour"));
            Assert.Empty(await rule.ValidateAsync("Café Bonjour"));
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new StartsWithValidationRule("test");
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync("testHello", cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Cancelled_Token()
        {
            // Arrange
            var rule = new StartsWithValidationRule("test");
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync("testHello", cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Long_Strings()
        {
            // Arrange
            var longString = "test" + new string('a', 10000);
            var rule = new StartsWithValidationRule("test");

            // Act
            var errors = await rule.ValidateAsync(longString);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Special_Characters()
        {
            // Arrange
            var rule = new StartsWithValidationRule("user@");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("user@example.com"));
            var errors = await rule.ValidateAsync("admin@example.com");
            Assert.Single(errors);
            Assert.Equal("Value must start with 'user@'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Whitespace_In_Prefix()
        {
            // Arrange
            var rule = new StartsWithValidationRule(" test ");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(" test hello"));
            var errors = await rule.ValidateAsync("hello test");
            Assert.Single(errors);
            Assert.Equal("Value must start with ' test '.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Newlines_In_Prefix()
        {
            // Arrange
            var rule = new StartsWithValidationRule("test\n");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("test\nhello"));
            var errors = await rule.ValidateAsync("hello\ntest");
            Assert.Single(errors);
            Assert.Equal("Value must start with 'test\n'.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Default_To_Ordinal_Comparison()
        {
            // Arrange
            var rule = new StartsWithValidationRule("Test");

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync("TestHello"));
            var errors = await rule.ValidateAsync("testHello");
            Assert.Single(errors);
            Assert.Equal("Value must start with 'Test'.", errors.First());
        }
    }
}