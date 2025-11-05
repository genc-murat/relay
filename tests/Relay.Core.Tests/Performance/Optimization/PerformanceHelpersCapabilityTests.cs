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
    public class PerformanceHelpersCapabilityTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceHelpersCapabilityTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void IsSIMDAvailable_ShouldReturnBoolean()
        {
            // Act
            var result = PerformanceHelpers.IsSIMDAvailable;

            // Assert
            Assert.IsType<bool>(result);
            _output.WriteLine($"SIMD Available: {result}");
        }

        [Fact]
        public void IsAVX2Available_ShouldReturnBoolean()
        {
            // Act
            var result = PerformanceHelpers.IsAVX2Available;

            // Assert
            Assert.IsType<bool>(result);
            _output.WriteLine($"AVX2 Available: {result}");
        }

        [Fact]
        public void IsSSEAvailable_ShouldReturnBoolean()
        {
            // Act
            var result = PerformanceHelpers.IsSSEAvailable;

            // Assert
            Assert.IsType<bool>(result);
            _output.WriteLine($"SSE Available: {result}");
        }

        [Fact]
        public void VectorSize_ShouldBePositive()
        {
            // Act
            var result = PerformanceHelpers.VectorSize;

            // Assert
            Assert.True(result > 0);
            _output.WriteLine($"Vector Size: {result}");
        }
    }
}
