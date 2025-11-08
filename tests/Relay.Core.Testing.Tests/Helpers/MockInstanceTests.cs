using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class MockInstanceTests
{
    private interface ITestService
    {
        string GetValue();
        Task<string> GetValueAsync();
        void Process();
    }

    [Fact]
    public void Setup_WithNonMethodCallExpression_ThrowsArgumentException()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mockBuilder = helper.Mock<ITestService>();
        var mockInstance = mockBuilder.Instance;
        Expression<Func<ITestService, int>> invalidExpression = x => 1; // Not a method call

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            mockInstance.Setup(invalidExpression, 42));

        Assert.Contains("Expression must be a method call", exception.Message);
    }

    [Fact]
    public void Setup_WithFuncAndNonMethodCallExpression_ThrowsArgumentException()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mockBuilder = helper.Mock<ITestService>();
        var mockInstance = mockBuilder.Instance;
        Expression<Func<ITestService, int>> invalidExpression = x => 1; // Not a method call

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            mockInstance.Setup(invalidExpression, () => 42));

        Assert.Contains("Expression must be a method call", exception.Message);
    }

    [Fact]
    public void SetupThrows_WithNonMethodCallExpression_ThrowsArgumentException()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mockBuilder = helper.Mock<ITestService>();
        var mockInstance = mockBuilder.Instance;
        Expression<Func<ITestService, int>> invalidExpression = x => 1; // Not a method call

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            mockInstance.SetupThrows(invalidExpression, new Exception()));

        Assert.Contains("Expression must be a method call", exception.Message);
    }

    [Fact]
    public void SetupSequence_WithNonMethodCallExpression_ThrowsArgumentException()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mockBuilder = helper.Mock<ITestService>();
        var mockInstance = mockBuilder.Instance;
        Expression<Func<ITestService, int>> invalidExpression = x => 1; // Not a method call

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            mockInstance.SetupSequence(invalidExpression, new[] { 1, 2, 3 }));

        Assert.Contains("Expression must be a method call", exception.Message);
    }

    [Fact]
    public void Verify_WithNonMethodCallExpression_ThrowsArgumentException()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mockBuilder = helper.Mock<ITestService>();
        var mockInstance = mockBuilder.Instance;
        Expression<Func<ITestService, int>> invalidExpression = x => 1; // Not a method call

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            mockInstance.Verify(invalidExpression, CallTimes.Once()));

        Assert.Contains("Expression must be a method call", exception.Message);
    }

    [Fact]
    public void Verify_WithUnmatchedCallCount_ThrowsMockVerificationException()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mockBuilder = helper.Mock<ITestService>();
        var mockInstance = mockBuilder.Instance;
        Expression<Action<ITestService>> expression = x => x.Process();

        // Act & Assert
        var exception = Assert.Throws<MockVerificationException>(() =>
            mockInstance.Verify(expression, CallTimes.Once()));

        Assert.Contains("Expected exactly 1 calls to Process()", exception.Message);
        Assert.Contains("but received 0 calls", exception.Message);
    }

    [Fact]
    public void Invoke_WithNonExistentMethod_ThrowsInvalidOperationException()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mockBuilder = helper.Mock<ITestService>();
        var mockInstance = mockBuilder.Instance;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            mockInstance.Invoke("NonExistentMethod()", new object[0]));

        Assert.Contains("Method NonExistentMethod() not found on type ITestService", exception.Message);
    }
}