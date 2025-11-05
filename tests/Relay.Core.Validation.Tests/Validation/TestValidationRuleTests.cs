using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class TestValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Request_Is_Valid()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "Valid Request";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Request_Is_Null()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Request cannot be null or empty", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Request_Is_Empty()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Request cannot be null or empty", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Request_Is_Whitespace()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "   ";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Request cannot be null or empty", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Request_Is_Newlines_Only()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "\n\t\r";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Request cannot be null or empty", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "Valid";
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
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Long_Strings()
        {
            // Arrange
            var longString = new string('a', 10000);
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();

            // Act
            var errors = await rule.ValidateAsync(longString);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Special_Characters()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "特殊字符";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Unicode_Whitespace()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "\u2000\u2001\u2002"; // Unicode spaces

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Request cannot be null or empty", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Numbers_As_Strings()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "12345";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_JSON_Strings()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "{\"key\": \"value\"}";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_XML_Strings()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "<root><element>value</element></root>";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Email_Strings()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "test@example.com";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_URL_Strings()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "https://example.com/path?query=value";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Minimal_Valid_String()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "a";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Maximum_Reasonable_String()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = new string('a', 1000000); // 1MB string

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Mixed_Content()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "Hello 世界 123 !@#$%^&*()";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Emoji()
        {
            // Arrange
            var rule = new Relay.Core.Validation.Rules.TestValidationRule();
            var request = "Hello 😀 World 🌍";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }
    }
}