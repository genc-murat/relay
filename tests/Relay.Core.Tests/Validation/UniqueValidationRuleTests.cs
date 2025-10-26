using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class UniqueValidationRuleTests
{
    [Fact]
    public async Task ValidateAsync_WithNullCollection_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();

        // Act
        var result = await rule.ValidateAsync((IEnumerable<string>)null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyCollection_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
        var emptyCollection = new List<string>();

        // Act
        var result = await rule.ValidateAsync(emptyCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithUniqueItems_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
        var uniqueCollection = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = await rule.ValidateAsync(uniqueCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithDuplicateItems_ReturnsError()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
        var duplicateCollection = new List<string> { "item1", "item2", "item1" }; // "item1" appears twice

        // Act
        var result = await rule.ValidateAsync(duplicateCollection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain unique items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithSingleItemCollection_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
        var singleItemCollection = new List<string> { "item1" };

        // Act
        var result = await rule.ValidateAsync(singleItemCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithAllItemsDuplicate_ReturnsError()
    {
        // Arrange
        var rule = new UniqueValidationRule<int>();
        var allDuplicateCollection = new List<int> { 42, 42, 42, 42 }; // All items are the same

        // Act
        var result = await rule.ValidateAsync(allDuplicateCollection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain unique items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithLargeCollectionOfUniqueItems_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new UniqueValidationRule<int>();
        var uniqueCollection = Enumerable.Range(0, 1000).ToList(); // 1000 unique integers

        // Act
        var result = await rule.ValidateAsync(uniqueCollection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithLargeCollectionWithDuplicates_ReturnsError()
    {
        // Arrange
        var rule = new UniqueValidationRule<int>();
        var duplicateCollection = Enumerable.Range(0, 100).Concat(Enumerable.Range(0, 10)).ToList(); // Has duplicates

        // Act
        var result = await rule.ValidateAsync(duplicateCollection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain unique items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
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
        var rule = new UniqueValidationRule<string>();
        var array = new[] { "item1", "item2", "item3" };

        // Act
        var result = await rule.ValidateAsync(array);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithArray_WithDuplicates()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
        var array = new[] { "item1", "item2", "item1" }; // Has duplicates

        // Act
        var result = await rule.ValidateAsync(array);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain unique items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithDifferentGenericTypes()
    {
        // Test with integers
        var intRule = new UniqueValidationRule<int>();
        var intCollection = new List<int> { 1, 2, 3, 4, 5 }; // All unique

        var intResult = await intRule.ValidateAsync(intCollection);
        Assert.Empty(intResult);

        // Test with complex objects
        var objRule = new UniqueValidationRule<TestObject>();
        List<TestObject> objCollection =
        [
            new() { Id = 1, Name = "Test1" }, 
            new() { Id = 2, Name = "Test2" } // Different objects are unique
        ];

        var objResult = await objRule.ValidateAsync(objCollection);
        Assert.Empty(objResult);
    }

    [Fact]
    public async Task ValidateAsync_WithComplexObjects_HavingSameValues_But_Different_References_ReturnsEmpty()
    {
        // Arrange
        var rule = new UniqueValidationRule<TestObject>();
        var obj1 = new TestObject { Id = 1, Name = "Test1" };
        var obj2 = new TestObject { Id = 1, Name = "Test1" }; // Same values but different reference
        var collection = new List<TestObject> { obj1, obj2 };

        // Act
        var result = await rule.ValidateAsync(collection);

        // Note: Since TestObject doesn't override Equals/GetHashCode, 
        // these are different references and should be considered unique
        Assert.Empty(result); // They are different references, so they're unique
    }

    [Fact]
    public async Task ValidateAsync_WithComplexObjects_HavingSameValues_WithCustomEqualityComparer()
    {
        // Arrange
        var rule = new UniqueValidationRule<TestObjectWithOverride>();
        var obj1 = new TestObjectWithOverride { Id = 1, Name = "Test1" };
        var obj2 = new TestObjectWithOverride { Id = 1, Name = "Test1" }; // Same as obj1
        var collection = new List<TestObjectWithOverride> { obj1, obj2 };

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain unique items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithNullItems()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
        var collection = new List<string> { null, "item1", null }; // Two nulls

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain unique items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithAllNullItems()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
        var collection = new List<string> { null, null, null }; // All nulls

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Single(result);
        Assert.Equal("Collection must contain unique items.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithOnlyNullItem()
    {
        // Arrange
        var rule = new UniqueValidationRule<string>();
        var collection = new List<string> { null }; // Only one null

        // Act
        var result = await rule.ValidateAsync(collection);

        // Assert
        Assert.Empty(result);
    }

    public class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestObjectWithOverride
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is TestObjectWithOverride other)
            {
                return Id == other.Id && Name == other.Name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}