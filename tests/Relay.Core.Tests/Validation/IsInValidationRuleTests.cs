using Relay.Core.Validation.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class IsInValidationRuleTests
{
    [Fact]
    public void Constructor_WithIEnumerable_Should_Throw_When_AllowedValues_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new IsInValidationRule<string>((IEnumerable<string>)null));
    }

    [Fact]
    public void Constructor_WithParamsArray_Should_Throw_When_AllowedValues_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new IsInValidationRule<string>((string[])null));
    }

    [Fact]
    public async Task ValidateAsync_WithValidValue_ReturnsEmptyErrors()
    {
        // Arrange
        var rule = new IsInValidationRule<string>("option1", "option2", "option3");

        // Act
        var result = await rule.ValidateAsync("option2");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidValue_ReturnsError()
    {
        // Arrange
        var rule = new IsInValidationRule<string>("option1", "option2", "option3");

        // Act
        var result = await rule.ValidateAsync("option4");

        // Assert
        Assert.Single(result);
        Assert.Contains("option1, option2, option3", result.First());
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyAllowedValues_And_Matching_Value()
    {
        // Arrange
        var rule = new IsInValidationRule<string>(new string[0]);

        // Act
        var result = await rule.ValidateAsync("anyValue");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyAllowedValues_And_Null_Value()
    {
        // Arrange
        var rule = new IsInValidationRule<string>(new string[0]);

        // Act
        var result = await rule.ValidateAsync((string)null);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task ValidateAsync_WithNullValue_And_Null_In_AllowedList()
    {
        // Arrange
        var rule = new IsInValidationRule<string>(new string[] { null, "option1", "option2" });

        // Act
        var result = await rule.ValidateAsync((string)null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithNullValue_And_Null_Not_In_AllowedList()
    {
        // Arrange
        var rule = new IsInValidationRule<string>("option1", "option2", "option3");

        // Act
        var result = await rule.ValidateAsync((string)null);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task ValidateAsync_With_CancellationToken_Cancelled_Should_Throw()
    {
        // Arrange
        var rule = new IsInValidationRule<string>("option1", "option2");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync("option1", cts.Token));
    }

    [Theory]
    [InlineData("option1", true)]
    [InlineData("option2", true)]
    [InlineData("option3", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public async Task ValidateAsync_WithVariousStringValues(string inputValue, bool shouldPass)
    {
        // Arrange
        var rule = new IsInValidationRule<string>("option1", "option2", "option3");

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
    [InlineData(0, false)]
    public async Task ValidateAsync_WithVariousIntValues(int inputValue, bool shouldPass)
    {
        // Arrange
        var rule = new IsInValidationRule<int>(1, 2, 3);

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
        var allowedValues = new List<string> { "value1", "value2", "value3" };
        var rule = new IsInValidationRule<string>(allowedValues);

        // Act
        var validResult = await rule.ValidateAsync("value2");
        var invalidResult = await rule.ValidateAsync("value4");

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
        var rule = new IsInValidationRule<TestObject>(obj1, obj2);

        // Act
        var result = await rule.ValidateAsync(obj3); // obj3 has same values as obj1, so should pass due to overridden Equals

        // Assert
        // Since TestObject overrides Equals method, obj3 equals obj1 and should pass validation
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_WithErrorInErrorMessage()
    {
        // Arrange
        var rule = new IsInValidationRule<string>("option1", "option2", "option3");

        // Act
        var result = await rule.ValidateAsync("invalid_value");

        // Assert
        Assert.Single(result);
        var errorMessage = result.First();
        Assert.Contains("Value must be one of:", errorMessage);
        Assert.Contains("option1", errorMessage);
        Assert.Contains("option2", errorMessage);
        Assert.Contains("option3", errorMessage);
    }

    [Fact]
    public async Task ValidateAsync_WithSingleAllowedValue()
    {
        // Arrange
        var rule = new IsInValidationRule<string>("only_value");

        // Act
        var validResult = await rule.ValidateAsync("only_value");
        var invalidResult = await rule.ValidateAsync("other_value");

        // Assert
        Assert.Empty(validResult);
        Assert.Single(invalidResult);
    }

    [Fact]
    public async Task ValidateAsync_WithDuplicateAllowedValues()
    {
        // Arrange
        var rule = new IsInValidationRule<string>("duplicate", "duplicate", "unique");

        // Act
        var validResult = await rule.ValidateAsync("duplicate");
        var invalidResult = await rule.ValidateAsync("invalid");

        // Assert
        Assert.Empty(validResult);
        Assert.Single(invalidResult);
    }

    [Fact]
    public async Task ValidateAsync_WithLargeNumberOfAllowedValues()
    {
        // Arrange
        var allowedValues = Enumerable.Range(1, 1000).Select(i => $"value{i}").ToArray();
        var rule = new IsInValidationRule<string>(allowedValues);

        // Act
        var validResult = await rule.ValidateAsync("value500");
        var invalidResult = await rule.ValidateAsync("nonexistent");

        // Assert
        Assert.Empty(validResult);
        Assert.Single(invalidResult);
    }

    [Fact]
    public async Task ValidateAsync_WithEnumValues()
    {
        // Arrange
        var rule = new IsInValidationRule<TestEnum>(TestEnum.Value1, TestEnum.Value2);

        // Act
        var validResult = await rule.ValidateAsync(TestEnum.Value2);
        var invalidResult = await rule.ValidateAsync(TestEnum.Value3);

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