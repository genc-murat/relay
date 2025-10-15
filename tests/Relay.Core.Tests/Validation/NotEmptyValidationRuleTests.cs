using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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