using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Validation
{
    public class EmptyValidationRuleTests
    {
        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Collection_Is_Empty()
        {
            // Arrange
            var rule = new EmptyValidationRule<string>();
            var request = new List<string>();

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Empty_Errors_When_Collection_Is_Null()
        {
            // Arrange
            var rule = new EmptyValidationRule<string>();
            List<string> request = null;

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Collection_Has_Items()
        {
            // Arrange
            var rule = new EmptyValidationRule<string>();
            var request = new List<string> { "item1" };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Collection must be empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Return_Error_When_Collection_Has_Multiple_Items()
        {
            // Arrange
            var rule = new EmptyValidationRule<int>();
            var request = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var errors = await rule.ValidateAsync(request);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Collection must be empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Different_Generic_Types()
        {
            // Test with different types
            var stringRule = new EmptyValidationRule<string>();
            var intRule = new EmptyValidationRule<int>();
            var objectRule = new EmptyValidationRule<object>();

            var emptyStrings = new List<string>();
            var emptyInts = new List<int>();
            var emptyObjects = new List<object>();

            var nonEmptyStrings = new List<string> { "test" };
            var nonEmptyInts = new List<int> { 42 };
            var nonEmptyObjects = new List<object> { new object() };

            // Act & Assert - Empty collections
            Assert.Empty(await stringRule.ValidateAsync(emptyStrings));
            Assert.Empty(await intRule.ValidateAsync(emptyInts));
            Assert.Empty(await objectRule.ValidateAsync(emptyObjects));

            // Act & Assert - Non-empty collections
            Assert.Single(await stringRule.ValidateAsync(nonEmptyStrings));
            Assert.Single(await intRule.ValidateAsync(nonEmptyInts));
            Assert.Single(await objectRule.ValidateAsync(nonEmptyObjects));
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_Array_Inputs()
        {
            // Arrange
            var rule = new EmptyValidationRule<string>();
            var emptyArray = Array.Empty<string>();
            var nonEmptyArray = new[] { "item1", "item2" };

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(emptyArray));
            var errors = await rule.ValidateAsync(nonEmptyArray);
            Assert.Single(errors);
            Assert.Equal("Collection must be empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Handle_IEnumerable_Inputs()
        {
            // Arrange
            var rule = new EmptyValidationRule<int>();
            var emptyEnumerable = Enumerable.Empty<int>();
            var nonEmptyEnumerable = Enumerable.Range(1, 3);

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(emptyEnumerable));
            var errors = await rule.ValidateAsync(nonEmptyEnumerable);
            Assert.Single(errors);
            Assert.Equal("Collection must be empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_CancellationToken()
        {
            // Arrange
            var rule = new EmptyValidationRule<string>();
            var request = new List<string>();
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
            var rule = new EmptyValidationRule<string>();
            var request = new List<string> { "item" };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await rule.ValidateAsync(request, cts.Token));
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Large_Collections()
        {
            // Arrange
            var rule = new EmptyValidationRule<int>();
            var largeCollection = Enumerable.Range(1, 10000).ToList();

            // Act
            var errors = await rule.ValidateAsync(largeCollection);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Collection must be empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Custom_Object_Types()
        {
            // Arrange
            var rule = new EmptyValidationRule<TestObject>();
            var emptyCollection = new List<TestObject>();
            var nonEmptyCollection = new List<TestObject> { new TestObject() };

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(emptyCollection));
            var errors = await rule.ValidateAsync(nonEmptyCollection);
            Assert.Single(errors);
            Assert.Equal("Collection must be empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Value_Types()
        {
            // Arrange
            var rule = new EmptyValidationRule<DateTime>();
            var emptyCollection = new List<DateTime>();
            var nonEmptyCollection = new List<DateTime> { DateTime.Now };

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(emptyCollection));
            var errors = await rule.ValidateAsync(nonEmptyCollection);
            Assert.Single(errors);
            Assert.Equal("Collection must be empty.", errors.First());
        }

        [Fact]
        public async Task ValidateAsync_Should_Work_With_Nullable_Value_Types()
        {
            // Arrange
            var rule = new EmptyValidationRule<int?>();
            var emptyCollection = new List<int?>();
            var nonEmptyCollection = new List<int?> { 42, null };

            // Act & Assert
            Assert.Empty(await rule.ValidateAsync(emptyCollection));
            var errors = await rule.ValidateAsync(nonEmptyCollection);
            Assert.Single(errors);
            Assert.Equal("Collection must be empty.", errors.First());
        }

        private class TestObject
        {
            public string Name { get; set; } = "Test";
        }
    }
}