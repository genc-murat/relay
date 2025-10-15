using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class PropertyValidationRuleTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_PropertyName_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new PropertyValidationRule<TestRequest, string>(
                    null!,
                    r => r.Name,
                    s => !string.IsNullOrEmpty(s),
                    "Error"));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_PropertyFunc_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new PropertyValidationRule<TestRequest, string>(
                    "Name",
                    null!,
                    s => !string.IsNullOrEmpty(s),
                    "Error"));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new PropertyValidationRule<TestRequest, string>(
                    "Name",
                    r => r.Name,
                    null!,
                    "Error"));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ErrorMessage_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new PropertyValidationRule<TestRequest, string>(
                    "Name",
                    r => r.Name,
                    s => !string.IsNullOrEmpty(s),
                    null!));
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Request_Is_Null()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            // Act
            var errors = await rule.ValidateAsync(null!);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Request cannot be null.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Predicate_Returns_True()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            var request = new TestRequest { Name = "Valid Name" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Predicate_Returns_False()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            var request = new TestRequest { Name = "" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name is required", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Value_Types()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, int>(
                "Age",
                r => r.Age,
                age => age > 0,
                "Age must be positive");

            var request = new TestRequest { Age = -5 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Age must be positive", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Nullable_Value_Types()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, int?>(
                "OptionalAge",
                r => r.OptionalAge,
                age => age.HasValue && age.Value > 0,
                "Optional age must be positive if provided");

            var request = new TestRequest { OptionalAge = -5 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Optional age must be positive if provided", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Complex_Predicates()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Email",
                r => r.Email,
                email => email.Contains("@") && email.Contains("."),
                "Email must be valid");

            var request = new TestRequest { Email = "invalid-email" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Email must be valid", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Null_Property_Values()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "NullableName",
                r => r.NullableName,
                name => name != null && name.Length > 3,
                "Name must be longer than 3 characters");

            var request = new TestRequest { NullableName = null };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name must be longer than 3 characters", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            var request = new TestRequest { Name = "Valid" };
            var cts = new System.Threading.CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(request, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_PropertyFunc_Throwing_Exception()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => { throw new InvalidOperationException("Property access failed"); },
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            var request = new TestRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await rule.ValidateAsync(request));
            Assert.Contains("Property access failed", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Predicate_Throwing_Exception()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => { throw new InvalidOperationException("Predicate failed"); },
                "Name is required");

            var request = new TestRequest { Name = "Valid" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await rule.ValidateAsync(request));
            Assert.Contains("Predicate failed", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Concurrent_Requests()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            var requests = new[]
            {
                new TestRequest { Name = "Valid1" },
                new TestRequest { Name = "" },
                new TestRequest { Name = "Valid2" },
                new TestRequest { Name = null }
            };

            // Act
            var tasks = requests.Select(r => rule.ValidateAsync(r)).ToArray();
            var results = await Task.WhenAll(tasks.Select(t => t.AsTask()));

            // Assert
            Assert.Empty(results[0]); // Valid1
            Assert.Single(results[1]); // Empty string
            Assert.Empty(results[2]); // Valid2
            Assert.Single(results[3]); // Null
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Large_Number_Of_Requests()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            var requests = Enumerable.Range(0, 1000)
                .Select(i => new TestRequest { Name = i % 2 == 0 ? $"Valid{i}" : "" })
                .ToList();

            // Act
            var tasks = requests.Select(r => rule.ValidateAsync(r)).ToArray();
            var results = await Task.WhenAll(tasks.Select(t => t.AsTask()));

            // Assert
            for (int i = 0; i < results.Length; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.Empty(results[i]); // Even indices are valid
                }
                else
                {
                    Assert.Single(results[i]); // Odd indices are invalid
                }
            }
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_CancellationToken_In_Concurrent_Scenario()
        {
            // Arrange
            var rule = new PropertyValidationRule<TestRequest, string>(
                "Name",
                r => r.Name,
                s => !string.IsNullOrEmpty(s),
                "Name is required");

            var cts = new CancellationTokenSource();
            var request = new TestRequest { Name = "Valid" };

            // Act
            var task = rule.ValidateAsync(request, cts.Token);
            cts.Cancel();

            // Assert - Should complete successfully since validation is fast
            var errors = await task;
            Assert.Empty(errors);
        }

        private class TestRequest
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public int? OptionalAge { get; set; }
            public string Email { get; set; } = "";
            public string? NullableName { get; set; }
        }
    }
}