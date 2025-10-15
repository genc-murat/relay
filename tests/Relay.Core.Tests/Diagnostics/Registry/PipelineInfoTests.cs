using Relay.Core.Diagnostics.Registry;
using System;
using Xunit;

namespace Relay.Core.Tests.Diagnostics.Registry;

public class PipelineInfoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var pipelineInfo = new PipelineInfo();

        // Assert
        Assert.Equal(string.Empty, pipelineInfo.PipelineType);
        Assert.Equal(string.Empty, pipelineInfo.MethodName);
        Assert.Equal(0, pipelineInfo.Order);
        Assert.Equal(string.Empty, pipelineInfo.Scope);
        Assert.True(pipelineInfo.IsEnabled);
    }

    [Fact]
    public void PipelineType_ShouldGetAndSetValue()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();
        var expectedValue = "TestPipelineType";

        // Act
        pipelineInfo.PipelineType = expectedValue;

        // Assert
        Assert.Equal(expectedValue, pipelineInfo.PipelineType);
    }

    [Fact]
    public void MethodName_ShouldGetAndSetValue()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();
        var expectedValue = "HandleAsync";

        // Act
        pipelineInfo.MethodName = expectedValue;

        // Assert
        Assert.Equal(expectedValue, pipelineInfo.MethodName);
    }

    [Fact]
    public void Order_ShouldGetAndSetValue()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();
        var expectedValue = 42;

        // Act
        pipelineInfo.Order = expectedValue;

        // Assert
        Assert.Equal(expectedValue, pipelineInfo.Order);
    }

    [Fact]
    public void Scope_ShouldGetAndSetValue()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();
        var expectedValue = "Global";

        // Act
        pipelineInfo.Scope = expectedValue;

        // Assert
        Assert.Equal(expectedValue, pipelineInfo.Scope);
    }

    [Fact]
    public void IsEnabled_ShouldGetAndSetValue()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        pipelineInfo.IsEnabled = false;

        // Assert
        Assert.False(pipelineInfo.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var pipelineInfo = new PipelineInfo();

        // Assert
        Assert.True(pipelineInfo.IsEnabled);
    }

    [Fact]
    public void Properties_ShouldBeIndependent()
    {
        // Arrange
        var pipelineInfo1 = new PipelineInfo();
        var pipelineInfo2 = new PipelineInfo();

        // Act
        pipelineInfo1.PipelineType = "Type1";
        pipelineInfo1.MethodName = "Method1";
        pipelineInfo1.Order = 1;
        pipelineInfo1.Scope = "Scope1";
        pipelineInfo1.IsEnabled = false;

        pipelineInfo2.PipelineType = "Type2";
        pipelineInfo2.MethodName = "Method2";
        pipelineInfo2.Order = 2;
        pipelineInfo2.Scope = "Scope2";
        pipelineInfo2.IsEnabled = true;

        // Assert
        Assert.Equal("Type1", pipelineInfo1.PipelineType);
        Assert.Equal("Method1", pipelineInfo1.MethodName);
        Assert.Equal(1, pipelineInfo1.Order);
        Assert.Equal("Scope1", pipelineInfo1.Scope);
        Assert.False(pipelineInfo1.IsEnabled);

        Assert.Equal("Type2", pipelineInfo2.PipelineType);
        Assert.Equal("Method2", pipelineInfo2.MethodName);
        Assert.Equal(2, pipelineInfo2.Order);
        Assert.Equal("Scope2", pipelineInfo2.Scope);
        Assert.True(pipelineInfo2.IsEnabled);
    }

    [Fact]
    public void CanCreatePipelineInfoWithAllPropertiesSet()
    {
        // Act
        var pipelineInfo = new PipelineInfo
        {
            PipelineType = "MyPipelineBehavior",
            MethodName = "ProcessAsync",
            Order = 10,
            Scope = "Request",
            IsEnabled = false
        };

        // Assert
        Assert.Equal("MyPipelineBehavior", pipelineInfo.PipelineType);
        Assert.Equal("ProcessAsync", pipelineInfo.MethodName);
        Assert.Equal(10, pipelineInfo.Order);
        Assert.Equal("Request", pipelineInfo.Scope);
        Assert.False(pipelineInfo.IsEnabled);
    }

    [Fact]
    public void Order_CanBeNegative()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        pipelineInfo.Order = -5;

        // Assert
        Assert.Equal(-5, pipelineInfo.Order);
    }

    [Fact]
    public void Order_CanBeLargePositiveNumber()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        pipelineInfo.Order = int.MaxValue;

        // Assert
        Assert.Equal(int.MaxValue, pipelineInfo.Order);
    }

    [Fact]
    public void PipelineType_CanBeNull()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        pipelineInfo.PipelineType = null!;

        // Assert
        Assert.Null(pipelineInfo.PipelineType);
    }

    [Fact]
    public void MethodName_CanBeNull()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        pipelineInfo.MethodName = null!;

        // Assert
        Assert.Null(pipelineInfo.MethodName);
    }

    [Fact]
    public void Scope_CanBeNull()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        pipelineInfo.Scope = null!;

        // Assert
        Assert.Null(pipelineInfo.Scope);
    }

    [Fact]
    public void ToString_ShouldReturnTypeName()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        var result = pipelineInfo.ToString();

        // Assert
        Assert.Equal("Relay.Core.Diagnostics.Registry.PipelineInfo", result);
    }

    [Fact]
    public void GetHashCode_ShouldReturnConsistentValue()
    {
        // Arrange
        var pipelineInfo1 = new PipelineInfo();
        var pipelineInfo2 = new PipelineInfo();

        // Act
        var hash1 = pipelineInfo1.GetHashCode();
        var hash2 = pipelineInfo2.GetHashCode();

        // Assert - Hash codes should be consistent for same object state
        Assert.Equal(hash1, pipelineInfo1.GetHashCode()); // Same instance should return same hash
        // Different instances with same state may or may not have same hash
    }

    [Fact]
    public void Equals_ShouldReturnFalseForNull()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        var result = pipelineInfo.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentType()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();
        var otherObject = new object();

        // Act
        var result = pipelineInfo.Equals(otherObject);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnTrueForSameInstance()
    {
        // Arrange
        var pipelineInfo = new PipelineInfo();

        // Act
        var result = pipelineInfo.Equals(pipelineInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentPropertyValues()
    {
        // Arrange
        var pipelineInfo1 = new PipelineInfo
        {
            PipelineType = "Type1",
            MethodName = "Method1",
            Order = 1,
            Scope = "Scope1",
            IsEnabled = true
        };

        var pipelineInfo2 = new PipelineInfo
        {
            PipelineType = "Type2",
            MethodName = "Method2",
            Order = 2,
            Scope = "Scope2",
            IsEnabled = false
        };

        // Act
        var result = pipelineInfo1.Equals(pipelineInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnTrueForIdenticalPropertyValues()
    {
        // Arrange
        var pipelineInfo1 = new PipelineInfo
        {
            PipelineType = "Type1",
            MethodName = "Method1",
            Order = 1,
            Scope = "Scope1",
            IsEnabled = true
        };

        var pipelineInfo2 = new PipelineInfo
        {
            PipelineType = "Type1",
            MethodName = "Method1",
            Order = 1,
            Scope = "Scope1",
            IsEnabled = true
        };

        // Act
        var result = pipelineInfo1.Equals(pipelineInfo2);

        // Assert
        Assert.True(result);
    }
}