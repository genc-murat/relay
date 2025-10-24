using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class MaxCountValidationRuleTests
{
    [Fact]
    public void Constructor_Should_Throw_When_MaxCount_Is_Negative()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaxCountValidationRule<string>(-1));
    }

    [Fact]
    public void Constructor_Should_Accept_Zero_MaxCount()
    {
        // Act
        var rule = new MaxCountValidationRule<string>(0);

        // Assert - should not throw
        Assert.NotNull(rule);
    }

    [Fact]
    public void Constructor_Should_Accept_Positive_MaxCount()
    {
        // Act
        var rule = new MaxCountValidationRule<string>(5);

        // Assert - should not throw
        Assert.NotNull(rule);
    }

    [Fact]
    public async Task ValidateAsync_WithNullCollection_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(3);

        // Act
        var result = await rule.ValidateAsync((IEnumerable<string>)null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyCollection_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(3);
        var emptyCollection = new List<string>();

        // Act
        var result = await rule.ValidateAsync(emptyCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithinMaxCount_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(3);
        var validCollection = new List<string> { "item1", "item2" }; // 2 items, max is 3

        // Act
        var result = await rule.ValidateAsync(validCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_ExactlyAtMaxCount_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(3);
        var validCollection = new List<string> { "item1", "item2", "item3" }; // Exactly 3 items, max is 3

        // Act
        var result = await rule.ValidateAsync(validCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_OverMaxCount_ReturnsError()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(3);
        var invalidCollection = new List<string> { "item1", "item2", "item3", "item4" }; // 4 items, max is 3

        // Act
        var result = await rule.ValidateAsync(invalidCollection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at most 3 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithZeroMaxCount_And_EmptyCollection()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(0);
        var emptyCollection = new List<string>();

        // Act
        var result = await rule.ValidateAsync(emptyCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithZeroMaxCount_And_NonEmptyCollection()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(0);
        var nonEmptyCollection = new List<string> { "item1" }; // 1 item, max is 0

        // Act
        var result = await rule.ValidateAsync(nonEmptyCollection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at most 0 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithLargeMaxCount()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(1000);
        var collection = Enumerable.Range(0, 500).Select(i => $"item{i}").ToList();

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithLargeCollection_OverMaxCount()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(100);
        var collection = Enumerable.Range(0, 150).Select(i => $"item{i}").ToList();

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at most 100 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(3);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var collection = new List<string> { "item1", "item2" };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync(collection, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ValidateAsync_WithArray()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(5);
        var array = new[] { "item1", "item2", "item3" };

        // Act
        var result = await rule.ValidateAsync(array);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithArray_OverMaxCount()
    {
        // Arrange
        var rule = new MaxCountValidationRule<string>(2);
        var array = new[] { "item1", "item2", "item3" }; // 3 items, max is 2

        // Act
        var result = await rule.ValidateAsync(array);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain at most 2 items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithDifferentGenericTypes()
    {
        // Test with integers
        var intRule = new MaxCountValidationRule<int>(2);
        var intCollection = new List<int> { 1, 2, 3 }; // 3 items, max is 2

        var intResult = await intRule.ValidateAsync(intCollection);
        Assert.Single(intResult);
        Assert.Equal("Collection must contain at most 2 items.", intResult.First());

        // Test with complex objects
        var objRule = new MaxCountValidationRule<TestObject>(1);
        var objCollection = new List<TestObject> { new TestObject(), new TestObject() }; // 2 items, max is 1

        var objResult = await objRule.ValidateAsync(objCollection);
        Assert.Single(objResult);
        Assert.Equal("Collection must contain at most 1 items.", objResult.First());
    }

    public class TestObject
    {
        public int Id { get; set; } = 1;
        public string Name { get; set; } = "Test";
    }
}