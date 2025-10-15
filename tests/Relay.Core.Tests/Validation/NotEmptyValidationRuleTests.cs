using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class NotEmptyValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Property_Is_Not_Empty()
        {
            // Arrange
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = "Valid Name" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Property_Is_Null()
        {
            // Arrange
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = null };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name cannot be null or empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Property_Is_Empty()
        {
            // Arrange
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = "" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name cannot be null or empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Property_Is_Whitespace()
        {
            // Arrange
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = "   " };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name cannot be null or empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Property_Is_Newlines_Only()
        {
            // Arrange
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = "\n\t\r" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name cannot be null or empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = "Valid" };
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
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = null };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Different_Property_Names()
        {
            // Arrange
            var rule = new TestEmailNotEmptyValidationRule();
            var request = new TestRequest { Email = "" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Email cannot be null or empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Complex_Property_Access()
        {
            // Arrange
            var rule = new TestNestedPropertyValidationRule();
            var request = new TestRequest
            {
                Address = new Address { Street = "" }
            };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Street cannot be null or empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Property_Access_Exceptions()
        {
            // Arrange
            var rule = new TestThrowingPropertyValidationRule();
            var request = new TestRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await rule.ValidateAsync(request));
            Assert.Contains("Property access failed", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Long_Strings()
        {
            // Arrange
            var longString = new string('a', 10000);
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = longString };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Special_Characters()
        {
            // Arrange
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = "特殊字符" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Unicode_Whitespace()
        {
            // Arrange
            var rule = new TestNotEmptyValidationRule();
            var request = new TestRequest { Name = "\u2000\u2001\u2002" }; // Unicode spaces

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name cannot be null or empty.", errors.First());
        }

        // Tests for the string version of NotEmptyValidationRule
        [Theory]
        [InlineData("hello")]
        [InlineData("a")]
        [InlineData("hello world")]
        [InlineData("123")]
        [InlineData("!@#$")]
        [InlineData("café")] // Unicode
        [InlineData("北京")] // Chinese
        public async Task ValidateAsync_StringVersion_ValidInputs_ReturnEmptyErrors(string input)
        {
            // Arrange
            var rule = new NotEmptyValidationRule();

            // Act
            var result = await rule.ValidateAsync(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r")]
        [InlineData("   ")]
        [InlineData("\t\n\r")]
        [InlineData("\u2000")] // Unicode space
        [InlineData("\u2001\u2002")] // Multiple Unicode spaces
        public async Task ValidateAsync_StringVersion_InvalidInputs_ReturnError(string input)
        {
            // Arrange
            var rule = new NotEmptyValidationRule();

            // Act
            var result = await rule.ValidateAsync(input);

            // Assert
            result.Should().ContainSingle("Value cannot be null or empty.");
        }

        [Fact]
        public async Task ValidateAsync_StringVersion_CustomErrorMessage_ReturnsCustomError()
        {
            // Arrange
            var customMessage = "Custom not empty error";
            var rule = new NotEmptyValidationRule(customMessage);
            var input = "";

            // Act
            var result = await rule.ValidateAsync(input);

            // Assert
            result.Should().ContainSingle(customMessage);
        }

        [Fact]
        public async Task ValidateAsync_StringVersion_CancellationToken_ThrowsWhenCancelled()
        {
            // Arrange
            var rule = new NotEmptyValidationRule();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync("hello", cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_StringVersion_LongValidString_ReturnsEmptyErrors()
        {
            // Arrange
            var input = new string('a', 10000);
            var rule = new NotEmptyValidationRule();

            // Act
            var result = await rule.ValidateAsync(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_StringVersion_UnicodeCharacters_Valid()
        {
            // Arrange
            var input = "café"; // Contains accented character
            var rule = new NotEmptyValidationRule();

            // Act
            var result = await rule.ValidateAsync(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_StringVersion_OnlyUnicodeWhitespace_Invalid()
        {
            // Arrange
            var input = "\u2000\u2001"; // Only Unicode whitespace
            var rule = new NotEmptyValidationRule();

            // Act
            var result = await rule.ValidateAsync(input);

            // Assert
            result.Should().ContainSingle("Value cannot be null or empty.");
        }

        // Test implementations
        private class TestNotEmptyValidationRule : NotEmptyValidationRule<TestRequest>
        {
            protected override string GetPropertyValue(TestRequest request)
            {
                return request.Name;
            }

            protected override string PropertyName => "Name";
        }

        private class TestEmailNotEmptyValidationRule : NotEmptyValidationRule<TestRequest>
        {
            protected override string GetPropertyValue(TestRequest request)
            {
                return request.Email;
            }

            protected override string PropertyName => "Email";
        }

        private class TestNestedPropertyValidationRule : NotEmptyValidationRule<TestRequest>
        {
            protected override string GetPropertyValue(TestRequest request)
            {
                return request.Address?.Street ?? "";
            }

            protected override string PropertyName => "Street";
        }

        private class TestThrowingPropertyValidationRule : NotEmptyValidationRule<TestRequest>
        {
            protected override string GetPropertyValue(TestRequest request)
            {
                throw new InvalidOperationException("Property access failed");
            }

            protected override string PropertyName => "Name";
        }

        private class TestRequest
        {
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public Address? Address { get; set; }
        }

        private class Address
        {
            public string Street { get; set; } = "";
        }
    }
}