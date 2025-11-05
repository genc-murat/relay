using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class CustomValidationRuleTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ValidationFunc_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CustomValidationRule<TestRequest>(null!));
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Errors_From_Custom_Validation_Func()
        {
            // Arrange
            var rule = new CustomValidationRule<TestRequest>((request, ct) =>
            {
                var errors = new List<string>();
                if (string.IsNullOrEmpty(request.Name))
                {
                    errors.Add("Name is required");
                }
                if (request.Age < 18)
                {
                    errors.Add("Must be at least 18 years old");
                }
                return new ValueTask<IEnumerable<string>>(errors);
            });

            var request = new TestRequest { Name = "", Age = 16 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Equal(2, errors.Count());
            Assert.Contains("Name is required", errors);
            Assert.Contains("Must be at least 18 years old", errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Custom_Validation_Passes()
        {
            // Arrange
            var rule = new CustomValidationRule<TestRequest>((request, ct) =>
            {
                var errors = new List<string>();
                if (string.IsNullOrEmpty(request.Name))
                {
                    errors.Add("Name is required");
                }
                return new ValueTask<IEnumerable<string>>(errors);
            });

            var request = new TestRequest { Name = "Valid Name", Age = 25 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_CancellationToken()
        {
            // Arrange
            var rule = new CustomValidationRule<TestRequest>((request, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            });

            var request = new TestRequest { Name = "Valid" };
            var cts = new CancellationTokenSource();

            // Act
            var errors = await rule.ValidateAsync(request, cts.Token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Respect_CancellationToken()
        {
            // Arrange
            var rule = new CustomValidationRule<TestRequest>((request, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(100); // Simulate work
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            });

            var request = new TestRequest { Name = "Valid" };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Async_Custom_Validation()
        {
            // Arrange
            var rule = new CustomValidationRule<TestRequest>(async (request, ct) =>
            {
                await Task.Delay(10, ct); // Simulate async work
                var errors = new List<string>();
                if (request.Name.Length < 3)
                {
                    errors.Add("Name must be at least 3 characters");
                }
                return errors;
            });

            var request = new TestRequest { Name = "AB" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Name must be at least 3 characters", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Exception_In_Custom_Validation_Func()
        {
            // Arrange
            var rule = new CustomValidationRule<TestRequest>((request, ct) =>
            {
                throw new InvalidOperationException("Custom validation failed");
            });

            var request = new TestRequest { Name = "Valid" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await rule.ValidateAsync(request));
            Assert.Contains("Custom validation failed", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Complex_Custom_Logic()
        {
            // Arrange
            var rule = new CustomValidationRule<TestRequest>((request, ct) =>
            {
                var errors = new List<string>();
                if (request.Name.Contains("invalid"))
                {
                    errors.Add("Name contains invalid word");
                }
                if (request.Age < 0 || request.Age > 150)
                {
                    errors.Add("Age must be between 0 and 150");
                }
                if (request.Tags != null && request.Tags.Any(tag => string.IsNullOrEmpty(tag)))
                {
                    errors.Add("Tags cannot contain empty strings");
                }
                return new ValueTask<IEnumerable<string>>(errors);
            });

            var request = new TestRequest
            {
                Name = "invalid name",
                Age = 200,
                Tags = new[] { "tag1", "", "tag2" }
            };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Equal(3, errors.Count());
            Assert.Contains("Name contains invalid word", errors);
            Assert.Contains("Age must be between 0 and 150", errors);
            Assert.Contains("Tags cannot contain empty strings", errors);
        }

        private class TestRequest
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public IEnumerable<string>? Tags { get; set; }
        }
    }
}