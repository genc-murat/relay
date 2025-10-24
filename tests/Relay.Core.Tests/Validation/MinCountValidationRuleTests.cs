using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class MinCountValidationRuleTests
{
    [Fact]
    public void Constructor_Should_Throw_When_MinCount_Is_Negative()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MinCountValidationRule<string>(-1));
    }

    [Fact]
    public void Constructor_Should_Accept_Zero_MinCount()
    {
        // Act
        var rule = new MinCountValidationRule<string>(0);

        // Assert - should not throw
        Assert.NotNull(rule);
    }

    [Fact]
    public void Constructor_Should_Accept_Positive_MinCount()
    {
        // Act
        var rule = new MinCountValidationRule<string>(5);

        // Assert - should not throw
        Assert.NotNull(rule);
    }

    [Fact]
    public async Task ValidateAsync_WithNullCollection_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(3);

        // Act
        var result = await rule.ValidateAsync((IEnumerable<string>)null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyCollection_ReturnsEmptyErrors_When_MinCount_Is_Zero()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(0);
        var emptyCollection = new List<string>();

        // Act
        var result = await rule.ValidateAsync(emptyCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyCollection_ReturnsError_When_MinCount_Is_Positive()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(3);
        var emptyCollection = new List<string>();

        // Act
        var result = await rule.ValidateAsync(emptyCollection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at least 3 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithinMinCount_ReturnsError()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(3);
        var invalidCollection = new List<string> { "item1", "item2" }; // 2 items, min is 3

        // Act
        var result = await rule.ValidateAsync(invalidCollection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at least 3 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_ExactlyAtMinCount_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(3);
        var validCollection = new List<string> { "item1", "item2", "item3" }; // Exactly 3 items, min is 3

        // Act
        var result = await rule.ValidateAsync(validCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_OverMinCount_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(3);
        var validCollection = new List<string> { "item1", "item2", "item3", "item4" }; // 4 items, min is 3

        // Act
        var result = await rule.ValidateAsync(validCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithZeroMinCount_And_EmptyCollection()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(0);
        var emptyCollection = new List<string>();

        // Act
        var result = await rule.ValidateAsync(emptyCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithZeroMinCount_And_NonEmptyCollection()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(0);
        var nonEmptyCollection = new List<string> { "item1" }; // 1 item, min is 0

        // Act
        var result = await rule.ValidateAsync(nonEmptyCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithLargeMinCount()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(100);
        var collection = Enumerable.Range(0, 150).Select(i => $"item{i}").ToList();

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithLargeCollection_UnderMinCount()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(1000);
        var collection = Enumerable.Range(0, 100).Select(i => $"item{i}").ToList(); // 100 items, min is 1000

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at least 1000 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(3);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var collection = new List<string> { "item1", "item2", "item3" };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync(collection, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ValidateAsync_WithArray()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(2);
        var array = new[] { "item1", "item2", "item3" }; // 3 items, min is 2

        // Act
        var result = await rule.ValidateAsync(array);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithArray_UnderMinCount()
    {
        // Arrange
        var rule = new MinCountValidationRule<string>(5);
        var array = new[] { "item1", "item2", "item3" }; // 3 items, min is 5

        // Act
        var result = await rule.ValidateAsync(array);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at least 5 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithSingleItemCollection()
    {
        // Arrange
        var rule = new MinCountValidationRule<int>(1);
        var collection = new List<int> { 42 }; // 1 item, min is 1

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithSingleItemCollection_UnderMinCount()
    {
        // Arrange
        var rule = new MinCountValidationRule<int>(3);
        var collection = new List<int> { 42 }; // 1 item, min is 3

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at least 3 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithDifferentGenericTypes()
    {
        // Test with integers
        var intRule = new MinCountValidationRule<int>(2);
        var intCollection = new List<int> { 1 }; // 1 item, min is 2

        var intResult = await intRule.ValidateAsync(intCollection);
        Assert.Single(intResult);
        Assert.Equal("Collection must contain at least 2 items.", intResult.First());

        // Test with complex objects
        var objRule = new MinCountValidationRule<TestObject>(3);
        var objCollection = new List<TestObject> { new TestObject() }; // 1 item, min is 3

        var objResult = await objRule.ValidateAsync(objCollection);
        Assert.Single(objResult);
        Assert.Equal("Collection must contain at least 3 items.", objResult.First());
    }

    public class TestObject
    {
        public int Id { get; set; } = 1;
        public string Name { get; set; } = "Test";
    }
}