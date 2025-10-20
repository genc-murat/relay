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
    public class AOTHelpersTests
    {
        [Fact]
        public void GetTypeName_ShouldReturnCorrectName()
        {
            // Act
            var result = AOTHelpers.GetTypeName<string>();

            // Assert
            Assert.Equal("String", result);
        }

        [Fact]
        public void IsValueType_ShouldReturnCorrectResult()
        {
            // Act & Assert
            Assert.True(AOTHelpers.IsValueType<int>());
            Assert.False(AOTHelpers.IsValueType<string>());
        }

        [Fact]
        public void CreateDefault_ShouldReturnDefault()
        {
            // Act & Assert
            Assert.Equal(0, AOTHelpers.CreateDefault<int>());
            Assert.Equal(default(string), AOTHelpers.CreateDefault<string>());
        }
    }
}