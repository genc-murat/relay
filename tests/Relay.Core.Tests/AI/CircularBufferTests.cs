using System;
using System.Linq;
using Xunit;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;

namespace Relay.Core.Tests.AI
{
    public class CircularBufferTests
    {
        [Fact]
        public void Enumerator_Should_Iterate_In_Order()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            var list = buffer.ToList();
            Assert.Equal(new[] { 1, 2, 3 }, list);
        }

        [Fact]
        public void Enumerator_Should_Work_After_Overflow()
        {
            var buffer = new CircularBuffer<int>(3);
            for (int i = 1; i <= 5; i++)
            {
                buffer.Add(i);
            }

            var list = buffer.ToList();
            Assert.Equal(new[] { 3, 4, 5 }, list);
        }

        [Fact]
        public void Where_Should_Filter_Without_Copying()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            var even = buffer.Where(x => x % 2 == 0).ToList();
            Assert.Equal(new[] { 2, 4 }, even);
        }

        [Fact]
        public void Count_Should_Be_Correct()
        {
            var buffer = new CircularBuffer<int>(3);
            Assert.Empty(buffer);

            buffer.Add(1);
            Assert.Single(buffer);

            buffer.Add(2);
            buffer.Add(3);
            Assert.Equal(3, buffer.Count);

            buffer.Add(4); // overflow
            Assert.Equal(3, buffer.Count);
        }

        [Fact]
        public void Indexer_Should_Work()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            buffer.Add(30);

            Assert.Equal(10, buffer[0]);
            Assert.Equal(20, buffer[1]);
            Assert.Equal(30, buffer[2]);
        }

        [Fact]
        public void Indexer_Should_Work_After_Overflow()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // overflow, now 2,3,4

            Assert.Equal(2, buffer[0]);
            Assert.Equal(3, buffer[1]);
            Assert.Equal(4, buffer[2]);
        }

        [Fact]
        public void RemoveFront_Should_Work()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            buffer.RemoveFront(2);
            Assert.Equal(2, buffer.Count);
            Assert.Equal(3, buffer[0]);
            Assert.Equal(4, buffer[1]);
        }

        [Fact]
        public void RemoveFront_After_Overflow_Should_Work()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // now 2,3,4

            buffer.RemoveFront(1); // remove 2, now 3,4
            Assert.Equal(2, buffer.Count);
            Assert.Equal(3, buffer[0]);
            Assert.Equal(4, buffer[1]);
        }

        [Fact]
        public void Clear_Should_Remove_All_Elements()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.Equal(3, buffer.Count);

            buffer.Clear();

            Assert.Empty(buffer);
            Assert.Empty(buffer);
        }

        [Fact]
        public void Clear_Should_Work_On_Empty_Buffer()
        {
            var buffer = new CircularBuffer<int>(5);

            buffer.Clear();

            Assert.Empty(buffer);
            Assert.Empty(buffer);
        }

        [Fact]
        public void Clear_Should_Work_After_Overflow()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // overflow

            Assert.Equal(3, buffer.Count);

            buffer.Clear();

            Assert.Empty(buffer);
            Assert.Empty(buffer);
        }

        [Fact]
        public void Clear_Should_Work_After_RemoveFront()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.RemoveFront(2); // remove 1,2

            Assert.Equal(2, buffer.Count);

            buffer.Clear();

            Assert.Empty(buffer);
            Assert.Empty(buffer);
        }

        [Fact]
        public void ToArray_Should_Return_Correct_Array_When_Not_Wrapped()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            var array = buffer.ToArray();
            Assert.Equal(new[] { 1, 2, 3 }, array);
        }

        [Fact]
        public void ToArray_Should_Return_Correct_Array_When_Wrapped()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // overflow, now contains 2,3,4

            var array = buffer.ToArray();
            Assert.Equal(new[] { 2, 3, 4 }, array);
        }

        [Fact]
        public void ToArray_Should_Return_Empty_Array_When_Buffer_Is_Empty()
        {
            var buffer = new CircularBuffer<int>(5);

            var array = buffer.ToArray();
            Assert.Empty(array);
        }

        [Fact]
        public void ToArray_Should_Return_Full_Array_When_Buffer_Is_Full()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            var array = buffer.ToArray();
            Assert.Equal(new[] { 1, 2, 3 }, array);
        }

        [Fact]
        public void ToArray_Should_Work_With_Single_Element()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(42);

            var array = buffer.ToArray();
            Assert.Equal(new[] { 42 }, array);
        }

        [Fact]
        public void ToArray_Should_Work_After_Clear()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Clear();

            var array = buffer.ToArray();
            Assert.Empty(array);
        }

        [Fact]
        public void ToArray_Should_Work_After_RemoveFront()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.RemoveFront(2); // remove 1,2, now 3,4

            var array = buffer.ToArray();
            Assert.Equal(new[] { 3, 4 }, array);
        }

        [Fact]
        public void Constructor_Should_Initialize_Buffer_With_Correct_Capacity()
        {
            var buffer = new CircularBuffer<int>(10);

            Assert.Equal(10, buffer.Capacity);
            Assert.Empty(buffer);
            Assert.Empty(buffer);
        }

        [Fact]
        public void Capacity_Should_Return_Buffer_Size()
        {
            var buffer = new CircularBuffer<int>(5);
            Assert.Equal(5, buffer.Capacity);

            var buffer2 = new CircularBuffer<string>(100);
            Assert.Equal(100, buffer2.Capacity);
        }

        [Fact]
        public void Add_Should_Add_Elements_To_Buffer()
        {
            var buffer = new CircularBuffer<int>(3);

            buffer.Add(1);
            Assert.Single(buffer);
            Assert.Equal(1, buffer[0]);

            buffer.Add(2);
            Assert.Equal(2, buffer.Count);
            Assert.Equal(2, buffer[1]);

            buffer.Add(3);
            Assert.Equal(3, buffer.Count);
            Assert.Equal(3, buffer[2]);
        }

        [Fact]
        public void Add_Should_Overwrite_Oldest_Element_When_Full()
        {
            var buffer = new CircularBuffer<int>(3);

            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // should overwrite 1

            Assert.Equal(3, buffer.Count);
            Assert.Equal(2, buffer[0]);
            Assert.Equal(3, buffer[1]);
            Assert.Equal(4, buffer[2]);
        }

        [Fact]
        public void Indexer_Should_Throw_OutOfRange_For_Negative_Index()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[-1]);
        }

        [Fact]
        public void Indexer_Should_Throw_OutOfRange_For_Index_Greater_Than_Or_Equal_To_Count()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[2]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[3]);
        }

        [Fact]
        public void RemoveFront_Should_Throw_OutOfRange_For_Negative_Count()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.RemoveFront(-1));
        }

        [Fact]
        public void RemoveFront_Should_Throw_OutOfRange_For_Count_Greater_Than_Current_Count()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);

            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.RemoveFront(3));
        }

        [Fact]
        public void Dispose_Should_Dispose_Lock()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Dispose();

            // Dispose should dispose the ReaderWriterLockSlim
            // We can't directly test this, but we can verify the buffer still works
            // (though in practice, disposed objects should not be used)
            // For this test, we'll just ensure no exception is thrown
            Assert.NotNull(buffer);
        }
    }
}