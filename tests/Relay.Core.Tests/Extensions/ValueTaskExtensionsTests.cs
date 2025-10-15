using Relay.Core;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class ValueTaskExtensionsTests
{
    [Fact]
    public async Task FromException_Generic_WithException_CreatesFaultedValueTask()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var valueTask = ValueTaskExtensions.FromException<string>(exception);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => valueTask.AsTask());
    }

    [Fact]
    public async Task FromException_NonGeneric_WithException_CreatesFaultedValueTask()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var valueTask = ValueTaskExtensions.FromException(exception);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => valueTask.AsTask());
    }

    [Fact]
    public async Task CompletedTask_IsCompleted()
    {
        // Act
        var valueTask = ValueTaskExtensions.CompletedTask;

        // Assert
        Assert.True(valueTask.IsCompleted);
        await valueTask;
    }
}