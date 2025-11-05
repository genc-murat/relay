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
    public class PerformanceHelpersPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceHelpersPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task PrefetchMemory_PerformanceTest_ShouldCompleteInReasonableTime()
        {
            // Arrange
            var objects = new TestObject[1000];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = new TestObject { Id = i, Name = $"Test{i}" };

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                foreach (var obj in objects)
                {
                    PerformanceHelpers.PrefetchMemory(obj);
                }
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"PrefetchMemory performance: 100,000 calls in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / 100000:F6}ms per call");

            // Should complete quickly (this is a very loose requirement)
            Assert.True(stopwatch.ElapsedMilliseconds < 10000, "PrefetchMemory took too long");
        }

        [Fact]
        public void PrefetchMemory_MemoryAccessPattern_ShouldNotCorruptData()
        {
            // Arrange
            var originalData = new int[1000];
            for (int i = 0; i < originalData.Length; i++)
                originalData[i] = i;

            var testData = new int[1000];
            Array.Copy(originalData, testData, originalData.Length);

            // Act
            PerformanceHelpers.PrefetchMemory(testData.AsSpan());

            // Assert - Data should remain unchanged
            for (int i = 0; i < originalData.Length; i++)
            {
                Assert.Equal(originalData[i], testData[i]);
            }
        }

        // Test helper class
        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;

            public override int GetHashCode()
            {
                return HashCode.Combine(Id, Name);
            }
        }
    }
}
