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
    public class PerformanceHelpersBatchSizeTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceHelpersBatchSizeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void GetOptimalBatchSize_ShouldReturnValidSize(int totalItems)
        {
            // Act
            var result = PerformanceHelpers.GetOptimalBatchSize(totalItems);

            // Assert
            Assert.True(result >= totalItems);
            if (PerformanceHelpers.IsSIMDAvailable)
            {
                Assert.True(result % PerformanceHelpers.VectorSize == 0 || result == totalItems);
            }
            _output.WriteLine($"Total Items: {totalItems}, Optimal Batch Size: {result}");
        }
    }
}