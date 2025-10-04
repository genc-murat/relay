
using System;
using Relay.Core.Caching;
using Xunit;
using FluentAssertions;

namespace Relay.Core.Tests.Caching
{
    public class DefaultCacheKeyGeneratorTests
    {
        private readonly DefaultCacheKeyGenerator _keyGenerator = new DefaultCacheKeyGenerator();

        public class SimpleRequest
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        public class AnotherRequest
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        public class ComplexRequest
        {
            public Guid RequestId { get; set; }
            public NestedData? Data { get; set; }
        }

        public class NestedData
        {
            public bool IsActive { get; set; }
            public string[]? Tags { get; set; }
        }

        [Fact]
        public void GenerateKey_WithDefaultPattern_GeneratesCorrectKey()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "Test" };
            var attribute = new DistributedCacheAttribute(); // Uses default pattern

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            key.Should().StartWith("SimpleRequest:");
            key.Should().NotContain("{"); // No unresolved placeholders
            key.Should().NotContain("}");
        }

        [Fact]
        public void GenerateKey_WithCustomPattern_GeneratesCorrectKey()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "Test" };
            var attribute = new DistributedCacheAttribute
            {
                KeyPattern = "MyCache:{Region}:{RequestType}-{RequestHash}",
                Region = "Users"
            };

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            key.Should().StartWith("MyCache:Users:SimpleRequest-");
        }

        [Fact]
        public void GenerateKey_ForIdenticalRequests_GeneratesSameKey()
        {
            // Arrange
            var request1 = new SimpleRequest { Id = 123, Name = "SameName" };
            var request2 = new SimpleRequest { Id = 123, Name = "SameName" };
            var attribute = new DistributedCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            key1.Should().Be(key2);
        }

        [Fact]
        public void GenerateKey_ForDifferentRequests_GeneratesDifferentKeys()
        {
            // Arrange
            var request1 = new SimpleRequest { Id = 1, Name = "A" };
            var request2 = new SimpleRequest { Id = 2, Name = "B" };
            var attribute = new DistributedCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void GenerateKey_ForDifferentRequestTypes_GeneratesDifferentKeys()
        {
            // Arrange
            var request1 = new SimpleRequest { Id = 1, Name = "Same" };
            var request2 = new AnotherRequest { Id = 1, Name = "Same" };
            var attribute = new DistributedCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            key1.Should().NotBe(key2);
            key1.Should().Contain("SimpleRequest");
            key2.Should().Contain("AnotherRequest");
        }

        [Fact]
        public void GenerateKey_WithComplexObject_GeneratesKey()
        {
            // Arrange
            var request = new ComplexRequest
            {
                RequestId = Guid.NewGuid(),
                Data = new NestedData
                {
                    IsActive = true,
                    Tags = new[] { "tag1", "tag2" }
                }
            };
            var attribute = new DistributedCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            key.Should().NotBeNullOrEmpty();
            key.Should().StartWith("ComplexRequest:");
        }

        [Fact]
        public void GenerateKey_WithDifferentPropertyOrder_GeneratesSameKey()
        {
            // Arrange
            // Note: JsonSerializer does not guarantee property order, but for a given type, it's typically consistent.
            // This test verifies that two objects that are equivalent result in the same key.
            var request1 = new SimpleRequest { Name = "OrderTest", Id = 99 };
            var request2 = new SimpleRequest { Id = 99, Name = "OrderTest" };
            var attribute = new DistributedCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            key1.Should().Be(key2);
        }
    }
}
