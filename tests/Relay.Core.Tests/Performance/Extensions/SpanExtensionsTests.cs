using System;
using Relay.Core.Performance.Extensions;
using Xunit;

namespace Relay.Core.Tests.Performance.Extensions;

/// <summary>
/// Comprehensive tests for SpanExtensions to increase test coverage
/// </summary>
public class SpanExtensionsTests
{
    [Fact]
    public void CopyToSpan_ReadOnlySpanToSpan_WithSourceLargerThanDestination_ShouldCopyLimitedElements()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3, 4, 5 }.AsSpan();
        var destination = new byte[3];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(3, copied);
        Assert.Equal(new byte[] { 1, 2, 3 }, destination);
    }

    [Fact]
    public void CopyToSpan_ReadOnlySpanToSpan_WithDestinationLargerThanSource_ShouldCopyAllSourceElements()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3 }.AsSpan();
        var destination = new byte[5];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(3, copied);
        Assert.Equal(new byte[] { 1, 2, 3, 0, 0 }, destination);
    }

    [Fact]
    public void CopyToSpan_ReadOnlySpanToSpan_WithSameSize_ShouldCopyAllElements()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3, 4, 5 }.AsSpan();
        var destination = new byte[5];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(5, copied);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, destination);
    }

    [Fact]
    public void CopyToSpan_SpanToSpan_WithSourceLargerThanDestination_ShouldCopyLimitedElements()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3, 4, 5 }.AsSpan();
        var destination = new byte[3];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(3, copied);
        Assert.Equal(new byte[] { 1, 2, 3 }, destination);
    }

    [Fact]
    public void CopyToSpan_SpanToSpan_WithDestinationLargerThanSource_ShouldCopyAllSourceElements()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3 }.AsSpan();
        var destination = new byte[5];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(3, copied);
        Assert.Equal(new byte[] { 1, 2, 3, 0, 0 }, destination);
    }

    [Fact]
    public void CopyToSpan_SpanToSpan_WithSameSize_ShouldCopyAllElements()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3, 4, 5 }.AsSpan();
        var destination = new byte[5];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(5, copied);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, destination);
    }

    [Fact]
    public void CopyToSpan_ReadOnlySpanToSpan_WithBothEmpty_ShouldCopyZeroElements()
    {
        // Arrange
        var source = Array.Empty<byte>().AsSpan();
        var destination = Array.Empty<byte>().AsSpan();

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(0, copied);
    }

    [Fact]
    public void CopyToSpan_SpanToSpan_WithBothEmpty_ShouldCopyZeroElements()
    {
        // Arrange
        var source = Array.Empty<byte>().AsSpan();
        var destination = Array.Empty<byte>().AsSpan();

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(0, copied);
    }

    [Fact]
    public void SafeSlice_Span_WithValidRange_ShouldReturnCorrectSlice()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var span = data.AsSpan();

        // Act
        var slice = span.SafeSlice(2, 4);

        // Assert
        Assert.Equal(4, slice.Length);
        Assert.Equal(new byte[] { 3, 4, 5, 6 }, slice.ToArray());
    }

    [Fact]
    public void SafeSlice_ReadOnlySpan_WithValidRange_ShouldReturnCorrectSlice()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var span = data.AsSpan();

        // Act
        var slice = ((ReadOnlySpan<byte>)span).SafeSlice(2, 4);

        // Assert
        Assert.Equal(4, slice.Length);
        Assert.Equal(new byte[] { 3, 4, 5, 6 }, slice.ToArray());
    }

    [Fact]
    public void SafeSlice_Span_WithStartAtZero_ShouldReturnSliceFromBeginning()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = span.SafeSlice(0, 3);

        // Assert
        Assert.Equal(3, slice.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, slice.ToArray());
    }

    [Fact]
    public void SafeSlice_ReadOnlySpan_WithStartAtZero_ShouldReturnSliceFromBeginning()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = ((ReadOnlySpan<byte>)span).SafeSlice(0, 3);

        // Assert
        Assert.Equal(3, slice.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, slice.ToArray());
    }

    [Fact]
    public void SafeSlice_Span_WithLengthExceedingBounds_ShouldReturnClampedSlice()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = span.SafeSlice(2, 10); // Request more than available

        // Assert
        Assert.Equal(3, slice.Length); // Only 3 elements available from index 2
        Assert.Equal(new byte[] { 3, 4, 5 }, slice.ToArray());
    }

    [Fact]
    public void SafeSlice_ReadOnlySpan_WithLengthExceedingBounds_ShouldReturnClampedSlice()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = ((ReadOnlySpan<byte>)span).SafeSlice(2, 10); // Request more than available

        // Assert
        Assert.Equal(3, slice.Length); // Only 3 elements available from index 2
        Assert.Equal(new byte[] { 3, 4, 5 }, slice.ToArray());
    }

    [Fact]
    public void SafeSlice_Span_WithStartBeyondSpanLength_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = span.SafeSlice(10, 2); // Start beyond length

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void SafeSlice_ReadOnlySpan_WithStartBeyondSpanLength_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = ((ReadOnlySpan<byte>)span).SafeSlice(10, 2); // Start beyond length

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void SafeSlice_Span_WithNegativeStart_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = span.SafeSlice(-1, 2); // Negative start

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void SafeSlice_ReadOnlySpan_WithNegativeStart_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = ((ReadOnlySpan<byte>)span).SafeSlice(-1, 2); // Negative start

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void SafeSlice_Span_WithNegativeLength_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = span.SafeSlice(2, -1); // Negative length

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void SafeSlice_ReadOnlySpan_WithNegativeLength_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = ((ReadOnlySpan<byte>)span).SafeSlice(2, -1); // Negative length

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void SafeSlice_Span_WithZeroLength_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = span.SafeSlice(2, 0); // Zero length

        // Assert
        Assert.Equal(0, slice.Length);
    }

    [Fact]
    public void SafeSlice_ReadOnlySpan_WithZeroLength_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = ((ReadOnlySpan<byte>)span).SafeSlice(2, 0); // Zero length

        // Assert
        Assert.Equal(0, slice.Length);
    }

    [Fact]
    public void SafeSlice_Span_WithStartAtEnd_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = span.SafeSlice(5, 2); // Start at end of span

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void SafeSlice_ReadOnlySpan_WithStartAtEnd_ShouldReturnEmpty()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();

        // Act
        var slice = ((ReadOnlySpan<byte>)span).SafeSlice(5, 2); // Start at end of span

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void CopyToSpan_WithDifferentValueTypes_ShouldWorkCorrectly()
    {
        // Test with int
        var sourceInt = new[] { 10, 20, 30, 40 }.AsSpan();
        var destInt = new int[2];
        var copiedInt = sourceInt.CopyToSpan(destInt);
        Assert.Equal(2, copiedInt);
        Assert.Equal(new[] { 10, 20 }, destInt);

        // Test with char
        var sourceChar = "hello".AsSpan();
        var destChar = new char[3];
        var copiedChar = sourceChar.CopyToSpan(destChar);
        Assert.Equal(3, copiedChar);
        Assert.Equal("hel", new string(destChar));

        // Test with float
        var sourceFloat = new[] { 1.1f, 2.2f, 3.3f }.AsSpan();
        var destFloat = new float[5];
        var copiedFloat = sourceFloat.CopyToSpan(destFloat);
        Assert.Equal(3, copiedFloat);
        Assert.Equal(1.1f, destFloat[0]);
        Assert.Equal(2.2f, destFloat[1]);
        Assert.Equal(3.3f, destFloat[2]);
    }

    [Fact]
    public void SafeSlice_WithDifferentValueTypes_ShouldWorkCorrectly()
    {
        // Test with int
        var intData = new[] { 100, 200, 300, 400, 500 };
        var intSlice = intData.AsSpan().SafeSlice(1, 3);
        Assert.Equal(3, intSlice.Length);
        Assert.Equal(new[] { 200, 300, 400 }, intSlice.ToArray());

        // Test with char
        var charData = "abcdefgh".AsSpan();
        var charSlice = charData.SafeSlice(2, 4);
        Assert.Equal(4, charSlice.Length);
        Assert.Equal("cdef", new string(charSlice));

        // Test with double
        var doubleData = new[] { 1.5, 2.5, 3.5, 4.5 };
        var doubleSlice = doubleData.AsSpan().SafeSlice(1, 2);
        Assert.Equal(2, doubleSlice.Length);
        Assert.Equal(new[] { 2.5, 3.5 }, doubleSlice.ToArray());
    }

    [Fact]
    public void CopyToSpan_WithSpanOverload_ShouldWorkSameAsReadOnlySpanVersion()
    {
        // Arrange - Use same data for both tests
        var sourceData = new byte[] { 10, 20, 30, 40, 50 };
        var destData1 = new byte[3];
        var destData2 = new byte[3];
        
        var sourceSpan = sourceData.AsSpan();
        var sourceReadOnlySpan = (ReadOnlySpan<byte>)sourceData.AsSpan();

        // Act - Both should produce same result
        var copiedFromSpan = sourceSpan.CopyToSpan(destData1);
        var copiedFromReadOnlySpan = sourceReadOnlySpan.CopyToSpan(destData2);

        // Assert - Both should produce same result
        Assert.Equal(copiedFromSpan, copiedFromReadOnlySpan);
        Assert.Equal(destData1, destData2);
    }
}