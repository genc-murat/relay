using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class NotInValidationRuleTests
{
    [Fact]
    public void Constructor_WithIEnumerable_Should_Throw_When_ForbiddenValues_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NotInValidationRule<string>((IEnumerable<string>)null));
    }

    [Fact]
    public void Constructor_WithParamsArray_Should_Throw_When_ForbiddenValues_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NotInValidationRule<string>((string[])null));
    }

    [Fact]
    public async Task ValidateAsync_WithValidValue_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new NotInValidationRule<string>("forbidden1", "forbidden2", "forbidden3");

        // Act
        var result = await rule.ValidateAsync("allowed_value");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithForbiddenValue_ReturnsError()
    {
        // Arrange
        var rule = new NotInValidationRule<string>("forbidden1", "forbidden2", "forbidden3");

        // Act
        var result = await rule.ValidateAsync("forbidden2");

        // Assert
        Assert.Single(result);
        Assert.Contains("must not be one of", result.First().ToLower());
        Assert.Contains("forbidden1", result.First());
        Assert.Contains("forbidden2", result.First());
        Assert.Contains("forbidden3", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyForbiddenList_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new NotInValidationRule<string>(new string[0]);

        // Act
        var result = await rule.ValidateAsync("any_value");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithNullForbiddenList()
    {
        // Arrange
        var rule = new NotInValidationRule<string>(new string[0]);

        // Act
        var result = await rule.ValidateAsync((string)null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithNullValue_And_Null_In_ForbiddenList()
    {
        // Arrange
        var rule = new NotInValidationRule<string>(new string[] { null, "forbidden1", "forbidden2" });

        // Act
        var result = await rule.ValidateAsync((string)null);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task ValidateAsync_WithNullValue_And_Null_Not_In_ForbiddenList()
    {
        // Arrange
        var rule = new NotInValidationRule<string>("forbidden1", "forbidden2", "forbidden3");

        // Act
        var result = await rule.ValidateAsync((string)null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_CancellationTokenCancelled_Should_Throw()
    {
        // Arrange
        var rule = new NotInValidationRule<string>("forbidden1", "forbidden2");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync("value", cts.Token));
    }

    [Theory]
    [InlineData("allowed1", true)]
    [InlineData("allowed2", true)]
    [InlineData("forbidden1", false)]
    [InlineData("forbidden2", false)]
    [InlineData("forbidden3", false)]
    public async Task ValidateAsync_WithVariousStringValues(string inputValue, bool shouldPass)
    {
        // Arrange
        var rule = new NotInValidationRule<string>("forbidden1", "forbidden2", "forbidden3");

        // Act
        var result = await rule.ValidateAsync(inputValue);

        // Assert
        if (shouldPass)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    public async Task ValidateAsync_WithVariousIntValues(int inputValue, bool shouldPass)
    {
        // Arrange
        var rule = new NotInValidationRule<int>(4, 5, 6);

        // Act
        var result = await rule.ValidateAsync(inputValue);

        // Assert
        if (shouldPass)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
        }
    }

    [Fact]
    public async Task Constructor_WithIEnumerableCollection_Should_Work()
    {
        // Arrange
        var forbiddenValues = new List<string> { "value1", "value2", "value3" };
        var rule = new NotInValidationRule<string>(forbiddenValues);

        // Act
        var validResult = await rule.ValidateAsync("value4");
        var invalidResult = await rule.ValidateAsync("value2");

        // Assert
        Assert.Empty(validResult);
        Assert.Single(invalidResult);
    }

    [Fact]
    public async Task ValidateAsync_WithComplexObjects_UsesDefaultEqualityComparer()
    {
        // Arrange
        var obj1 = new TestObject { Id = 1, Name = "Test1" };
        var obj2 = new TestObject { Id = 2, Name = "Test2" };
        var obj3 = new TestObject { Id = 1, Name = "Test1" }; // Same values as obj1
        var rule = new NotInValidationRule<TestObject>(obj1, obj2);

        // Act
        var result = await rule.ValidateAsync(obj3); // obj3 has same values as obj1

        // Assert
        // For reference types with overridden Equals method, obj3 should be considered equal to obj1
        Assert.Single(result); // Should fail because obj3 equals obj1
    }

    [Fact]
    public async Task ValidateAsync_WithErrorInErrorMessage()
    {
        // Arrange
        var rule = new NotInValidationRule<string>("forbidden1", "forbidden2", "forbidden3");

        // Act
        var result = await rule.ValidateAsync("forbidden2");

        // Assert
        Assert.Single(result);
        var errorMessage = result.First();
        Assert.Contains("Value must not be one of:", errorMessage);
        Assert.Contains("forbidden1", errorMessage);
        Assert.Contains("forbidden2", errorMessage);
        Assert.Contains("forbidden3", errorMessage);
    }

    [Fact]
    public async Task ValidateAsync_WithSingleForbiddenValue()
    {
        // Arrange
        var rule = new NotInValidationRule<string>("only_forbidden_value");

        // Act
        var validResult = await rule.ValidateAsync("allowed_value");
        var invalidResult = await rule.ValidateAsync("only_forbidden_value");

        // Assert
        Assert.Empty(validResult);
        Assert.Single(invalidResult);
    }

    [Fact]
    public async Task ValidateAsync_WithDuplicateForbiddenValues()
    {
        // Arrange
        var rule = new NotInValidationRule<string>("duplicate", "duplicate", "unique");

        // Act
        var result = await rule.ValidateAsync("duplicate");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task ValidateAsync_WithLargeNumberOfForbiddenValues()
    {
        // Arrange
        var forbiddenValues = Enumerable.Range(1, 1000).Select(i => $"value{i}").ToArray();
        var rule = new NotInValidationRule<string>(forbiddenValues);

        // Act
        var validResult = await rule.ValidateAsync("allowed_value");
        var invalidResult = await rule.ValidateAsync("value500");

        // Assert
        Assert.Empty(validResult);
        Assert.Single(invalidResult);
    }

    [Fact]
    public async Task ValidateAsync_WithEnumValues()
    {
        // Arrange
        var rule = new NotInValidationRule<TestEnum>(TestEnum.Value1, TestEnum.Value2);

        // Act
        var validResult = await rule.ValidateAsync(TestEnum.Value3);
        var invalidResult = await rule.ValidateAsync(TestEnum.Value2);

        // Assert
        Assert.Empty(validResult);
        Assert.Single(invalidResult);
    }

    public class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is TestObject other)
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

    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }
}