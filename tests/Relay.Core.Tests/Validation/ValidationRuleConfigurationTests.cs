using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class ValidationRuleConfigurationTests
    {
        [Fact]
        public async Task IValidationRuleConfiguration_Should_Be_Implemented_By_PropertyValidationRule()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            // Act
            var errors = await rule.ValidateAsync(new TestRequest { Name = "" });

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name is required", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Support_Different_Request_Types()
        {
            // Arrange - String request type
            var stringRule = new PropertyValidationRule<string, int>(
                "Length",
                s => s.Length,
                length => length > 0,
                "String must not be empty");

            // Arrange - Int request type
            var intRule = new PropertyValidationRule<int, bool>(
                "IsPositive",
                i => i > 0,
                isPositive => isPositive,
                "Number must be positive");

            // Act
            var stringErrors = await stringRule.ValidateAsync("");
            var intErrors = await intRule.ValidateAsync(-5);

            // Assert
            Assert.Single(stringErrors);
            Assert.Equal("String must not be empty", stringErrors.First());

            Assert.Single(intErrors);
            Assert.Equal("Number must be positive", intErrors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Handle_Complex_Property_Types()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, DateTime>(
                "CreatedDate",
                r => r.CreatedDate,
                date => date > DateTime.MinValue,
                "Created date must be set");

            var request = new TestRequest { CreatedDate = DateTime.MinValue };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Created date must be set", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Collection_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, IEnumerable<string>>(
                "Tags",
                r => r.Tags,
                tags => tags != null && tags.Any(),
                "At least one tag is required");

            var request = new TestRequest { Tags = new List<string>() };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("At least one tag is required", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Custom_Object_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, Address>(
                "Address",
                r => r.Address,
                address => address != null && !string.IsNullOrEmpty(address.City),
                "Valid address is required");

            var request = new TestRequest { Address = new Address { City = "" } };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Valid address is required", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Support_Async_Property_Access()
        {
            // Arrange - Mock async property access
            var rule = new PropertyValidationRule<TestRequest, string>(
                "AsyncProperty",
                r => r.GetAsyncProperty(),
                value => value == "valid",
                "Async property must be valid");

            var request = new TestRequest();

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Async property must be valid", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Handle_CancellationToken_In_Property_Access()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            var request = new TestRequest { Name = "Valid" };

            // Act
            var errors = await rule.ValidateAsync(request, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Enum_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, Status>(
                "Status",
                r => r.Status,
                status => status != Status.Invalid,
                "Status must be valid");

            var request = new TestRequest { Status = Status.Invalid };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Status must be valid", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Nullable_Enum_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, Status?>(
                "NullableStatus",
                r => r.NullableStatus,
                status => status.HasValue && status.Value != Status.Invalid,
                "Nullable status must be valid if provided");

            var request = new TestRequest { NullableStatus = Status.Invalid };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Nullable status must be valid if provided", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Guid_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, Guid>(
                "Id",
                r => r.Id,
                id => id != Guid.Empty,
                "Id must not be empty");

            var request = new TestRequest { Id = Guid.Empty };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Id must not be empty", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Nullable_Guid_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, Guid?>(
                "NullableId",
                r => r.NullableId,
                id => !id.HasValue || id.Value != Guid.Empty,
                "Nullable id must not be empty if provided");

            var request = new TestRequest { NullableId = Guid.Empty };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Nullable id must not be empty if provided", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Decimal_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, decimal>(
                "Price",
                r => r.Price,
                price => price >= 0,
                "Price must be non-negative");

            var request = new TestRequest { Price = -10.5m };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Price must be non-negative", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_DateTimeOffset_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, DateTimeOffset>(
                "Timestamp",
                r => r.Timestamp,
                timestamp => timestamp > DateTimeOffset.MinValue,
                "Timestamp must be set");

            var request = new TestRequest { Timestamp = DateTimeOffset.MinValue };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Timestamp must be set", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_TimeSpan_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, TimeSpan>(
                "Duration",
                r => r.Duration,
                duration => duration > TimeSpan.Zero,
                "Duration must be positive");

            var request = new TestRequest { Duration = TimeSpan.FromHours(-1) };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Duration must be positive", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Array_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string[]>(
                "Items",
                r => r.Items,
                items => items != null && items.Length > 0,
                "At least one item is required");

            var request = new TestRequest { Items = new string[0] };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("At least one item is required", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Work_With_Dictionary_Properties()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, Dictionary<string, string>>(
                "Metadata",
                r => r.Metadata,
                metadata => metadata != null && metadata.ContainsKey("required"),
                "Metadata must contain required key");

            var request = new TestRequest { Metadata = new Dictionary<string, string>() };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Metadata must contain required key", errors.First());
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Handle_Property_Access_Exceptions()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "FaultyProperty",
                r => throw new InvalidOperationException("Property access failed"),
                s => true,
                "Should not reach here");

            var request = new TestRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await rule.ValidateAsync(request));
            Assert.Contains("Property access failed", exception.Message);
        }

        [Fact]
        public async Task IValidationRuleConfiguration_Should_Handle_Predicate_Exceptions()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => throw new InvalidOperationException("Predicate failed"),
                "Should not reach here");

            var request = new TestRequest { Name = "Valid" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await rule.ValidateAsync(request));
            Assert.Contains("Predicate failed", exception.Message);
        }

        // Test classes and enums
        private class TestRequest
        {
            public string Name { get; set; } = "";
            public DateTime CreatedDate { get; set; }
            public IEnumerable<string> Tags { get; set; } = new List<string>();
            public Address Address { get; set; } = new Address();
            public Status Status { get; set; }
            public Status? NullableStatus { get; set; }
            public Guid Id { get; set; }
            public Guid? NullableId { get; set; }
            public decimal Price { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public TimeSpan Duration { get; set; }
            public string[] Items { get; set; } = new string[0];
            public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

            public string GetAsyncProperty() => "invalid";
        }

        private class Address
        {
            public string City { get; set; } = "";
        }

        private enum Status
        {
            Invalid,
            Active,
            Inactive
        }
    }
}