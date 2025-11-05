using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class UrlValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_URL_Is_Valid()
        {
            // Arrange
            var rule = new UrlValidationRule();
            var request = "https://www.example.com";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_URL_Is_HTTP()
        {
            // Arrange
            var rule = new UrlValidationRule();
            var request = "http://example.com/path";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_URL_Is_Null()
        {
            // Arrange
            var rule = new UrlValidationRule();
            string request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_URL_Is_Invalid()
        {
            // Arrange
            var rule = new UrlValidationRule();
            var request = "not-a-url";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid URL format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_URL_Has_Invalid_Scheme()
        {
            // Arrange
            var rule = new UrlValidationRule();
            var request = "ftp://example.com";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid URL format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
        {
            // Arrange
            var rule = new UrlValidationRule("Custom URL error");
            var request = "invalid-url";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Custom URL error", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new UrlValidationRule();
            var request = "https://example.com";
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
            var rule = new UrlValidationRule();
            var request = "invalid";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Theory]
        [InlineData("https://example.com:8080")]
        [InlineData("http://localhost:3000")]
        [InlineData("https://api.example.com/v1/users")]
        [InlineData("https://example.com/path?query=value")]
        [InlineData("https://example.com/path#fragment")]
        [InlineData("https://user:pass@example.com")]
        [InlineData("https://192.168.1.1")]
        [InlineData("http://[::1]")] // IPv6 localhost
        [InlineData("https://example.com/path/to/resource.html")]
        [InlineData("HTTP://EXAMPLE.COM")] // Case insensitive
        public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_URL_Formats(string url)
        {
            // Arrange
            var rule = new UrlValidationRule();

            // Act
            var errors = await rule.ValidateAsync(url);

            // Assert
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("ftp://example.com")]
        [InlineData("file:///path/to/file")]
        [InlineData("mailto:test@example.com")]
        [InlineData("javascript:alert('xss')")]
        [InlineData("data:text/plain;base64,SGVsbG8=")]
        public async Task ValidateAsync_Should_Return_Error_For_Invalid_Schemes(string url)
        {
            // Arrange
            var rule = new UrlValidationRule();

            // Act
            var errors = await rule.ValidateAsync(url);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid URL format.", errors.First());
        }

        [Theory]
        [InlineData("example.com")] // No scheme
        [InlineData("://example.com")] // Empty scheme
        [InlineData("http:///example.com")] // Triple slash
        [InlineData("https://")] // No host
        [InlineData("https:// example.com")] // Space in host
        [InlineData("https://exam ple.com")] // Space in domain
        [InlineData("https://.com")] // Dot at start
        [InlineData("https://example..com")] // Double dot
        public async Task ValidateAsync_Should_Return_Error_For_Malformed_URLs(string url)
        {
            // Arrange
            var rule = new UrlValidationRule();

            // Act
            var errors = await rule.ValidateAsync(url);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid URL format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Very_Long_URL()
        {
            // Arrange
            var longPath = new string('a', 2000);
            var url = $"https://example.com/{longPath}";
            var rule = new UrlValidationRule();

            // Act
            var errors = await rule.ValidateAsync(url);

            // Assert
            Assert.Empty(errors); // Uri.TryCreate should handle it
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_URL_With_Invalid_Characters()
        {
            // Arrange
            var rule = new UrlValidationRule();
            var request = "https://example.com/<script>";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid URL format.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Empty_String()
        {
            // Arrange
            var rule = new UrlValidationRule();
            var request = "";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors); // Null or whitespace
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_For_Relative_URL()
        {
            // Arrange
            var rule = new UrlValidationRule();
            var request = "/path/to/resource";

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid URL format.", errors.First());
        }
    }
}