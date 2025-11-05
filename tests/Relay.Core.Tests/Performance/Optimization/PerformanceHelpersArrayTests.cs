using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Relay.Core.Performance.Optimization;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests.Performance.Optimization
{
    public class PerformanceHelpersArrayTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceHelpersArrayTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void InitializeArray_ValidSpan_ShouldFillCorrectly()
        {
            // Arrange
            var array = new int[100];
            var span = array.AsSpan();
            const int value = 42;

            // Act
            PerformanceHelpers.InitializeArray(span, value);

            // Assert
            foreach (var item in span)
            {
                Assert.Equal(value, item);
            }
        }

        [Fact]
        public void InitializeArray_EmptySpan_ShouldNotThrow()
        {
            // Arrange
            var span = new Span<int>();

            // Act & Assert
            PerformanceHelpers.InitializeArray(span, 42);
        }

        [Fact]
        public void InitializeArray_SmallSpan_ShouldFillCorrectly()
        {
            // Arrange
            var array = new int[3];
            var span = array.AsSpan();
            const int value = 123;

            // Act
            PerformanceHelpers.InitializeArray(span, value);

            // Assert
            Assert.Equal(value, span[0]);
            Assert.Equal(value, span[1]);
            Assert.Equal(value, span[2]);
        }

        [Fact]
        public void SequenceEqual_IdenticalSpans_ShouldReturnTrue()
        {
            // Arrange
            var data1 = new byte[] { 1, 2, 3, 4, 5 };
            var data2 = new byte[] { 1, 2, 3, 4, 5 };
            var span1 = data1.AsSpan();
            var span2 = data2.AsSpan();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SequenceEqual_DifferentSpans_ShouldReturnFalse()
        {
            // Arrange
            var data1 = new byte[] { 1, 2, 3, 4, 5 };
            var data2 = new byte[] { 1, 2, 3, 4, 6 };
            var span1 = data1.AsSpan();
            var span2 = data2.AsSpan();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqual_DifferentLengths_ShouldReturnFalse()
        {
            // Arrange
            var data1 = new byte[] { 1, 2, 3 };
            var data2 = new byte[] { 1, 2, 3, 4 };
            var span1 = data1.AsSpan();
            var span2 = data2.AsSpan();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqual_EmptySpans_ShouldReturnTrue()
        {
            // Arrange
            var span1 = new Span<byte>();
            var span2 = new Span<byte>();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SequenceEqual_LargeSpans_ShouldReturnCorrectResult()
        {
            // Arrange
            var data1 = new byte[10000];
            var data2 = new byte[10000];

            for (int i = 0; i < data1.Length; i++)
            {
                data1[i] = (byte)(i % 256);
                data2[i] = (byte)(i % 256);
            }

            // Make one difference
            data2[5000] = 255;

            var span1 = data1.AsSpan();
            var span2 = data2.AsSpan();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.False(result);
        }
    }
}
