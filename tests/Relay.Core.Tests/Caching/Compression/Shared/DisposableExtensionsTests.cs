using Relay.Core.Caching.Compression;
using System;
using Xunit;

namespace Relay.Core.Tests.Caching.Compression.Shared;

public class DisposableExtensionsTests
{
    private class TestDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public bool ActionExecuted { get; set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    [Fact]
    public void DisposeAfter_WithValidDisposableAndAction_ShouldExecuteActionAndDispose()
    {
        // Arrange
        var disposable = new TestDisposable();
        var actionExecuted = false;

        // Act
        disposable.DisposeAfter(d =>
        {
            actionExecuted = true;
            d.ActionExecuted = true;
        });

        // Assert
        Assert.True(actionExecuted);
        Assert.True(disposable.ActionExecuted);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeAfter_WithNullDisposable_ShouldExecuteActionWithNull()
    {
        // Arrange
        TestDisposable? disposable = null;
        TestDisposable? actionParameter = null;

        // Act
        disposable!.DisposeAfter(d => actionParameter = d);

        // Assert - action should be executed with null parameter
        Assert.Null(actionParameter);
    }

    [Fact]
    public void DisposeAfter_WithNullAction_ShouldThrowNullReferenceException()
    {
        // Arrange
        var disposable = new TestDisposable();
        Action<TestDisposable>? action = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => disposable.DisposeAfter(action!));
    }

    [Fact]
    public void DisposeAfter_WhenActionThrowsException_ShouldStillDispose()
    {
        // Arrange
        var disposable = new TestDisposable();
        var exceptionThrown = false;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            disposable.DisposeAfter(d =>
            {
                throw new InvalidOperationException("Test exception");
            }));

        Assert.Equal("Test exception", exception.Message);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeAfter_WithActionThatAccessesDisposable_ShouldWorkCorrectly()
    {
        // Arrange
        var disposable = new TestDisposable();
        string result = null!;

        // Act
        disposable.DisposeAfter(d =>
        {
            result = "Action executed";
        });

        // Assert
        Assert.Equal("Action executed", result);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeAfter_WithMultipleCalls_ShouldWorkIndependently()
    {
        // Arrange
        var disposable1 = new TestDisposable();
        var disposable2 = new TestDisposable();
        var results = new System.Collections.Generic.List<string>();

        // Act
        disposable1.DisposeAfter(d => results.Add("First"));
        disposable2.DisposeAfter(d => results.Add("Second"));

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains("First", results);
        Assert.Contains("Second", results);
        Assert.True(disposable1.IsDisposed);
        Assert.True(disposable2.IsDisposed);
    }

    [Fact]
    public void DisposeAfter_WithActionThatModifiesDisposable_ShouldReflectChanges()
    {
        // Arrange
        var disposable = new TestDisposable();

        // Act
        disposable.DisposeAfter(d =>
        {
            d.ActionExecuted = true;
        });

        // Assert
        Assert.True(disposable.ActionExecuted);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeAfter_WithEmptyAction_ShouldStillDispose()
    {
        // Arrange
        var disposable = new TestDisposable();

        // Act
        disposable.DisposeAfter(d => { });

        // Assert
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeAfter_WithActionThatThrowsAndCatches_ShouldStillDispose()
    {
        // Arrange
        var disposable = new TestDisposable();
        var exceptionHandled = false;

        // Act
        disposable.DisposeAfter(d =>
        {
            try
            {
                throw new InvalidOperationException("Test exception");
            }
            catch
            {
                exceptionHandled = true;
            }
        });

        // Assert
        Assert.True(exceptionHandled);
        Assert.True(disposable.IsDisposed);
    }
}