using System;
using System.Data;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions;

/// <summary>
/// Tests for TransactionAttribute and mandatory isolation level support.
/// </summary>
public class TransactionAttributeTests
{
    [Fact]
    public void TransactionAttribute_Should_Require_IsolationLevel()
    {
        // Act
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted);

        // Assert
        Assert.Equal(IsolationLevel.ReadCommitted, attribute.IsolationLevel);
    }

    [Fact]
    public void TransactionAttribute_Should_Reject_Unspecified_IsolationLevel()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TransactionAttribute(IsolationLevel.Unspecified));
    }

    [Theory]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    public void TransactionAttribute_Should_Support_All_Isolation_Levels(IsolationLevel level)
    {
        // Act
        var attribute = new TransactionAttribute(level);

        // Assert
        Assert.Equal(level, attribute.IsolationLevel);
    }

    [Fact]
    public void TransactionAttribute_Should_Have_Default_Timeout()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted);

        // Assert
        Assert.Equal(30, attribute.TimeoutSeconds);
    }

    [Fact]
    public void TransactionAttribute_Should_Allow_Custom_Timeout()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            TimeoutSeconds = 60
        };

        // Assert
        Assert.Equal(60, attribute.TimeoutSeconds);
    }

    [Fact]
    public void TransactionAttribute_Should_Default_IsReadOnly_To_False()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted);

        // Assert
        Assert.False(attribute.IsReadOnly);
    }

    [Fact]
    public void TransactionAttribute_Should_Support_ReadOnly_Flag()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            IsReadOnly = true
        };

        // Assert
        Assert.True(attribute.IsReadOnly);
    }

    [Fact]
    public void TransactionAttribute_Should_Default_UseDistributedTransaction_To_False()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted);

        // Assert
        Assert.False(attribute.UseDistributedTransaction);
    }

    [Fact]
    public void TransactionAttribute_Should_Support_DistributedTransaction_Flag()
    {
        // Arrange & Act
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            UseDistributedTransaction = true
        };

        // Assert
        Assert.True(attribute.UseDistributedTransaction);
    }

    [Fact]
    public void TransactionRetryAttribute_Should_Have_Default_Values()
    {
        // Arrange & Act
        var attribute = new TransactionRetryAttribute();

        // Assert
        Assert.Equal(3, attribute.MaxRetries);
        Assert.Equal(100, attribute.InitialDelayMs);
        Assert.Equal(RetryStrategy.ExponentialBackoff, attribute.Strategy);
    }

    [Fact]
    public void TransactionRetryAttribute_Should_Allow_Custom_Values()
    {
        // Arrange & Act
        var attribute = new TransactionRetryAttribute
        {
            MaxRetries = 5,
            InitialDelayMs = 200,
            Strategy = RetryStrategy.Linear
        };

        // Assert
        Assert.Equal(5, attribute.MaxRetries);
        Assert.Equal(200, attribute.InitialDelayMs);
        Assert.Equal(RetryStrategy.Linear, attribute.Strategy);
    }

    [Fact]
    public void Validate_Should_Throw_When_IsolationLevel_Is_Unspecified()
    {
        // Note: The constructor of TransactionAttribute prevents creating an instance 
        // with IsolationLevel.Unspecified, so this validation check in Validate() method 
        // is a defensive measure. To test it, we need to use reflection to create an instance 
        // and manipulate the private backing field of the auto-property.
        
        // Create an instance using reflection to bypass constructor validation
        var attribute = CreateUninitializedTransactionAttribute();
        
        // Use reflection to set the backing field for the IsolationLevel auto-property
        var isolationLevelField = typeof(TransactionAttribute).GetField("<IsolationLevel>k__BackingField", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (isolationLevelField != null)
        {
            // Set the backing field directly to Unspecified
            isolationLevelField.SetValue(attribute, IsolationLevel.Unspecified);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
            Assert.Contains("Transaction isolation level cannot be Unspecified", exception.Message);
        }
        else
        {
            // Fallback: if the backing field name is different, we could try other approaches
            // In this case, since the property is auto-implemented, the backing field should have this name
            Assert.Fail("Could not find the backing field for IsolationLevel property");
        }
    }

    // Helper method to create an uninitialized instance without using FormatterServices
    private TransactionAttribute CreateUninitializedTransactionAttribute()
    {
        // Create instance using Activator.CreateInstance with non-public constructor
        var constructor = typeof(TransactionAttribute).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            System.Reflection.CallingConventions.Any,
            new Type[] { },
            null);
        
        if (constructor != null)
        {
            return (TransactionAttribute)constructor.Invoke(new object[] { });
        }
        
        // If no parameterless constructor exists, try FormatterServices as fallback
        // even though it's obsolete, we'll mark it with the proper suppress attribute
#pragma warning disable SYSLIB0050 // Type or member is obsolete
        return (TransactionAttribute)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(TransactionAttribute));
#pragma warning restore SYSLIB0050 // Type or member is obsolete
    }

    [Fact]
    public void Validate_Should_Throw_When_TimeoutSeconds_Is_Less_Than_Negative_One()
    {
        // Arrange
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            TimeoutSeconds = -5
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("Transaction timeout cannot be less than -1", exception.Message);
        Assert.Contains("-5", exception.Message);
    }

    [Fact]
    public void Validate_Should_Throw_When_IsReadOnly_And_UseDistributedTransaction_Are_Both_True()
    {
        // Arrange
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            IsReadOnly = true,
            UseDistributedTransaction = true
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => attribute.Validate());
        Assert.Contains("Read-only transactions cannot be used with distributed transactions", exception.Message);
    }

    [Fact]
    public void Validate_Should_Not_Throw_When_Configuration_Is_Valid()
    {
        // Arrange
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            TimeoutSeconds = 60,
            IsReadOnly = false,
            UseDistributedTransaction = false
        };

        // Act & Assert (should not throw)
        attribute.Validate();
    }

    [Fact]
    public void Validate_Should_Not_Throw_When_TimeoutSeconds_Is_Zero()
    {
        // Arrange
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            TimeoutSeconds = 0
        };

        // Act & Assert (should not throw)
        attribute.Validate();
    }

    [Fact]
    public void Validate_Should_Not_Throw_When_TimeoutSeconds_Is_Negative_One()
    {
        // Arrange
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            TimeoutSeconds = -1
        };

        // Act & Assert (should not throw)
        attribute.Validate();
    }

    [Fact]
    public void Validate_Should_Not_Throw_When_IsReadOnly_Is_True_And_UseDistributedTransaction_Is_False()
    {
        // Arrange
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            IsReadOnly = true,
            UseDistributedTransaction = false
        };

        // Act & Assert (should not throw)
        attribute.Validate();
    }

    [Fact]
    public void Validate_Should_Not_Throw_When_IsReadOnly_Is_False_And_UseDistributedTransaction_Is_True()
    {
        // Arrange
        var attribute = new TransactionAttribute(IsolationLevel.ReadCommitted)
        {
            IsReadOnly = false,
            UseDistributedTransaction = true
        };

        // Act & Assert (should not throw)
        attribute.Validate();
    }
}
