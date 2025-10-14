using System.Linq;
using Xunit;
using Relay.Core.AI;

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
    }
}