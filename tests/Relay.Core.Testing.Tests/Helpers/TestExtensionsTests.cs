using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class TestExtensionsTests
{
    #region Async Extensions

    [Fact]
    public async Task ShouldCompleteWithin_Action_CompletesWithinTimeout()
    {
        // Arrange
        var action = () => Task.Delay(50);

        // Act & Assert - Should not throw
        await TestExtensions.ShouldCompleteWithin(action, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldCompleteWithin_Action_ThrowsOnTimeout()
    {
        // Arrange
        var action = () => Task.Delay(1000);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            TestExtensions.ShouldCompleteWithin(action, TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public async Task ShouldCompleteWithin_Function_CompletesWithinTimeout()
    {
        // Arrange
        var function = () => Task.FromResult(42);

        // Act
        var result = await TestExtensions.ShouldCompleteWithin(function, TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ShouldCompleteWithin_Function_ThrowsOnTimeout()
    {
        // Arrange
        var function = () => Task.Delay(1000).ContinueWith(_ => 42);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            TestExtensions.ShouldCompleteWithin(function, TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public async Task ShouldThrow_Action_ThrowsExpectedException()
    {
        // Arrange
        var action = () => Task.FromException(new InvalidOperationException("Test exception"));

        // Act
        var exception = await action.ShouldThrow<InvalidOperationException>();

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task ShouldThrow_Action_ThrowsAssertionExceptionWhenNoException()
    {
        // Arrange
        var action = () => Task.CompletedTask;

        // Act & Assert
        await Assert.ThrowsAsync<AssertionException>(() =>
            action.ShouldThrow<InvalidOperationException>());
    }

    [Fact]
    public async Task ShouldThrow_Function_ThrowsExpectedException()
    {
        // Arrange
        var function = () => Task.FromException<int>(new InvalidOperationException("Test exception"));

        // Act
        var exception = await function.ShouldThrow<int, InvalidOperationException>();

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task MeasureExecutionTime_Action_ReturnsExecutionTime()
    {
        // Arrange
        var action = () => Task.Delay(50);

        // Act
        var executionTime = await action.MeasureExecutionTime();

        // Assert
        Assert.True(executionTime >= TimeSpan.FromMilliseconds(40));
        Assert.True(executionTime <= TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task MeasureExecutionTime_Function_ReturnsResultAndExecutionTime()
    {
        // Arrange
        var function = () => Task.Delay(50).ContinueWith(_ => 42);

        // Act
        var (result, executionTime) = await function.MeasureExecutionTime();

        // Assert
        Assert.Equal(42, result);
        Assert.True(executionTime >= TimeSpan.FromMilliseconds(40));
        Assert.True(executionTime <= TimeSpan.FromMilliseconds(200));
    }

    #endregion

    #region Collection Extensions

    [Fact]
    public void ShouldContain_CollectionContainsItem_DoesNotThrow()
    {
        // Arrange
        var collection = new[] { 1, 2, 3, 4, 5 };

        // Act & Assert - Should not throw
        collection.ShouldContain(3);
    }

    [Fact]
    public void ShouldContain_CollectionDoesNotContainItem_ThrowsAssertionException()
    {
        // Arrange
        var collection = new[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        Assert.Throws<AssertionException>(() => collection.ShouldContain(6));
    }

    [Fact]
    public void ShouldNotContain_CollectionDoesNotContainItem_DoesNotThrow()
    {
        // Arrange
        var collection = new[] { 1, 2, 3, 4, 5 };

        // Act & Assert - Should not throw
        collection.ShouldNotContain(6);
    }

    [Fact]
    public void ShouldNotContain_CollectionContainsItem_ThrowsAssertionException()
    {
        // Arrange
        var collection = new[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        Assert.Throws<AssertionException>(() => collection.ShouldNotContain(3));
    }

    [Fact]
    public void ShouldBeEmpty_EmptyCollection_DoesNotThrow()
    {
        // Arrange
        var collection = Array.Empty<int>();

        // Act & Assert - Should not throw
        collection.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldBeEmpty_NonEmptyCollection_ThrowsAssertionException()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<AssertionException>(() => collection.ShouldBeEmpty());
    }

    [Fact]
    public void ShouldNotBeEmpty_NonEmptyCollection_DoesNotThrow()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act & Assert - Should not throw
        collection.ShouldNotBeEmpty();
    }

    [Fact]
    public void ShouldNotBeEmpty_EmptyCollection_ThrowsAssertionException()
    {
        // Arrange
        var collection = Array.Empty<int>();

        // Act & Assert
        Assert.Throws<AssertionException>(() => collection.ShouldNotBeEmpty());
    }

    [Fact]
    public void ShouldHaveCount_CorrectCount_DoesNotThrow()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act & Assert - Should not throw
        collection.ShouldHaveCount(3);
    }

    [Fact]
    public void ShouldHaveCount_IncorrectCount_ThrowsAssertionException()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<AssertionException>(() => collection.ShouldHaveCount(5));
    }

    [Fact]
    public void ShouldAll_AllItemsSatisfyPredicate_DoesNotThrow()
    {
        // Arrange
        var collection = new[] { 2, 4, 6, 8, 10 };

        // Act & Assert - Should not throw
        collection.ShouldAll(x => x % 2 == 0);
    }

    [Fact]
    public void ShouldAll_SomeItemsFailPredicate_ThrowsAssertionException()
    {
        // Arrange
        var collection = new[] { 2, 4, 5, 8, 10 };

        // Act & Assert
        Assert.Throws<AssertionException>(() => collection.ShouldAll(x => x % 2 == 0));
    }

    [Fact]
    public void ShouldAny_AtLeastOneItemSatisfiesPredicate_DoesNotThrow()
    {
        // Arrange
        var collection = new[] { 1, 3, 5, 7, 8 };

        // Act & Assert - Should not throw
        collection.ShouldAny(x => x % 2 == 0);
    }

    [Fact]
    public void ShouldAny_NoItemsSatisfyPredicate_ThrowsAssertionException()
    {
        // Arrange
        var collection = new[] { 1, 3, 5, 7, 9 };

        // Act & Assert
        Assert.Throws<AssertionException>(() => collection.ShouldAny(x => x % 2 == 0));
    }

    #endregion

    #region Object Extensions

    [Fact]
    public void ShouldNotBeNull_NonNullObject_DoesNotThrow()
    {
        // Arrange
        var obj = new object();

        // Act & Assert - Should not throw
        obj.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldNotBeNull_NullObject_ThrowsAssertionException()
    {
        // Arrange
        object obj = null!;

        // Act & Assert
        Assert.Throws<AssertionException>(() => obj.ShouldNotBeNull());
    }

    [Fact]
    public void ShouldBeNull_NullObject_DoesNotThrow()
    {
        // Arrange
        object obj = null!;

        // Act & Assert - Should not throw
        obj.ShouldBeNull();
    }

    [Fact]
    public void ShouldBeNull_NonNullObject_ThrowsAssertionException()
    {
        // Arrange
        var obj = new object();

        // Act & Assert
        Assert.Throws<AssertionException>(() => obj.ShouldBeNull());
    }

    [Fact]
    public void ShouldEqual_EqualObjects_DoesNotThrow()
    {
        // Arrange
        var obj1 = "test";
        var obj2 = "test";

        // Act & Assert - Should not throw
        obj1.ShouldEqual(obj2);
    }

    [Fact]
    public void ShouldEqual_UnequalObjects_ThrowsAssertionException()
    {
        // Arrange
        var obj1 = "test1";
        var obj2 = "test2";

        // Act & Assert
        Assert.Throws<AssertionException>(() => obj1.ShouldEqual(obj2));
    }

    [Fact]
    public void ShouldNotEqual_UnequalObjects_DoesNotThrow()
    {
        // Arrange
        var obj1 = "test1";
        var obj2 = "test2";

        // Act & Assert - Should not throw
        obj1.ShouldNotEqual(obj2);
    }

    [Fact]
    public void ShouldNotEqual_EqualObjects_ThrowsAssertionException()
    {
        // Arrange
        var obj1 = "test";
        var obj2 = "test";

        // Act & Assert
        Assert.Throws<AssertionException>(() => obj1.ShouldNotEqual(obj2));
    }

    [Fact]
    public void ShouldBeOfType_CorrectType_DoesNotThrow()
    {
        // Arrange
        var obj = "test";

        // Act & Assert - Should not throw
        obj.ShouldBeOfType(typeof(string));
    }

    [Fact]
    public void ShouldBeOfType_IncorrectType_ThrowsAssertionException()
    {
        // Arrange
        var obj = "test";

        // Act & Assert
        Assert.Throws<AssertionException>(() => obj.ShouldBeOfType(typeof(int)));
    }

    [Fact]
    public void ShouldBeAssignableTo_AssignableType_DoesNotThrow()
    {
        // Arrange
        var obj = "test";

        // Act & Assert - Should not throw
        obj.ShouldBeAssignableTo(typeof(object));
    }

    [Fact]
    public void ShouldBeAssignableTo_NotAssignableType_ThrowsAssertionException()
    {
        // Arrange
        var obj = "test";

        // Act & Assert
        Assert.Throws<AssertionException>(() => obj.ShouldBeAssignableTo(typeof(int)));
    }

    #endregion

    #region String Extensions

    [Fact]
    public void ShouldNotBeNullOrEmpty_NonEmptyString_DoesNotThrow()
    {
        // Arrange
        var str = "test";

        // Act & Assert - Should not throw
        str.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ShouldNotBeNullOrEmpty_EmptyString_ThrowsAssertionException()
    {
        // Arrange
        var str = string.Empty;

        // Act & Assert
        Assert.Throws<AssertionException>(() => str.ShouldNotBeNullOrEmpty());
    }

    [Fact]
    public void ShouldNotBeNullOrEmpty_NullString_ThrowsAssertionException()
    {
        // Arrange
        string str = null!;

        // Act & Assert
        Assert.Throws<AssertionException>(() => str.ShouldNotBeNullOrEmpty());
    }

    [Fact]
    public void ShouldNotBeNullOrWhiteSpace_NonWhitespaceString_DoesNotThrow()
    {
        // Arrange
        var str = "test";

        // Act & Assert - Should not throw
        str.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ShouldNotBeNullOrWhiteSpace_WhitespaceString_ThrowsAssertionException()
    {
        // Arrange
        var str = "   ";

        // Act & Assert
        Assert.Throws<AssertionException>(() => str.ShouldNotBeNullOrWhiteSpace());
    }

    [Fact]
    public void ShouldContain_StringContainsSubstring_DoesNotThrow()
    {
        // Arrange
        var str = "Hello World";

        // Act & Assert - Should not throw
        str.ShouldContain("World");
    }

    [Fact]
    public void ShouldContain_StringDoesNotContainSubstring_ThrowsAssertionException()
    {
        // Arrange
        var str = "Hello World";

        // Act & Assert
        Assert.Throws<AssertionException>(() => str.ShouldContain("Universe"));
    }

    [Fact]
    public void ShouldStartWith_StringStartsWithPrefix_DoesNotThrow()
    {
        // Arrange
        var str = "Hello World";

        // Act & Assert - Should not throw
        str.ShouldStartWith("Hello");
    }

    [Fact]
    public void ShouldStartWith_StringDoesNotStartWithPrefix_ThrowsAssertionException()
    {
        // Arrange
        var str = "Hello World";

        // Act & Assert
        Assert.Throws<AssertionException>(() => str.ShouldStartWith("World"));
    }

    [Fact]
    public void ShouldEndWith_StringEndsWithSuffix_DoesNotThrow()
    {
        // Arrange
        var str = "Hello World";

        // Act & Assert - Should not throw
        str.ShouldEndWith("World");
    }

    [Fact]
    public void ShouldEndWith_StringDoesNotEndWithSuffix_ThrowsAssertionException()
    {
        // Arrange
        var str = "Hello World";

        // Act & Assert
        Assert.Throws<AssertionException>(() => str.ShouldEndWith("Hello"));
    }

    #endregion

    #region Numeric Extensions

    [Fact]
    public void ShouldBeGreaterThan_ValueGreaterThanThreshold_DoesNotThrow()
    {
        // Arrange
        var value = 10;

        // Act & Assert - Should not throw
        value.ShouldBeGreaterThan(5);
    }

    [Fact]
    public void ShouldBeGreaterThan_ValueEqualToThreshold_ThrowsAssertionException()
    {
        // Arrange
        var value = 5;

        // Act & Assert
        Assert.Throws<AssertionException>(() => value.ShouldBeGreaterThan(5));
    }

    [Fact]
    public void ShouldBeGreaterThan_ValueLessThanThreshold_ThrowsAssertionException()
    {
        // Arrange
        var value = 3;

        // Act & Assert
        Assert.Throws<AssertionException>(() => value.ShouldBeGreaterThan(5));
    }

    [Fact]
    public void ShouldBeLessThan_ValueLessThanThreshold_DoesNotThrow()
    {
        // Arrange
        var value = 3;

        // Act & Assert - Should not throw
        value.ShouldBeLessThan(5);
    }

    [Fact]
    public void ShouldBeLessThan_ValueEqualToThreshold_ThrowsAssertionException()
    {
        // Arrange
        var value = 5;

        // Act & Assert
        Assert.Throws<AssertionException>(() => value.ShouldBeLessThan(5));
    }

    [Fact]
    public void ShouldBeLessThan_ValueGreaterThanThreshold_ThrowsAssertionException()
    {
        // Arrange
        var value = 10;

        // Act & Assert
        Assert.Throws<AssertionException>(() => value.ShouldBeLessThan(5));
    }

    [Fact]
    public void ShouldBeInRange_ValueInRange_DoesNotThrow()
    {
        // Arrange
        var value = 5;

        // Act & Assert - Should not throw
        value.ShouldBeInRange(1, 10);
    }

    [Fact]
    public void ShouldBeInRange_ValueBelowRange_ThrowsAssertionException()
    {
        // Arrange
        var value = 0;

        // Act & Assert
        Assert.Throws<AssertionException>(() => value.ShouldBeInRange(1, 10));
    }

    [Fact]
    public void ShouldBeInRange_ValueAboveRange_ThrowsAssertionException()
    {
        // Arrange
        var value = 15;

        // Act & Assert
        Assert.Throws<AssertionException>(() => value.ShouldBeInRange(1, 10));
    }

    #endregion

    #region Boolean Extensions

    [Fact]
    public void ShouldBeTrue_TrueValue_DoesNotThrow()
    {
        // Arrange
        var value = true;

        // Act & Assert - Should not throw
        value.ShouldBeTrue();
    }

    [Fact]
    public void ShouldBeTrue_FalseValue_ThrowsAssertionException()
    {
        // Arrange
        var value = false;

        // Act & Assert
        Assert.Throws<AssertionException>(() => value.ShouldBeTrue());
    }

    [Fact]
    public void ShouldBeFalse_FalseValue_DoesNotThrow()
    {
        // Arrange
        var value = false;

        // Act & Assert - Should not throw
        value.ShouldBeFalse();
    }

    [Fact]
    public void ShouldBeFalse_TrueValue_ThrowsAssertionException()
    {
        // Arrange
        var value = true;

        // Act & Assert
        Assert.Throws<AssertionException>(() => value.ShouldBeFalse());
    }

    #endregion

    #region Time Extensions

    [Fact]
    public void ShouldBeCloseTo_TimeSpanWithinTolerance_DoesNotThrow()
    {
        // Arrange
        var actual = TimeSpan.FromSeconds(5);
        var expected = TimeSpan.FromSeconds(5.1);
        var tolerance = TimeSpan.FromSeconds(0.2);

        // Act & Assert - Should not throw
        actual.ShouldBeCloseTo(expected, tolerance);
    }

    [Fact]
    public void ShouldBeCloseTo_TimeSpanOutsideTolerance_ThrowsAssertionException()
    {
        // Arrange
        var actual = TimeSpan.FromSeconds(5);
        var expected = TimeSpan.FromSeconds(6);
        var tolerance = TimeSpan.FromSeconds(0.5);

        // Act & Assert
        Assert.Throws<AssertionException>(() => actual.ShouldBeCloseTo(expected, tolerance));
    }

    [Fact]
    public void ShouldBeCloseTo_DateTimeWithinTolerance_DoesNotThrow()
    {
        // Arrange
        var actual = new DateTime(2023, 1, 1, 12, 0, 0);
        var expected = new DateTime(2023, 1, 1, 12, 0, 5);
        var tolerance = TimeSpan.FromSeconds(10);

        // Act & Assert - Should not throw
        actual.ShouldBeCloseTo(expected, tolerance);
    }

    [Fact]
    public void ShouldBeCloseTo_DateTimeOutsideTolerance_ThrowsAssertionException()
    {
        // Arrange
        var actual = new DateTime(2023, 1, 1, 12, 0, 0);
        var expected = new DateTime(2023, 1, 1, 13, 0, 0);
        var tolerance = TimeSpan.FromMinutes(30);

        // Act & Assert
        Assert.Throws<AssertionException>(() => actual.ShouldBeCloseTo(expected, tolerance));
    }

    #endregion

    #region Test Context Extensions

    [Fact]
    public void WithScenario_CreatesScenarioWithName()
    {
        // Arrange
        var testClass = new object();

        // Act
        var scenario = testClass.WithScenario("Test Scenario");

        // Assert
        Assert.NotNull(scenario);
        Assert.Equal("Test Scenario", scenario.Name);
    }

    [Fact]
    public void WithScenario_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var testClass = new object();
        var configured = false;

        // Act
        var scenario = testClass.WithScenario("Test Scenario", s => configured = true);

        // Assert
        Assert.True(configured);
    }

    [Fact]
    public async Task InIsolation_Action_ExecutesInIsolation()
    {
        // Arrange
        var testClass = new object();
        var executed = false;

        // Act
        await testClass.InIsolation(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task InIsolation_Function_ExecutesInIsolationAndReturnsResult()
    {
        // Arrange
        var testClass = new object();

        // Act
        var result = await testClass.InIsolation(() => Task.FromResult(42));

        // Assert
        Assert.Equal(42, result);
    }

    #endregion
}