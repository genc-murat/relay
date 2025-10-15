using System;
using Relay.Core.Diagnostics.Registry;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    /// <summary>
    /// Unit tests for PipelineInfo to improve code coverage.
    /// </summary>
    public class PipelineInfoTests
    {
        [Fact]
        public void Constructor_DefaultValues_SetsProperties()
        {
            // Arrange & Act
            var pipelineInfo = new PipelineInfo();

            // Assert
            Assert.Equal(string.Empty, pipelineInfo.PipelineType);
            Assert.Equal(string.Empty, pipelineInfo.MethodName);
            Assert.Equal(0, pipelineInfo.Order);
            Assert.Equal(string.Empty, pipelineInfo.Scope);
            Assert.True(pipelineInfo.IsEnabled);
        }

        [Fact]
        public void Properties_CanBeSet_AndRetrieved()
        {
            // Arrange
            var pipelineInfo = new PipelineInfo
            {
                PipelineType = "TestPipeline",
                MethodName = "HandleAsync",
                Order = 10,
                Scope = "Global",
                IsEnabled = false
            };

            // Assert
            Assert.Equal("TestPipeline", pipelineInfo.PipelineType);
            Assert.Equal("HandleAsync", pipelineInfo.MethodName);
            Assert.Equal(10, pipelineInfo.Order);
            Assert.Equal("Global", pipelineInfo.Scope);
            Assert.False(pipelineInfo.IsEnabled);
        }

        [Fact]
        public void Equals_WithSameValues_ReturnsTrue()
        {
            // Arrange
            var pipeline1 = new PipelineInfo
            {
                PipelineType = "TestPipeline",
                MethodName = "HandleAsync",
                Order = 10,
                Scope = "Global",
                IsEnabled = true
            };

            var pipeline2 = new PipelineInfo
            {
                PipelineType = "TestPipeline",
                MethodName = "HandleAsync",
                Order = 10,
                Scope = "Global",
                IsEnabled = true
            };

            // Act
            var result = pipeline1.Equals(pipeline2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_WithDifferentPipelineType_ReturnsFalse()
        {
            // Arrange
            var pipeline1 = new PipelineInfo { PipelineType = "Pipeline1" };
            var pipeline2 = new PipelineInfo { PipelineType = "Pipeline2" };

            // Act
            var result = pipeline1.Equals(pipeline2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_WithDifferentMethodName_ReturnsFalse()
        {
            // Arrange
            var pipeline1 = new PipelineInfo
            {
                PipelineType = "Test",
                MethodName = "Method1"
            };
            var pipeline2 = new PipelineInfo
            {
                PipelineType = "Test",
                MethodName = "Method2"
            };

            // Act
            var result = pipeline1.Equals(pipeline2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_WithDifferentOrder_ReturnsFalse()
        {
            // Arrange
            var pipeline1 = new PipelineInfo
            {
                PipelineType = "Test",
                MethodName = "Handle",
                Order = 10
            };
            var pipeline2 = new PipelineInfo
            {
                PipelineType = "Test",
                MethodName = "Handle",
                Order = 20
            };

            // Act
            var result = pipeline1.Equals(pipeline2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_WithDifferentScope_ReturnsFalse()
        {
            // Arrange
            var pipeline1 = new PipelineInfo
            {
                PipelineType = "Test",
                MethodName = "Handle",
                Scope = "Global"
            };
            var pipeline2 = new PipelineInfo
            {
                PipelineType = "Test",
                MethodName = "Handle",
                Scope = "Request"
            };

            // Act
            var result = pipeline1.Equals(pipeline2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_WithDifferentIsEnabled_ReturnsFalse()
        {
            // Arrange
            var pipeline1 = new PipelineInfo
            {
                PipelineType = "Test",
                MethodName = "Handle",
                IsEnabled = true
            };
            var pipeline2 = new PipelineInfo
            {
                PipelineType = "Test",
                MethodName = "Handle",
                IsEnabled = false
            };

            // Act
            var result = pipeline1.Equals(pipeline2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var pipeline = new PipelineInfo();

            // Act
            var result = pipeline.Equals(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_WithNullObject_ReturnsFalse()
        {
            // Arrange
            var pipeline = new PipelineInfo();

            // Act
            var result = pipeline.Equals((object?)null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_WithDifferentType_ReturnsFalse()
        {
            // Arrange
            var pipeline = new PipelineInfo();
            var other = new object();

            // Act
            var result = pipeline.Equals(other);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetHashCode_WithSameValues_ReturnsSameHash()
        {
            // Arrange
            var pipeline1 = new PipelineInfo
            {
                PipelineType = "TestPipeline",
                MethodName = "HandleAsync",
                Order = 10,
                Scope = "Global",
                IsEnabled = true
            };

            var pipeline2 = new PipelineInfo
            {
                PipelineType = "TestPipeline",
                MethodName = "HandleAsync",
                Order = 10,
                Scope = "Global",
                IsEnabled = true
            };

            // Act
            var hash1 = pipeline1.GetHashCode();
            var hash2 = pipeline2.GetHashCode();

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void GetHashCode_WithDifferentValues_ReturnsDifferentHash()
        {
            // Arrange
            var pipeline1 = new PipelineInfo { PipelineType = "Pipeline1" };
            var pipeline2 = new PipelineInfo { PipelineType = "Pipeline2" };

            // Act
            var hash1 = pipeline1.GetHashCode();
            var hash2 = pipeline2.GetHashCode();

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void IsEnabled_DefaultValue_IsTrue()
        {
            // Arrange & Act
            var pipelineInfo = new PipelineInfo();

            // Assert
            Assert.True(pipelineInfo.IsEnabled);
        }

        [Fact]
        public void Order_CanBeNegative()
        {
            // Arrange
            var pipelineInfo = new PipelineInfo { Order = -10 };

            // Act & Assert
            Assert.Equal(-10, pipelineInfo.Order);
        }

        [Fact]
        public void Properties_CanBeUpdated()
        {
            // Arrange
            var pipelineInfo = new PipelineInfo
            {
                PipelineType = "Initial",
                MethodName = "Initial",
                Order = 1
            };

            // Act
            pipelineInfo.PipelineType = "Updated";
            pipelineInfo.MethodName = "Updated";
            pipelineInfo.Order = 100;
            pipelineInfo.Scope = "Request";
            pipelineInfo.IsEnabled = false;

            // Assert
            Assert.Equal("Updated", pipelineInfo.PipelineType);
            Assert.Equal("Updated", pipelineInfo.MethodName);
            Assert.Equal(100, pipelineInfo.Order);
            Assert.Equal("Request", pipelineInfo.Scope);
            Assert.False(pipelineInfo.IsEnabled);
        }
    }
}
